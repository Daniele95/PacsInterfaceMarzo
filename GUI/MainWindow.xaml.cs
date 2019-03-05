using System.Windows;

namespace GUI
{
    public partial class MainWindow : Window
    {
        public QueryPage queryPage = new QueryPage();
        public DownloadPage downloadPage = new DownloadPage();

        public MainWindow()
        {
            InitializeComponent();
        }

        public void QueryPageButton_Click(object sender, RoutedEventArgs e)
        {
            frame.Navigate(queryPage);
        }

        public void DownloadPageButton_Click(object sender, RoutedEventArgs e)
        {
            frame.Navigate(downloadPage);
        }

    }
}
