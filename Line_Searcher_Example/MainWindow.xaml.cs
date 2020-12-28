using Line_Searcher_Example.Inspect;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
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
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private BitmapSource m_OriginImage;
        private DrawingImage m_OverlayImage;
        private string m_FilePath;
        private Bitmap m_CurrentBitmap;

        public BitmapSource OriginImageSource
        {
            get { return m_OriginImage; }
            set
            {
                m_OriginImage = value;
                this.RaisePropertyChanged("OriginImageSource");
                ImageWidth = m_OriginImage.PixelWidth;
                ImageHeight = m_OriginImage.PixelHeight;
                this.RaisePropertyChanged("ImageWidth");
                this.RaisePropertyChanged("ImageHeight");
            }
        }
        public DrawingImage OverlayImageSource
        {
            get { return m_OverlayImage; }
            set
            {
                m_OverlayImage = value;
                this.RaisePropertyChanged("OverlayImageSource");
            }
        }
        public double ImageWidth { get; private set; }
        public double ImageHeight { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propName)
        {
            if (PropertyChanged != null) PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Main_LineSearcher.CaliperCount++;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Main_LineSearcher.CaliperCount--;
        }

        private void ImageOpen_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Bitmap images. (*.bmp)|*.bmp"
            };

            if ((bool)dialog.ShowDialog())
            {
                System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(dialog.FileName);
                if (bmp.PixelFormat != System.Drawing.Imaging.PixelFormat.Format8bppIndexed)
                {

                }
                else
                {
                    m_CurrentBitmap = bmp;
                    var data = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, bmp.PixelFormat);

                    OriginImageSource = BitmapSource.Create(data.Width, data.Height, bmp.HorizontalResolution, bmp.VerticalResolution, PixelFormats.Gray8, null, data.Scan0, data.Stride * data.Height, data.Stride);
                    OriginImageSource.Freeze();


                    OverlayImageSource = null;
                    bmp.UnlockBits(data);
                }
                //m_FilePath = dialog.FileName;
                //BitmapImage bi = new BitmapImage(new Uri(m_FilePath));
                //bi.Freeze();
                //OriginImageSource = bi;
                //OverlayImageSource = null;





                //m_OriginImage = bi;
                //m_OverlayImage = null;

                //this.RaisePropertyChanged("OriginImageSource");
                //this.RaisePropertyChanged("OverlayImageSource");

                //ImageWidth = m_OriginImage.Width;
                //ImageHeight = m_OriginImage.Height;
                //this.RaisePropertyChanged("ImageWidth");
                //this.RaisePropertyChanged("ImageHeight");
            }

        }

        private void Run_Click(object sender, RoutedEventArgs e)
        {
            OverlayImageSource = (Main_LineSearcher.EdgeDetectCollection as UserControls.EdgeCollection).DetectLine(m_CurrentBitmap);
            //this.RaisePropertyChanged("OverlayImageSource");
        }
    }
}
