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
            dataGrid.IsReadOnly = true;
        }

        public delegate void Series_ClickEvent(DataGridRow sender);
        public event Series_ClickEvent series_ClickEvent;
        private void Series_Clicked(object sender, RoutedEventArgs e)
        {
            DataGridRow item = sender as DataGridRow;
            if (item != null && item.IsSelected)
            {
                series_ClickEvent(item);
            }
        }

        public delegate void Thumb_ClickEvent(FrameworkElement s);
        public event Thumb_ClickEvent thumb_ClickEvent;
        private void Thumb_Clicked(object sender, RoutedEventArgs e)
        {
            //Get the button that raised the event
            Button btn = (Button)sender;
            btn.IsEnabled = false;
            btn.Background= Brushes.Transparent;
            btn.BorderBrush = Brushes.Transparent;
            var stackPanel = (btn.FindName("stackPanel") as StackPanel);
            stackPanel.Children.Remove(stackPanel.FindName("buttonLabel") as Label);

            //Get the row that contains this button
            var asd = ((FrameworkElement)sender).DataContext as dynamic;

            thumb_ClickEvent(sender as FrameworkElement);

        }
    }
}
