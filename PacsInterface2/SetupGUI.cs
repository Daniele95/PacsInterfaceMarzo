using Dicom;
using Dicom.Media;
using GUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace PacsInterface
{
    class SetupGUI
    {
        // setup guest user interface
        MainWindow mainWindow;
        QueryPage queryPage;
        DownloadPage downloadPage;
        LocalPage localPage;
        internal SetupGUI(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            queryPage = mainWindow.queryPage;
            downloadPage = mainWindow.downloadPage;
            localPage = mainWindow.localPage;
        }

        // setup query page
        Study studyTemplate;
        List<Control> queryFields = new List<Control>();
        internal void setupQueryFields(Study studyTemplate)
        {
            this.studyTemplate = studyTemplate;
            for (int i = 0; i < studyTemplate.Count; i++)
            {
                var textBlock = new TextBlock();
                textBlock.Text = studyTemplate[i].name;
                Grid.SetRow(textBlock, 0);
                Grid.SetColumn(textBlock, i);
                queryPage.grid.Children.Add(textBlock);
                queryPage.grid.ColumnDefinitions.Add(new ColumnDefinition());

                if (!studyTemplate[i].name.Contains("Date"))
                {
                    var textBox = new TextBox();
                    queryFields.Add(textBox);
                    textBox.KeyDown += onKeyDownHandler;
                    Grid.SetRow(textBox, 1);
                    Grid.SetColumn(textBox, i);
                    queryPage.grid.Children.Add(textBox);
                }
                else
                {
                    var datePickerMin = new DatePicker();
                    queryFields.Add(datePickerMin);
                    Grid.SetRow(datePickerMin, 1);
                    Grid.SetColumn(datePickerMin, i);
                    queryPage.grid.Children.Add(datePickerMin);

                    var datePickerMax = new DatePicker();
                    queryFields.Add(datePickerMax);
                    Grid.SetRow(datePickerMax, 2);
                    Grid.SetColumn(datePickerMax, i);
                    queryPage.grid.Children.Add(datePickerMax);
                }
            }
        }
        internal void setupStudyTable(Study studyTemplate)
        {
            foreach (var studyParameter in studyTemplate)
                queryPage.gridView.Columns.Add(new GridViewColumn
                {
                    Header = studyParameter.name,
                    DisplayMemberBinding = new Binding(studyParameter.name),
                    Width = 100
                });
        }
        internal void addStudiesToTable(List<Study> studyResponses)
        {
            queryPage.listView.Items.Clear();
            foreach (Study studyResponse in studyResponses)
                queryPage.listView.Items.Add(studyResponse.getDynamic());
        }

        // get query parameters specified by the user on 'enter' key
        internal delegate void SearchStudiesEvent(Study study);
        internal SearchStudiesEvent searchStudiesEvent;
        void onKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                int i = 0;
                foreach (var studyParameter in studyTemplate)
                {
                    if (queryFields[i].GetType() == typeof(TextBox))
                    {
                        studyParameter.value = (queryFields[i] as TextBox).Text;
                    }
                    if (queryFields[i].GetType() == typeof(DatePicker))
                    {
                        var controlMin = (queryFields[i] as DatePicker).SelectedDate;
                        var controlMax = (queryFields[i + 1] as DatePicker).SelectedDate;
                        if (controlMin != null && controlMax != null)
                            studyParameter.value = controlMin.Value.ToString("yyyyMMdd") + "-" + controlMax.Value.ToString("yyyyMMdd");
                        else if (controlMin == null && controlMax != null)
                            studyParameter.value = controlMax.Value.ToString("yyyyMMdd") + "-" + controlMax.Value.AddDays(1).ToString("yyyyMMdd");
                        else if (controlMin != null && controlMax == null)
                            studyParameter.value = controlMin.Value.ToString("yyyyMMdd") + "-" + controlMin.Value.AddDays(1).ToString("yyyyMMdd");
                        else if (controlMin == null && controlMax == null)
                            studyParameter.value = new DateTime(1900, 1, 1).ToString("yyyyMMdd") + "-" + DateTime.Now.ToString("yyyyMMdd");
                        i++;
                    }
                    i++;
                }
                searchStudiesEvent(studyTemplate);
            }
        }

        // setup download page
        internal void setupSeriesTable(Series seriesTemplate)
        {
            foreach (var seriesParameter in seriesTemplate)
                downloadPage.gridView.Columns.Add(new GridViewColumn
                {
                    Header = seriesParameter.name,
                    DisplayMemberBinding = new Binding(seriesParameter.name),
                    Width = 100
                });
            downloadPage.gridView.Columns.Add(new GridViewColumn
            {
                Header = "Image",
                CellTemplate = downloadPage.FindResource("iconTemplate") as DataTemplate
            });
        }
        internal void addSeriesToTable(List<Series> seriesResponses)
        {
            mainWindow.frame.Navigate(downloadPage);
            downloadPage.listView.Items.Clear();
            foreach (Series seriesResponse in seriesResponses)
                downloadPage.listView.Items.Add(seriesResponse.getDynamic());
        }
        internal void addImage(int seriesNumber, BitmapImage img)
        {
            var dyn = downloadPage.listView.Items[seriesNumber] as dynamic;
            dyn.Image = img;
        }

        // setup local page
        internal void setupLocalTable(Study studyTemplate)
        {
            foreach (var parameter in studyTemplate)
                localPage.gridView.Columns.Add(new GridViewColumn
                {
                    Header = parameter.name,
                    DisplayMemberBinding = new Binding(parameter.name),
                    Width = 100
                });
        }
        internal void showLocal()
        {
            localPage.listView.Items.Clear();
            mainWindow.frame.Navigate(localPage);

            string fileDestination = File.ReadAllLines("ServerConfig.txt")[6];
            string dicomDirPath = Path.Combine(fileDestination, "DICOMDIR");

            using (var fileStream = new FileStream(dicomDirPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                var dicomDir = DicomDirectory.Open(fileStream);
                if (dicomDir != null)
                    foreach (var patientRecord in dicomDir.RootDirectoryRecordCollection)
                    {
                        foreach (var studyRecord in patientRecord.LowerLevelDirectoryRecordCollection)
                        {
                            studyRecord.Add(DicomTag.PatientName, patientRecord.GetSingleValue<string>(DicomTag.PatientName));
                            studyRecord.Add(DicomTag.PatientID,patientRecord.GetSingleValue<string>(DicomTag.PatientID));
                            Study myStudy = new Study(studyRecord, studyTemplate);
                            localPage.listView.Items.Add(myStudy.getDynamic());
                        }
                    }
                 printDataset(dicomDir);
            }
        }

        void printDataset(DicomDirectory dicomDir2)
        {
            foreach (var patientRecord in dicomDir2.RootDirectoryRecordCollection)
            {
                Console.WriteLine(
                    "Patient: {0} ({1})",
                    patientRecord.GetSingleValue<string>(DicomTag.PatientName),
                    patientRecord.GetSingleValue<string>(DicomTag.PatientID));


                foreach (var studyRecord in patientRecord.LowerLevelDirectoryRecordCollection)
                {
                    Console.WriteLine("\tStudy: {0}", studyRecord.GetSingleValue<string>(DicomTag.StudyInstanceUID));

                    foreach (var seriesRecord in studyRecord.LowerLevelDirectoryRecordCollection)
                    {
                        Console.WriteLine("\t\tSeries: {0}", seriesRecord.GetSingleValue<string>(DicomTag.SeriesInstanceUID));


                    }
                }
            }
        }
    }
}