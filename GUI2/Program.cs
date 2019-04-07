using GUI;
using PacsLibrary.LocalQuery;
using PacsLibrary.Query;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Imaging;
using PacsLibrary;

namespace GUI
{
    class Program
    {
        QueryObject queryObject = new QueryObject();

        // configure server access, init GUI, init listener
        SetupGUI setupGUI;
        internal Program(MainWindow mainWindow)
        {
            // configure server info
            restartListener();
            // setup GUI and handle GUI events
            setupGUI = new SetupGUI(mainWindow);

            // setup query page
            setupGUI.setupQueryFields(queryObject.getStudyTemplate());
            setupGUI.searchStudiesEvent += searchStudies;

            setupGUI.setupStudyTable(queryObject.getStudyTemplate());
            mainWindow.queryPage.onStudyClickedEvent += searchSeries;

            // setup download page
            setupGUI.setupSeriesTable(queryObject.getSeriesTemplate());
            mainWindow.downloadPage.onSeriesClickedEvent += downloadSeries;

            mainWindow.downloadPage.onThumbClickedEvent += onThumbClicked;

            // setup local page
            setupGUI.setupLocalStudyTable(queryObject.getStudyTemplate());
            setupGUI.setupLocalQueryFields();
            setupGUI.searchLocalStudiesEvent += searchLocalStudies;
            setupGUI.setupLocalSeriesTable(queryObject.getSeriesTemplate());
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
                    FileName = "Listener"
                }
            };
            newListener.Start();
        }

        // ----------------------------study REMOTE query-------------------------------------------------
        List<Study> studyResponses;
        void searchStudies(Study studyQuery)
        {
            studyResponses = queryObject.CFINDStudies(studyQuery);
            setupGUI.addStudiesToTable(studyResponses);
        }

        // series query
        List<Series> seriesResponses;
        void searchSeries(int studyNumber)
        {
            seriesResponses = queryObject.CFINDSeries(studyResponses[studyNumber]);
            setupGUI.addSeriesToTable(seriesResponses);
        }

        // series download
        void downloadSeries(int seriesNumber)
        {
            string seriesPath = queryObject.CMOVESeries(seriesResponses[seriesNumber]);
            copyIntoClipboard(seriesPath);
        }

        // download sample image from series
        void onThumbClicked(int seriesNumber)
        {
            BitmapImage img = queryObject.downloadSampleImage(seriesResponses[seriesNumber]);
            setupGUI.addImage(seriesNumber, img);
        }

        //--------------------------- search LOCAL studies-------------------------------------------------
        List<Study> localStudyResponses;
        void searchLocalStudies(Study studyTemplate)
        {
            localStudyResponses = queryObject.CFINDLocalStudies();
            setupGUI.addLocalStudiesToTable(localStudyResponses);
        }

        // local series
        List<Series> localSeriesResponses;
        void searchLocalSeries(int index)
        {
            localSeriesResponses = queryObject.CFINDLocalSeries(localStudyResponses[index]);
            setupGUI.addLocalSeriesToTable(localSeriesResponses);
            addThumbs(localSeriesResponses);
        }
        private void showLocalSeries(int index)
        {
            string seriesPath = localSeriesResponses[index].getFullPath(queryObject.getLocalFilesDestination());
            copyIntoClipboard(seriesPath);
        }

        // local sample image
        void addThumbs(List<Series> seriesResponses)
        {
            foreach (var series in seriesResponses)
            {
                var imageJpg = queryObject.getThumb(series);
                setupGUI.addLocalSeriesImage(seriesResponses.IndexOf(series), imageJpg);

            }

        }

        void copyIntoClipboard(string myString)
        {
            System.Windows.Forms.Clipboard.SetDataObject(myString, true);
            MessageBox.Show("Full series path: " + Environment.NewLine + myString +
               Environment.NewLine + "Copied into clipboard");
        }
    }
}
