using System.Windows;
using System.Windows.Controls;

namespace GUI
{
    public partial class QueryPage : Page
    {
        public QueryPage()
        {
            InitializeComponent();            
        }
        
        public delegate void Study_ClickEvent(ListViewItem sender);
        public event Study_ClickEvent study_ClickEvent;

        private void Study_Clicked(object sender, RoutedEventArgs e)
        {
            ListViewItem item = sender as ListViewItem;
            if (item != null && item.IsSelected)
            {
                study_ClickEvent(item);
            }
        }

        private void listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
