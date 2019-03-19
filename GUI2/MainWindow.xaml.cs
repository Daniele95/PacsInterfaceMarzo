using System.Windows;

namespace GUI
{
    public partial class MainWindow : Window
    {

        public QueryPage queryPage = new QueryPage();
        public DownloadPage downloadPage = new DownloadPage();
        public LocalStudiesPage localStudiesPage = new LocalStudiesPage();
        public LocalSeriesPage localSeriesPage = new LocalSeriesPage();

        public MainWindow()
        {
            InitializeComponent();
            frame.Navigate(queryPage);
        }

        private void queryPageButton_Click(object sender, RoutedEventArgs e)
        {
            frame.Navigate(queryPage);
        }

        private void downloadPageButton_Click(object sender, RoutedEventArgs e)
        {
            frame.Navigate(downloadPage);
        }

        private void localPageButton_Click(object sender, RoutedEventArgs e)
        {
            frame.Navigate(localStudiesPage);
        }

        private void localSeriesButton_Click(object sender, RoutedEventArgs e)
        {
            frame.Navigate(localSeriesPage);
        }
    }
}