using System.Windows;
using System.Windows.Controls;

namespace GUI
{
    public partial class LocalStudiesPage : Page
    {
        public LocalStudiesPage()
        {
            InitializeComponent();
        }

        public delegate void OnLocalStudyClickedEvent(int index);
        public event OnLocalStudyClickedEvent onLocalStudyClickedEvent;
        private void onLocalStudyClicked(object sender, RoutedEventArgs e)
        {
            // find the number of the clicked study
            ListViewItem item = sender as ListViewItem;
            if (item != null && item.IsSelected)
            {
                var dyn = ((FrameworkElement)item).DataContext as dynamic;
                var list = listView.Items;
                int index = list.IndexOf(dyn);

                // raise the event
                if (sender != null && item.IsSelected) onLocalStudyClickedEvent(index);
            }
        }

    }
}
