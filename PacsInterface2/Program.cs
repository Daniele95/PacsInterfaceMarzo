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
            localStudyResponses = LocalQuery.searchLocalStudies(configuration, "StudyColumnsToShow.txt");
            setupGUI.addLocalStudiesToTable(localStudyResponses);
        }

        // local series
        List<Series> localSeriesResponses;
        void searchLocalSeries(int index)
        {
            localSeriesResponses = LocalQuery.searchLocalSeries(configuration, localStudyResponses[index],"SeriesColumnsToShow.txt");
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
                var imageJpg = LocalQuery.getThumb(configuration, series);
                setupGUI.addLocalSeriesImage(seriesResponses.IndexOf(series), imageJpg);

            }

        }

    }
}
