using System.Windows;
using System.Windows.Controls;

namespace GUI
{
    public partial class LocalSeriesPage : Page
    {
        public LocalSeriesPage()
        {
            InitializeComponent();
        }

        public delegate void OnLocalSeriesClickedEvent(int index);
        public event OnLocalSeriesClickedEvent onLocalSeriesClickedEvent;
        private void onLocalSeriesClicked(object sender, RoutedEventArgs e)
        {
            // find the number of the series clicked 
            ListViewItem item = sender as ListViewItem;
            if (item != null && item.IsSelected)
            {
                var dyn = ((FrameworkElement)item).DataContext as dynamic;
                var list = listView.Items;
                int index = list.IndexOf(dyn);

                // raise the event
                if (sender != null && item.IsSelected) onLocalSeriesClickedEvent(index);
            }
        }

    }
}
