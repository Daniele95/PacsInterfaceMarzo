using Dicom;
using Dicom.Imaging;
using Dicom.Media;
using Dicom.Network;
using GUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Media.Imaging;
using PacsLibrary.Query;


namespace PacsInterface
{
    class Program
    {
        public const bool useTls = true;
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

            // setup query page
            var studyTemplate = Query.studyParametersToShow("StudyColumnsToShow.txt");
            setupGUI.setupQueryFields(studyTemplate);
            setupGUI.searchStudiesEvent += searchStudies;

            setupGUI.setupStudyTable(studyTemplate);
            mainWindow.queryPage.onStudyClickedEvent += searchSeries;

            // setup download page
            seriesTemplate = Query.seriesParametersToShow("SeriesColumnsToShow.txt");
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
            studyResponses = Query.searchStudies(configuration,studyQuery);
            setupGUI.addStudiesToTable(studyResponses);
        }

        // series query
        List<Series> seriesResponses;
        void searchSeries(int studyNumber)
        {
            seriesResponses = Query.searchSeries(configuration,studyResponses[studyNumber], "SeriesColumnsToShow.txt");
            setupGUI.addSeriesToTable(seriesResponses);
        }

        // series download
        void downloadSeries(int seriesNumber)
        {
            Query.downloadSeries(configuration,seriesResponses[seriesNumber]);
        }

        // download sample image from series
        void onThumbClicked(int seriesNumber)
        {
            BitmapImage img = Query.downloadSampleImage(configuration,seriesResponses[seriesNumber]);
            setupGUI.addImage(seriesNumber, img);


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
                try
                {
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
                catch (Exception e) { }
            }

        }

    }
}
