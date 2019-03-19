using Dicom;
using Dicom.Imaging;
using Dicom.Media;
using Dicom.Network;
using GUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace PacsInterface
{
    class Program
    {
        // configure server access, init GUI, init listener
        CurrentConfiguration configuration;
        SetupGUI setupGUI;
        Series seriesTemplate;
        internal Program(MainWindow mainWindow)
        {
            // configure server info
            configuration = new CurrentConfiguration();
            Debug.welcome();
            // delete all local files
            /*
            var di = new DirectoryInfo(configuration.fileDestination);
            foreach (var file in di.GetFiles()) file.Delete();
            foreach (var folder in di.GetDirectories()) Directory.Delete(folder.FullName, true);
            */

            // setup GUI and handle GUI events
            setupGUI = new SetupGUI(mainWindow);

            // setup query page
            var studyTemplate = new Study();
            bool studyInstanceUIDVisible = false;
            foreach (var line in File.ReadAllLines("StudyColumnsToShow.txt"))
            {
                if (line != "StudyInstanceUID")
                    studyTemplate.Add(new QueryParameter { name = line });
                else
                    studyInstanceUIDVisible = true;
            }
            studyTemplate.Add(new QueryParameter { name = "StudyInstanceUID", visible = studyInstanceUIDVisible });

            setupGUI.setupQueryFields(studyTemplate);
            setupGUI.searchStudiesEvent += searchStudies;

            setupGUI.setupStudyTable(studyTemplate);
            mainWindow.queryPage.onStudyClickedEvent += searchSeries;

            // setup download page
            seriesTemplate = new Series();
            studyInstanceUIDVisible = false;
            bool seriesInstanceUIDVisible = false;
            foreach (var line in File.ReadAllLines("SeriesColumnsToShow.txt"))
            {
                if (line == "StudyInstanceUID") studyInstanceUIDVisible = true;
                else if (line == "SeriesInstanceUID") seriesInstanceUIDVisible = true;
                else seriesTemplate.Add(new QueryParameter { name = line });
            }
            seriesTemplate.Add(new QueryParameter { name = "StudyInstanceUID", visible = studyInstanceUIDVisible });
            seriesTemplate.Add(new QueryParameter { name = "SeriesInstanceUID", visible = seriesInstanceUIDVisible });

            setupGUI.setupSeriesTable(seriesTemplate);
            mainWindow.downloadPage.onSeriesClickedEvent += downloadSeries;

            mainWindow.downloadPage.onThumbClickedEvent += onThumbClicked;

            // setup local page
            setupGUI.setupLocalStudyTable(studyTemplate);
            setupGUI.setupLocalQueryFields();
            setupGUI.searchLocalStudiesEvent += searchLocalStudies;
            setupGUI.setupLocalSeriesTable(seriesTemplate);
            mainWindow.localStudiesPage.onLocalStudyClickedEvent += searchLocalSeries;
            mainWindow.localSeriesPage.onLocalSeriesClickedEvent += showLocalSeries;
        }

        // ----------------------------study REMOTE query-------------------------------------------------
        List<Study> studyResponses;
        void searchStudies(Study studyQuery)
        {
            // init find request
            DicomCFindRequest cfind = new DicomCFindRequest(DicomQueryRetrieveLevel.Study);

            foreach (QueryParameter studyParameter in studyQuery)
                cfind.Dataset.Add(studyParameter.getTag(), studyParameter.value);

            cfind.OnResponseReceived = (request, response) =>
            {
                if (response.HasDataset) studyResponses.Add(new Study(response.Dataset, studyQuery));
                if (!response.HasDataset) Debug.gotNumberOfResults(studyResponses.Count);
            };

            var client = new DicomClient();
            client.AddRequest(cfind);

            // prepare to receive data
            studyResponses = new List<Study>();

            // send query
            Debug.studyQuery(configuration, studyQuery);
            try { client.Send(configuration.ip, configuration.port, false, configuration.thisNodeAET, configuration.AET); }
            catch (Exception) { Debug.cantReachServer(); }

            // arrange results in table
            setupGUI.addStudiesToTable(studyResponses);

        }

        // series query
        List<Series> seriesResponses;
        void searchSeries(int studyNumber)
        {
            // init find request
            DicomCFindRequest cfind = new DicomCFindRequest(DicomQueryRetrieveLevel.Series);
            seriesTemplate.setStudyInstanceUID(studyResponses[studyNumber]);

            foreach (QueryParameter studyParameter in seriesTemplate)
                cfind.Dataset.Add(studyParameter.getTag(), studyParameter.value);

            cfind.OnResponseReceived = (request, response) =>
            {
                if (response.HasDataset) seriesResponses.Add(new Series(response.Dataset, seriesTemplate));
                else Debug.gotNumberOfResults(seriesResponses.Count);
            };

            var client = new DicomClient();
            client.AddRequest(cfind);

            // prepare to receive data
            seriesResponses = new List<Series>();

            // send query
            Debug.seriesQuery(configuration, seriesTemplate);
            try { client.Send(configuration.ip, configuration.port, false, configuration.thisNodeAET, configuration.AET); }
            catch (Exception) { Debug.cantReachServer(); }

            // arrange results in table
            setupGUI.addSeriesToTable(seriesResponses);

        }

        // series download
        void downloadSeries(int seriesNumber)
        {
            // init move request
            var seriesResponse = seriesResponses[seriesNumber];
            var cmove = new DicomCMoveRequest(configuration.thisNodeAET, seriesResponse.getStudyInstanceUID(), seriesResponse.getSeriesInstanceUID());

            var client = new DicomClient();
            client.AddRequest(cmove);

            // prepare to receive data
            File.Delete("singleImage.txt");
            // write file path based on uids, current date, and path chosen by user
            string path = seriesResponse.getFullPath(configuration.fileDestination);
            Directory.CreateDirectory(path);
            using (var streamWriter = new StreamWriter("pathForDownload.txt", false))
            {
                streamWriter.WriteLine(path);
            }

            // send query
            if (Directory.GetFiles(path).Length == 0)
            {
                Debug.downloading(configuration);
                client.Send(configuration.ip, configuration.port, false, configuration.thisNodeAET, configuration.AET);
                Debug.done();
            }
            else Console.WriteLine("File is already present in database.");

        }

        // download sample image from series
        void onThumbClicked(int seriesNumber)
        {
            var seriesResponse = seriesResponses[seriesNumber];
            try
            {
                BitmapImage img = new BitmapImage();
                string SOPInstanceUID = getImagesInSeries(seriesResponse);
                if (SOPInstanceUID != "")
                {
                    Debug.downloadingImage(configuration, SOPInstanceUID);
                    img = downloadSampleImage(seriesResponse, SOPInstanceUID);
                    setupGUI.addImage(seriesNumber, img);
                    Debug.done();
                }

            }
            catch (Exception ec) { Console.WriteLine(ec.StackTrace); }

        }
        List<string> imageIDs;
        string getImagesInSeries(Series seriesResponse)
        {
            // init image query
            DicomCFindRequest cfind = new DicomCFindRequest(DicomQueryRetrieveLevel.Image);

            cfind.Dataset.Add(DicomTag.StudyInstanceUID, seriesResponse.getStudyInstanceUID());
            cfind.Dataset.Add(DicomTag.SeriesInstanceUID, seriesResponse.getSeriesInstanceUID());
            cfind.Dataset.Add(DicomTag.SOPInstanceUID, "");
            cfind.Dataset.Add(DicomTag.InstanceNumber, "");

            cfind.OnResponseReceived = (request, response) =>
            {
                if (response.HasDataset) imageIDs.Add(response.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID));
                if (!response.HasDataset) Debug.gotNumberOfResults(imageIDs.Count);
            };

            var client = new DicomClient();
            client.AddRequest(cfind);

            // prepare to receive data
            imageIDs = new List<string>();

            // send query
            Debug.imageQuery(configuration, seriesResponse);
            try { client.Send(configuration.ip, configuration.port, false, configuration.thisNodeAET, configuration.AET); }
            catch (Exception) { Debug.cantReachServer(); }

            // get just the image at the half of the series
            string SOPInstanceUID = "";
            if (imageIDs.Count > 0) SOPInstanceUID = imageIDs[(int)(imageIDs.Count / 2.0f)];
            return SOPInstanceUID;

        }
        BitmapImage downloadSampleImage(Series seriesResponse, string SOPInstanceUID)
        {
            // init move request
            var cmove = new DicomCMoveRequest(configuration.thisNodeAET, seriesResponse.getStudyInstanceUID(), seriesResponse.getSeriesInstanceUID(), SOPInstanceUID);

            var client = new DicomClient();
            client.AddRequest(cmove);

            // prepare to receive data
            File.Create("singleImage.txt").Close();
            DirectoryInfo di = new DirectoryInfo("./images/");
            foreach (FileInfo file in di.GetFiles()) file.Delete();

            // send query
            client.Send(configuration.ip, configuration.port, false, configuration.thisNodeAET, configuration.AET);

            // load the image downloaded from the listener
            var image = new BitmapImage();
            if (File.Exists("./images/file.jpg"))
            {
                var uriSource = new Uri(Path.GetFullPath("./images/file.jpg"));
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                image.UriSource = uriSource;
                image.EndInit();
            }
            else MessageBox.Show("Listener didn't receive anything or didn't work");

            File.Delete("singleImage.txt");
            return image;
        }

        //--------------------------- search LOCAL studies-------------------------------------------------
        List<Study> localStudyResponses;
        void searchLocalStudies(Study studyTemplate)
        {
            string dicomDirPath = Path.Combine(configuration.fileDestination, "DICOMDIR");

            // prepare to receive data
            localStudyResponses = new List<Study>();

            using (var fileStream = new FileStream(dicomDirPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                var dicomDir = DicomDirectory.Open(fileStream);
                if (dicomDir != null)
                    foreach (var patientRecord in dicomDir.RootDirectoryRecordCollection)
                    {
                        foreach (var studyRecord in patientRecord.LowerLevelDirectoryRecordCollection)
                        {
                            studyRecord.Add(DicomTag.PatientName, patientRecord.GetSingleValue<string>(DicomTag.PatientName));
                            studyRecord.Add(DicomTag.PatientID, patientRecord.GetSingleValue<string>(DicomTag.PatientID));
                            Study myStudy = new Study(studyRecord, studyTemplate);
                            localStudyResponses.Add(myStudy);
                        }
                    }
            }

            setupGUI.addLocalStudiesToTable(localStudyResponses);
        }

        // local series
        List<Series> localSeriesResponses;
        void searchLocalSeries(int index)
        {
            string dicomDirPath = Path.Combine(configuration.fileDestination, "DICOMDIR");

            // prepare to receive data
            localSeriesResponses = new List<Series>();

            using (var fileStream = new FileStream(dicomDirPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                var dicomDir = DicomDirectory.Open(fileStream);
                if (dicomDir != null)
                    foreach (var patientRecord in dicomDir.RootDirectoryRecordCollection)
                    {
                        foreach (var studyRecord in patientRecord.LowerLevelDirectoryRecordCollection)
                        {
                            if (studyRecord.GetSingleValue<string>(DicomTag.StudyInstanceUID) ==
                                localStudyResponses[index].getStudyInstanceUID())
                                foreach (var seriesRecord in studyRecord.LowerLevelDirectoryRecordCollection)
                                {
                                    seriesRecord.Add(DicomTag.StudyInstanceUID,
                                        studyRecord.GetSingleValue<string>(DicomTag.StudyInstanceUID));
                                    seriesRecord.Add(DicomTag.StudyDate,
                                        studyRecord.GetSingleValue<string>(DicomTag.StudyDate));
                                    Series mySeries = new Series(seriesRecord, seriesTemplate);
                                    localSeriesResponses.Add(mySeries);
                                }
                        }
                    }
            }

            setupGUI.addLocalSeriesToTable(localSeriesResponses);
            addThumbs(localSeriesResponses);
        }
        private void showLocalSeries(int index)
        {
            string fullSeriesPath = localSeriesResponses[index].getFullPath(configuration.fileDestination);
            System.Windows.Forms.Clipboard.SetDataObject(fullSeriesPath, true);
            MessageBox.Show("Full series path: " + Environment.NewLine + fullSeriesPath +
                Environment.NewLine + "Copied into clipboard");

        }

        // local sample image
        void addThumbs(List<Series> seriesResponses)
        {
            foreach (var series in seriesResponses)
            {
                string seriesPath = series.getFullPath(configuration.fileDestination);
                string thumbPath = Path.Combine(seriesPath, "thumb.jpg");
                if (!File.Exists(thumbPath))
                {
                    var files = Directory.GetFiles(seriesPath).OrderBy(name => name).ToArray();
                    string imagePath = files[(int)(files.Length / 2.0f)];
                    var thumb = new DicomImage(imagePath);
                    thumb.RenderImage().AsClonedBitmap().Save(Path.Combine(thumbPath));
                }
                var imageJpg = new BitmapImage();
                var uriSource = new Uri(Path.GetFullPath(thumbPath));
                imageJpg.BeginInit();
                imageJpg.CacheOption = BitmapCacheOption.OnLoad;
                imageJpg.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                imageJpg.UriSource = uriSource;
                imageJpg.EndInit();

                setupGUI.addLocalSeriesImage(seriesResponses.IndexOf(series), imageJpg);
            }

        }

    }
}
