using Line_Searcher_Example.Inspect;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

namespace Line_Searcher_Example.UserControls
{
    /// <summary>
    /// LineSearcher.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LineSearcher : UserControl, INotifyPropertyChanged
    {

        #region Fields
        private readonly object m_MoveLock = new object();

        private bool m_IsCaptured;
        private Point m_LastMovePoint;

        private double m_LineOriginX;
        private double m_LineOriginY;
        private double m_LineWidth;
        private double m_LineThickness;

        private RotateTransform m_LineRotateTransform;
        private double m_Radian;

        private EdgeCollection m_EdgeDetectCollection;
        #endregion

        #region Properties
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propName)
        {
            if (PropertyChanged != null) PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propName));
        }
        #region Common Properties
        public RotateTransform LineRotateTransform
        {
            get { return m_LineRotateTransform; }
            set
            {
                m_LineRotateTransform = value;
                RaisePropertyChanged("RectRotateTransform");
            }
        }
        public IReadOnlyList<EdgeDetctor> EdgeDetectCollection
        {
            get { return m_EdgeDetectCollection; }
        }
        
        #endregion

        #region Dependency Properties
        public static readonly DependencyProperty CaliperCountProperty =
            DependencyProperty.Register("CaliperCount", typeof(int), typeof(LineSearcher));

        public static readonly DependencyProperty HalfPixelCountProperty =
            DependencyProperty.Register("HalfPixelCount", typeof(uint), typeof(LineSearcher));

        public static readonly DependencyProperty ContrastThresholdProperty =
            DependencyProperty.Register("ContrastThreshold", typeof(uint), typeof(LineSearcher));

        public static readonly DependencyProperty EdgeDirectionProperty =
            DependencyProperty.Register("EdgeDirection", typeof(EDirection), typeof(LineSearcher));

        public static readonly DependencyProperty SearchLengthProperty =
                    DependencyProperty.Register("SearchLength", typeof(double), typeof(LineSearcher));

        public static readonly DependencyProperty ProjectionLengthProperty =
                    DependencyProperty.Register("ProjectionLength", typeof(double), typeof(LineSearcher));

        public static readonly DependencyProperty OriginXProperty =
                    DependencyProperty.Register("OriginX", typeof(double), typeof(LineSearcher));

        public static readonly DependencyProperty OriginYProperty =
            DependencyProperty.Register("OriginY", typeof(double), typeof(LineSearcher));

        public static readonly DependencyProperty RotationProperty =
            DependencyProperty.Register("Rotation", typeof(double), typeof(LineSearcher));
        
        public static readonly DependencyProperty RadianProperty =
            DependencyProperty.Register("Radian", typeof(double), typeof(LineSearcher));


        public int CaliperCount
        {
            get { return (int)GetValue(CaliperCountProperty); }
            set
            {
                if (value < 3) return; 
                SetValue(CaliperCountProperty, value);
                if(m_EdgeDetectCollection.Count != value)
                {
                    var sub = m_EdgeDetectCollection.Count - value;
                    if(sub > 0)
                    {
                        for(int i = 0; i < sub; i++)
                        {
                            m_EdgeDetectCollection.RemoveAt(0);
                        }
                    }
                    else
                    {
                        for(int i = sub; i < 0; i++)
                        {
                            m_EdgeDetectCollection.Add(null);
                        }
                    }
                }
                if (this.Width != double.NaN || this.Width != 0) this.UpdateCaliper();
                RaisePropertyChanged("CaliperCount");
            }
        }

        public uint ContrastThreshold
        {
            get { return (uint)GetValue(ContrastThresholdProperty); }
            set
            {
                if(value < 1) SetValue(ContrastThresholdProperty, 1);
                else SetValue(ContrastThresholdProperty, value);
                if (this.Width != double.NaN || this.Width != 0) this.UpdateCaliper();
                RaisePropertyChanged("ContrastThreshold");
            }
        }
        public uint HalfPixelCount
        {
            get { return (uint)GetValue(HalfPixelCountProperty); }
            set
            {
                if (value < 1) SetValue(HalfPixelCountProperty, 1);
                else SetValue(HalfPixelCountProperty, value);
                if (this.Width != double.NaN || this.Width != 0) this.UpdateCaliper();
                RaisePropertyChanged("HalfPixelCount");
            }
        }
        public EDirection EdgeDirection
        {
            get { return (EDirection)GetValue(EdgeDirectionProperty); }
            set
            {
                SetValue(EdgeDirectionProperty, value);
                if (this.Width != double.NaN || this.Width != 0) this.UpdateCaliper();
                RaisePropertyChanged("EdgeDirection");
            }
        }
        public double SearchLength
        {
            get { return (double)GetValue(SearchLengthProperty); }
            set
            {
                SetValue(SearchLengthProperty, value);
                if (this.Width != double.NaN || this.Width != 0) this.UpdateCaliper();
                RaisePropertyChanged("SearchLength");
            }
        }
        public double ProjectionLength
        {
            get { return (double)GetValue(ProjectionLengthProperty); }
            set
            {
                SetValue(ProjectionLengthProperty, value);
                if (this.Width != double.NaN || this.Width != 0) this.UpdateCaliper(); this.UpdateCaliper();
                RaisePropertyChanged("ProjectionLength");
            }
        }

        public double OriginX
        {
            get { return (double)GetValue(OriginXProperty); }
            set
            {
                SetValue(OriginXProperty, value);
                if (!m_IsCaptured && (this.Width != double.NaN || this.Width != 0)) this.UpdateCaliper();
            }
        }
        public double OriginY
        {
            get { return (double)GetValue(OriginYProperty); }
            set
            {
                SetValue(OriginYProperty, value);
                if (!m_IsCaptured && (this.Width != double.NaN || this.Width != 0)) this.UpdateCaliper();
            }
        }

        public double Rotation
        {
            get
            {
                if (m_Radian != 0) return m_Radian * 180 / Math.PI;
                return (double)GetValue(RotationProperty);
            }
            set
            {
                m_Radian = value * (Math.PI / 180);
                SetValue(RadianProperty, m_Radian);
                RotateTransform t = LineRotateTransform as RotateTransform;
                if (t != null)
                {
                    t.Angle = value;
                }
                else
                {
                    LineRotateTransform = new RotateTransform(value, this.Width / 2, m_LineThickness / 2);
                }
                RaisePropertyChanged("LineRotateTransform");
                SetValue(RotationProperty, value);
                if (!m_IsCaptured && (this.Width != double.NaN || this.Width != 0)) this.UpdateCaliper();
            }
        }

        public double Radian
        {
            get
            {
                if (m_Radian != 0) return m_Radian;
                return (double)GetValue(RadianProperty);
            }
            set
            {
                SetValue(RadianProperty, value);
                m_Radian = value;
                var deg = value * (180 / Math.PI);
                RotateTransform t = LineRotateTransform as RotateTransform;
                if (t != null)
                {
                    t.Angle = deg;
                }
                else
                {
                    LineRotateTransform = new RotateTransform(deg, this.Width / 2, m_LineThickness / 2);
                }
                RaisePropertyChanged("LineRotateTransform");
                SetValue(RotationProperty, deg);
                if (!m_IsCaptured && (this.Width != double.NaN || this.Width != 0)) this.UpdateCaliper();
            }
        }
        #endregion

        #endregion

        public LineSearcher()
        {
            InitializeComponent();
            DataContext = this;
            m_EdgeDetectCollection = new EdgeCollection();
        }

        #region Methods
        private void UpdateCaliper()
        {
            m_LineWidth = this.Width;
            m_LineOriginX = this.OriginX;
            m_LineOriginY = this.OriginY;
            
            var caliperCenter = this.Width / CaliperCount;

            for (int i = 0; i < m_EdgeDetectCollection.Count; i++)
            {
                var edge = new EdgeDetctor(new CvsRectangleAffine
                {
                    LeftTopX = (int)((i + 1) * caliperCenter - ProjectionLength / 2),
                    LeftTopY = (int)(SearchLength / 2),
                    Width = (int)ProjectionLength,
                    Height = (int)SearchLength,
                    Angle = (int)Rotation
                }, ContrastThreshold, HalfPixelCount, EdgeDirection);
                m_EdgeDetectCollection[i] = edge;
            }
        }

        private Point GetCenter()
        {
            return new Point(this.OriginX + this.Width / 2, this.OriginY + this.Height / 2);
        }
        private void MoveRotateOrigin(string controlName)
        {
            if (LineRotateTransform.CenterX == 0 && controlName.Contains("Start"))
            {
                //새로 회전축이 될 좌표 구하기
                //그 좌표에서 0도가 될 때의 원점 좌표 구하기
                
                var newCenter = this.GetPointByRotation(new Point(this.Width, 0), m_Radian, new Point()) - new Point(-(this.OriginX), -(this.OriginY + m_LineThickness / 2));
                
                LineRotateTransform = new RotateTransform(Rotation, this.Width, m_LineThickness / 2);
                RaisePropertyChanged("LineRotateTransform");

                this.OriginX = newCenter.X - LineRotateTransform.CenterX;
                this.OriginY = newCenter.Y - LineRotateTransform.CenterY;

            }
            else if (LineRotateTransform.CenterX != 0 && controlName.Contains("End"))
            {
                var newCenter = this.GetPointByRotation(new Point(-this.Width, 0), m_Radian, new Point()) - new Point(-(this.OriginX + this.Width), -(this.OriginY + m_LineThickness / 2));
                
                LineRotateTransform = new RotateTransform(Rotation, 0, m_LineThickness / 2);
                RaisePropertyChanged("LineRotateTransform");

                this.OriginX = newCenter.X - LineRotateTransform.CenterX;
                this.OriginY = newCenter.Y - LineRotateTransform.CenterY;
            }
        }
        private Point GetPointByRotation(Point rotatePoint, double rad, Point center)
        {
            return new Point(Math.Cos(rad) * rotatePoint.X - Math.Sin(rad) * rotatePoint.Y - center.X, Math.Sin(rad) * rotatePoint.X + Math.Cos(rad) * rotatePoint.Y - center.Y);
        }
        #endregion

        #region Events
        private void LineSearcher_Loaded(object sender, RoutedEventArgs e)
        {
            CaliperCount = 3;
            EdgeDirection = EDirection.Any;
            HalfPixelCount = 2;
            ContrastThreshold = 2;
            ProjectionLength = 30;
            SearchLength = 100;
            m_LineThickness = this.MinHeight;
            Radian = 0;
        }

        private void Line_MouseLeave(object sender, MouseEventArgs e)
        {
            var control = sender as FrameworkElement;
            if (control != null)
            {
                this.Cursor = Cursors.Arrow;
            }
        }

        private void Line_MouseEnter(object sender, MouseEventArgs e)
        {
            var control = sender as FrameworkElement;
            if (control != null)
            {
                switch (control.Name)
                {
                    case "Point_Start":
                    case "Point_End":
                        this.Cursor = Cursors.Cross;
                        break;
                    case "Segment":
                        this.Cursor = Cursors.SizeAll;
                        break;
                }
            }
        }
        private void Line_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var element = sender as IInputElement;
            Canvas canvas = this.Parent as Canvas;
            if (element != null && this.Parent != null && canvas != null)
            {
                element.CaptureMouse();
                m_IsCaptured = true;
                m_LastMovePoint = e.GetPosition(canvas);

                this.UpdateCaliper();

                var control = sender as FrameworkElement;
                if (control != null) this.MoveRotateOrigin(control.Name);

                e.Handled = true;
            }
        }

        private void Line_MouseUp(object sender, MouseButtonEventArgs e)
        {
            lock (m_MoveLock)
            {
                Mouse.Capture(null);
                m_IsCaptured = false;
                m_LastMovePoint = new Point();

                this.UpdateCaliper();

                e.Handled = true;
            }
        }
        private void Line_MouseMove(object sender, MouseEventArgs e)
        {
            if (m_IsCaptured)
            {
                lock (m_MoveLock)
                {
                    var control = sender as FrameworkElement;

                    Canvas canvas = this.Parent as Canvas;
                    if (control != null && this.Parent != null && canvas != null)
                    {
                        e.Handled = true;

                        switch (control.Name)
                        {
                            case "Point_Start":
                                var tmpRad = Math.Atan2(e.GetPosition(canvas).Y - (LineRotateTransform.CenterY + this.OriginY), e.GetPosition(canvas).X - (LineRotateTransform.CenterX + this.OriginX));
                                m_Radian = tmpRad > Math.PI ? Math.PI - tmpRad : Math.PI + tmpRad;
                                LineRotateTransform = new RotateTransform(Rotation, this.Width, m_LineThickness / 2);
                                RaisePropertyChanged("LineRotateTransform");

                                var offset = Math.Sqrt(Math.Pow(e.GetPosition(canvas).X - (LineRotateTransform.CenterX + this.OriginX), 2) + Math.Pow(e.GetPosition(canvas).Y - (LineRotateTransform.CenterY + this.OriginY), 2)) - this.Width;

                                this.OriginX -= offset;
                                this.Width += offset;

                                break;
                            case "Point_End":
                                m_Radian = Math.Atan2(e.GetPosition(canvas).Y - (LineRotateTransform.CenterY + this.OriginY), e.GetPosition(canvas).X - (LineRotateTransform.CenterX + this.OriginX));
                                LineRotateTransform = new RotateTransform(Rotation, 0, m_LineThickness / 2);
                                RaisePropertyChanged("LineRotateTransform");

                                this.Width = Math.Sqrt(Math.Pow(e.GetPosition(canvas).X - (LineRotateTransform.CenterX + this.OriginX), 2) + Math.Pow(e.GetPosition(canvas).Y - (LineRotateTransform.CenterY + this.OriginY), 2));

                                break;
                            case "Segment":
                                Vector moveOffset = e.GetPosition(canvas) - m_LastMovePoint;
                                this.OriginX += moveOffset.X;
                                this.OriginY += moveOffset.Y;
                                m_LastMovePoint = e.GetPosition(canvas);
                                break;
                        }
                    }
                }
            }
        }

        private void SymmetryRectangle_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var xMargin = -ProjectionLength / 2;
            var yMargin = -SearchLength / 2;
            (sender as SymmetryRectangle).Margin = new Thickness(xMargin, yMargin, xMargin, yMargin);            
        }
        #endregion


    }

    public class EdgeCollection : ObservableCollection<EdgeDetctor>
    {
        private CvsLineDetect m_LineDetect;
        
        public Point DetectLine(System.Drawing.Bitmap originImage)
        {
            List<Point> inputEdges = new List<Point>();
            foreach(var item in this)
            {
                inputEdges.Add(item.Detect(originImage));
            }
            if (m_LineDetect == null) m_LineDetect = new CvsLineDetect(inputEdges);
            else m_LineDetect.InputPoints = inputEdges;
            m_LineDetect.CalcCoefficient();

            return m_LineDetect.Coefficient;
        }
    }

    public class EdgeDetctor
    {
        private readonly CvsEdgeDetect m_Detect;
        private readonly CvsRectangleAffine m_Rect;

        public EdgeDetctor(CvsRectangleAffine rect, uint contrastThreshold, uint halfPixelCount, EDirection edgeDirection)
        {
            m_Rect = rect;
            m_Detect = new CvsEdgeDetect { EdgeDirection = edgeDirection, HalfPixelCount = halfPixelCount, ContrastThreshold = contrastThreshold };
        }

        public Point Detect(System.Drawing.Bitmap originImage)
        {
            m_Detect.DetectImage = m_Rect.GetCropImage(originImage);
            m_Detect.Detect();

            return m_Detect.Edge;
        }
    }
}
