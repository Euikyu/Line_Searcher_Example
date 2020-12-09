using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace Line_Searcher_Example
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            //EdgeDetectCollection = new ObservableCollection<UserControls.Item>();
            //EdgeDetectCollection.Add(new UserControls.Item { SearchLength = 100, ProjectionLength = 10 });
            //EdgeDetectCollection.Add(new UserControls.Item { SearchLength = 100, ProjectionLength = 10 });
            //EdgeDetectCollection.Add(new UserControls.Item { SearchLength = 100, ProjectionLength = 10 });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Main_LineSearcher.CaliperCount++;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Main_LineSearcher.CaliperCount--;
        }
    }
}
