using Dicom;
using Dicom.Imaging;
using Dicom.Media;
using GUI;
using PacsLibrary;
using PacsLibrary.LocalQuery;
using PacsLibrary.Query;
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
        PacsLibrary.Configuration configuration;
        SetupGUI setupGUI;
        internal Program(MainWindow mainWindow)
        {
            // configure server info
            configuration = new PacsLibrary.Configuration("ServerConfig.txt");
            restartListener();
            Debug.welcome();
            // setup GUI and handle GUI events
            setupGUI = new SetupGUI(mainWindow);

            // setup query page
            setupGUI.setupQueryFields(configuration.studyTemplate);
            setupGUI.searchStudiesEvent += searchStudies;

            setupGUI.setupStudyTable(configuration.studyTemplate);
            mainWindow.queryPage.onStudyClickedEvent += searchSeries;

            // setup download page
            setupGUI.setupSeriesTable(configuration.seriesTemplate);
            mainWindow.downloadPage.onSeriesClickedEvent += downloadSeries;

            mainWindow.downloadPage.onThumbClickedEvent += onThumbClicked;

            // setup local page
            setupGUI.setupLocalStudyTable(configuration.studyTemplate);
            setupGUI.setupLocalQueryFields();
            setupGUI.searchLocalStudiesEvent += searchLocalStudies;
            setupGUI.setupLocalSeriesTable(configuration.seriesTemplate);
            mainWindow.localStudiesPage.onLocalStudyClickedEvent += searchLocalSeries;
            mainWindow.localSeriesPage.onLocalSeriesClickedEvent += showLocalSeries;
        }

        void restartListener()
        {
            System.Diagnostics.Process[] listeners = System.Diagnostics.Process.GetProcessesByName("Listener");
            if (listeners.Length != 0) foreach (var listener in listeners) listener.Kill();
            var newListener = new System.Diagnostics.Process
            {
                StartInfo = {
                    FileName = "Listener",
                    Arguments = configuration.thisNodePort.ToString()+" "+configuration.certificatePath
                }
            };
            newListener.Start();
        }

        // ----------------------------study REMOTE query-------------------------------------------------
        List<Study> studyResponses;
        void searchStudies(Study studyQuery)
        {
            studyResponses = Query.CFINDStudies(configuration,studyQuery);
            setupGUI.addStudiesToTable(studyResponses);
        }

        // series query
        List<Series> seriesResponses;
        void searchSeries(int studyNumber)
        {
            seriesResponses = Query.CFINDSeries(configuration,studyResponses[studyNumber]);
            setupGUI.addSeriesToTable(seriesResponses);
        }

        // series download
        void downloadSeries(int seriesNumber)
        {
            string seriesPath = Query.CMOVESeries(configuration,seriesResponses[seriesNumber]);
            MessageBox.Show(Debug.seriesPathCopied(seriesPath));
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
            localStudyResponses = LocalQuery.CFINDLocalStudies(configuration);
            setupGUI.addLocalStudiesToTable(localStudyResponses);
        }

        // local series
        List<Series> localSeriesResponses;
        void searchLocalSeries(int index)
        {
            localSeriesResponses = LocalQuery.CFINDLocalSeries(configuration, localStudyResponses[index]);
            setupGUI.addLocalSeriesToTable(localSeriesResponses);
            addThumbs(localSeriesResponses);
        }
        private void showLocalSeries(int index)
        {
            string fullSeriesPath = localSeriesResponses[index].getFullPath(configuration.fileDestination);
            System.Windows.Forms.Clipboard.SetDataObject(fullSeriesPath, true);
            MessageBox.Show(Debug.seriesPathCopied(fullSeriesPath));

        }

        // local sample image
        void addThumbs(List<Series> seriesResponses)
        {
            foreach (var series in seriesResponses)
            {
                var imageJpg = LocalQuery.getThumb(configuration, series);
                setupGUI.addLocalSeriesImage(seriesResponses.IndexOf(series), imageJpg);

            }

        }

    }
}
