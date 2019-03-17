using Dicom;
using Dicom.Media;
using Dicom.Network;
using GUI;
using System;
using System.Collections.Generic;
using System.IO;
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

            // setup GUI and handle GUI events
            setupGUI = new SetupGUI(mainWindow);
            setupGUI.searchStudiesEvent += searchStudies;
            mainWindow.queryPage.onStudyClickedEvent += searchSeries;
            mainWindow.downloadPage.onSeriesClickedEvent += downloadSeries;
            mainWindow.downloadPage.onThumbClickedEvent += onThumbClicked;

            // setup query page
            var studyTemplate = new Study();
            studyTemplate.Add(new QueryParameter { name = "StudyInstanceUID" });
            studyTemplate.Add(new QueryParameter { name = "PatientName" });
            studyTemplate.Add(new QueryParameter { name = "PatientID" });
            studyTemplate.Add(new QueryParameter { name = "StudyDate" });
            studyTemplate.Add(new QueryParameter { name = "ModalitiesInStudy" });
            studyTemplate.Add(new QueryParameter { name = "PatientBirthDate" });
            studyTemplate.Add(new QueryParameter { name = "AccessionNumber" });
            setupGUI.setupQueryFields(studyTemplate);
            setupGUI.setupStudyTable(studyTemplate);

            // setup download page
            seriesTemplate = new Series();
            seriesTemplate.Add(new QueryParameter { name = "StudyInstanceUID" });
            seriesTemplate.Add(new QueryParameter { name = "SeriesInstanceUID" });
            seriesTemplate.Add(new QueryParameter { name = "StudyDate" });
            seriesTemplate.Add(new QueryParameter { name = "SeriesDescription" });
            setupGUI.setupSeriesTable(seriesTemplate);

            // setup local page
            setupGUI.setupLocalTable(studyTemplate);
        }

        // study query
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

            Debug.addedSeriesToTable();

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
            string path = Path.Combine(
                configuration.fileDestination,
                DateTime.Now.Year.ToString(),
                DateTime.Now.Month.ToString(),
                DateTime.Now.Day.ToString(),
                seriesResponse.getStudyInstanceUID(),
                seriesResponse.getSeriesInstanceUID());
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
            } else Console.WriteLine("File is already present in database.");
            setupGUI.showLocal();

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

    }
}
