using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GUI
{
    public partial class DownloadPage : Page
    {
        public DownloadPage()
        {
            InitializeComponent();
        }

        public delegate void OnSeriesClickedEvent(int index);
        public event OnSeriesClickedEvent onSeriesClickedEvent;
        private void onSeriesClicked(object sender, RoutedEventArgs e)
        {
            // find the number of the series clicked 
            ListViewItem item = sender as ListViewItem;
            if (item != null && item.IsSelected)
            {
                var dyn = ((FrameworkElement)item).DataContext as dynamic;
                var list = listView.Items;
                int index = list.IndexOf(dyn);

                // raise the event
                if (sender != null && item.IsSelected) onSeriesClickedEvent(index);
            }
        }

        public delegate void OnThumbClickedEvent(int index);
        public event OnThumbClickedEvent onThumbClickedEvent;
        private void onThumbClicked(object sender, RoutedEventArgs e)
        {
            //Get the button that raised the event
            Button btn = (Button)sender;
            btn.IsEnabled = false;
            btn.Background = Brushes.Transparent;
            btn.BorderBrush = Brushes.Transparent;
            var stackPanel = (btn.FindName("stackPanel") as StackPanel);
            stackPanel.Children.Remove(stackPanel.FindName("buttonLabel") as Label);

            //Get the row that contains this button
            var dyn = ((FrameworkElement)btn).DataContext as dynamic;
            var list = listView.Items;
            int index = list.IndexOf(dyn);

            // raise the event
            onThumbClickedEvent(index);
        }
    }
}
