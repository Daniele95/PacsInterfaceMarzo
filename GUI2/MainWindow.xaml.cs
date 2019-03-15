using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GUI2
{
    public partial class MainWindow : Window
    {

        public QueryPage queryPage = new QueryPage();
        public DownloadPage downloadPage = new DownloadPage();
        public LocalPage localPage = new LocalPage();

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
            frame.Navigate(localPage);
        }
    }
}