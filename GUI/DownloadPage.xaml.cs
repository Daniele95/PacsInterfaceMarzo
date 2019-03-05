using System.Windows;
using System.Windows.Controls;

namespace GUI
{
    public class DataItem
    {
        public string Column1 { get; set; }
        public string Column2 { get; set; }
        public string Column3 { get; set; }
        public string Column4 { get; set; }
    }
    public partial class DownloadPage : Page
    {
        public DownloadPage()
        {
            InitializeComponent();
        }

        public delegate void Series_ClickEvent(ListViewItem sender);
        public event Series_ClickEvent series_ClickEvent;

        private void Series_Clicked(object sender, RoutedEventArgs e)
        {
            ListViewItem item = sender as ListViewItem;
            if (item != null && item.IsSelected)
            {
                series_ClickEvent(item);
            }
        }
        public delegate void Thumb_ClickEvent(FrameworkElement s);
        public event Thumb_ClickEvent thumb_ClickEvent;

        private void button_download_image(object sender, RoutedEventArgs e)
        {

            //Get the button that raised the event
            Button btn = (Button)sender;

            //Get the row that contains this button
            var asd = ((FrameworkElement)sender).DataContext as dynamic;

            thumb_ClickEvent(sender as FrameworkElement);

        }
    }
}
