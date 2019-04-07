using GUI;
using PacsLibrary.Query;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace GUI
{
    class SetupGUI
    {
        // setup guest user interface
        MainWindow mainWindow;
        QueryPage queryPage;
        DownloadPage downloadPage;
        LocalStudiesPage localStudiesPage;
        LocalSeriesPage localSeriesPage;
        internal SetupGUI(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            queryPage = mainWindow.queryPage;
            downloadPage = mainWindow.downloadPage;
            localStudiesPage = mainWindow.localStudiesPage;
            localSeriesPage = mainWindow.localSeriesPage;
            queryPage.executeQueryButton.Click += onButtonPressedHandler;
        }


        //----------------------------------------- setup query page----------------------------------------
        Study studyTemplate;
        List<Control> queryFields = new List<Control>();
        internal void setupStudyTable(Study studyTemplate)
        {
            foreach (var studyParameter in studyTemplate)
                if (studyParameter.visible == true)
                    queryPage.gridView.Columns.Add(new GridViewColumn
                    {
                        Header = studyParameter.name,
                        DisplayMemberBinding = new Binding(studyParameter.name),
                        Width = 100
                    });
        }

        // setup query fields and query event
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
        internal delegate void SearchStudiesEvent(Study study);
        internal SearchStudiesEvent searchStudiesEvent;
        void onKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                fillQueryFields();
            }
        }
        private void onButtonPressedHandler(object sender, RoutedEventArgs e)
        {
            fillQueryFields();
        }
        void fillQueryFields()
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

        internal void addStudiesToTable(List<Study> studyResponses)
        {
            queryPage.listView.Items.Clear();
            foreach (Study studyResponse in studyResponses)
                queryPage.listView.Items.Add(studyResponse.getDynamic());
        }

        // setup download page--------------------------------------
        internal void setupSeriesTable(Series seriesTemplate)
        {
            foreach (var seriesParameter in seriesTemplate)
                if (seriesParameter.visible == true)
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


        //----------------------------------------- setup local studies page-----------------------------------------
        internal void setupLocalStudyTable(Study studyTemplate)
        {
            foreach (var parameter in studyTemplate)
                if (parameter.visible == true)
                    localStudiesPage.gridView.Columns.Add(new GridViewColumn
                    {
                        Header = parameter.name,
                        DisplayMemberBinding = new Binding(parameter.name),
                        Width = 100
                    });
        }
        internal void setupLocalQueryFields()
        {
            var button = new Button { Content = "Show" };
            button.Click += onLocalSearchStudies;
            localStudiesPage.grid.Children.Add(button);
        }

        internal delegate void SearchLocalStudiesEvent(Study study);
        internal SearchLocalStudiesEvent searchLocalStudiesEvent;
        internal void onLocalSearchStudies(object sender, RoutedEventArgs e)
        {
            searchLocalStudiesEvent(studyTemplate);
        }

        internal void addLocalStudiesToTable(List<Study> studyResponses)
        {
            localStudiesPage.listView.Items.Clear();
            mainWindow.frame.Navigate(localStudiesPage);

            foreach (var myStudy in studyResponses)
                localStudiesPage.listView.Items.Add(myStudy.getDynamic());
        }

        // setup local series page------------------------------------------
        internal void setupLocalSeriesTable(Series seriesTemplate)
        {
            foreach (var seriesParameter in seriesTemplate)
                if (seriesParameter.visible == true)
                    localSeriesPage.gridView.Columns.Add(new GridViewColumn
                    {
                        Header = seriesParameter.name,
                        DisplayMemberBinding = new Binding(seriesParameter.name),
                        Width = 100
                    });
            localSeriesPage.gridView.Columns.Add(new GridViewColumn
            {
                Header = "Image",
                CellTemplate = localSeriesPage.FindResource("iconTemplate") as DataTemplate
            });
        }
        internal void addLocalSeriesToTable(List<Series> seriesResponses)
        {
            mainWindow.frame.Navigate(localSeriesPage);
            localSeriesPage.listView.Items.Clear();
            foreach (Series seriesResponse in seriesResponses)
                localSeriesPage.listView.Items.Add(seriesResponse.getDynamic());
        }
        internal void addLocalSeriesImage(int seriesNumber, BitmapImage img)
        {
            var dyn = localSeriesPage.listView.Items[seriesNumber] as dynamic;
            dyn.Image = img;
        }
    }
}