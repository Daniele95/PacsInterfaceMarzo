using GUI;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using static PacsInterface.QueryParams;

namespace PacsInterface
{
    class SetupGUI
    {
        public static void setupStudyTable(MainWindow mainWindow)
        {
            // setup table according to required properties
            PropertyInfo[] properties = typeof(StudyQueryOut).GetProperties();
            foreach (PropertyInfo property in properties)
            {
                // aggiungo la colonna
                // e riduco a 0 la visibilità delle colonne nascoste
                string contents = File.ReadAllText("StudyColumnsToShow.txt");
                bool isHidden = !contents.Contains(property.Name);
                int colWidth = isHidden ? 1 : 0;
                mainWindow.queryPage.gridView.Columns.Add(new GridViewColumn
                {
                    Header = property.Name,
                    DisplayMemberBinding = new Binding(property.Name),
                    Width = 100 * (1 - colWidth)
                });
            }
        }

        public static void setupSeriesTable(DownloadPage downloadPage)
        {

            // setup table according to required properties
            PropertyInfo[] properties = typeof(SeriesQueryOut).GetProperties();
            foreach (PropertyInfo property in properties)
            {
                // aggiungo la colonna
                // e riduco a 0 la visibilità delle colonne nascoste
                string contents = File.ReadAllText("SeriesColumnsToShow.txt");
                bool isHidden = !contents.Contains(property.Name);
                int colWidth = isHidden ? 1 : 0;
                downloadPage.dgUsers.Columns.Add(new DataGridTextColumn
                {
                    Header = property.Name,
                    Binding = new Binding(property.Name),
                    Width = 100 * (1 - colWidth)
                });
            }
            downloadPage.dgUsers.Columns.Add(new DataGridTemplateColumn
            {
                Header = "Image",
                CellTemplate = downloadPage.FindResource("iconTemplate") as DataTemplate
            });
        }

        public static void addElementToSeriesTable(DownloadPage downloadPage, SeriesQueryOut response)
        {
            dynamic myItem = new System.Dynamic.ExpandoObject();
            IDictionary<string, object> myItemValues;

            PropertyInfo[] property = typeof(SeriesQueryOut).GetProperties();
            for (int j = 0; j < property.Length; j++)
            {
                myItemValues = (IDictionary<string, object>)myItem;
                myItemValues[property[j].Name] = property[j].GetValue(response);
            }
         //   myItem.Image = image;
            downloadPage.dgUsers.Items.Add(myItem);
        }

        public static void addImage(DownloadPage downloadPage,FrameworkElement sender, BitmapImage image)
        {
            var dyn = ((FrameworkElement)sender).DataContext as dynamic; 
            dyn.Image = image;
        }
        // show or hide button
        /*
        void ShowHideDetails(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("ciao");
            for (var vis = sender as Visual; vis != null; vis = VisualTreeHelper.GetParent(vis) as Visual)
                if (vis is DataGridRow)
                {
                    var row = (DataGridRow)vis;
                    row.DetailsVisibility =
                    row.DetailsVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                    break;
                }
        }*/
    }
}
