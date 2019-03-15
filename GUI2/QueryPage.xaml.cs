using System.Windows;
using System.Windows.Controls;

namespace GUI2
{
    public partial class QueryPage : Page
    {
        public QueryPage()
        {
            InitializeComponent();
        }

        public delegate void OnStudyClickedEvent(int index);
        public event OnStudyClickedEvent onStudyClickedEvent;
        private void onStudyClicked(object sender, RoutedEventArgs e)
        {
            // find the number of the clicked study
            ListViewItem item = sender as ListViewItem;
            if (item != null && item.IsSelected)
            {
                var dyn = ((FrameworkElement)item).DataContext as dynamic;
                var list = listView.Items;
                int index = list.IndexOf(dyn);

                // raise the event
                if (sender != null && item.IsSelected) onStudyClickedEvent(index);
            }
        }
    }
}
