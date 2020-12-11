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
        /// <summary>
        /// 현재 선 도형의 회전 변환 정보를 가져옵니다.
        /// </summary>
        public RotateTransform LineRotateTransform
        {
            get { return m_LineRotateTransform; }
            private set
            {
                m_LineRotateTransform = value;
                RaisePropertyChanged("RectRotateTransform");
            }
        }

        /// <summary>
        /// 현재 라인에 대한 캘리퍼 도구 리스트를 가져옵니다.
        /// </summary>
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

        /// <summary>
        /// 캘리퍼의 개수를 가져오거나 설정합니다.
        /// </summary>
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

        /// <summary>
        /// 각 캘리퍼의 대비 임계값을 가져오거나 설정합니다.
        /// </summary>
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

        /// <summary>
        /// 각 캘리퍼의 절반픽셀 연산 수치를 가져오거나 설정합니다.
        /// </summary>
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

        /// <summary>
        /// 각 캘리퍼의 에지 탐색 방향을 가져오거나 설정합니다.
        /// </summary>
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

        /// <summary>
        /// 각 캘리퍼가 탐색할 길이(높이)를 가져오거나 설정합니다.
        /// </summary>
        public double SearchLength
        {
            get { return (double)GetValue(SearchLengthProperty); }
            set
            {
                SetValue(SearchLengthProperty, value);
                RaisePropertyChanged("SearchLength");
            }
        }

        /// <summary>
        /// 각 캘리퍼가 투사할 길이(너비)를 가져오거나 설정합니다.
        /// </summary>
        public double ProjectionLength
        {
            get { return (double)GetValue(ProjectionLengthProperty); }
            set
            {
                SetValue(ProjectionLengthProperty, value);
                RaisePropertyChanged("ProjectionLength");
            }
        }

        /// <summary>
        /// 현재 선의 원점 X좌표를 가져오거나 설정합니다.
        /// </summary>
        public double OriginX
        {
            get { return (double)GetValue(OriginXProperty); }
            set
            {
                SetValue(OriginXProperty, value);
                if (!m_IsCaptured && (this.Width != double.NaN || this.Width != 0)) this.UpdateCaliper();
            }
        }

        /// <summary>
        /// 현재 선의 원점 Y좌표를 가져오거나 설정합니다.
        /// </summary>
        public double OriginY
        {
            get { return (double)GetValue(OriginYProperty); }
            set
            {
                SetValue(OriginYProperty, value);
                if (!m_IsCaptured && (this.Width != double.NaN || this.Width != 0)) this.UpdateCaliper();
            }
        }

        /// <summary>
        /// 현재 선의 각도를 Degree 값으로 가져오거나 설정합니다.
        /// </summary>
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
                    LineRotateTransform = new RotateTransform(value, m_LineRotateTransform.CenterX, m_LineRotateTransform.CenterY);
                }
                RaisePropertyChanged("LineRotateTransform");
                SetValue(RotationProperty, value);
                if (!m_IsCaptured && (this.Width != double.NaN || this.Width != 0)) this.UpdateCaliper();
            }
        }

        /// <summary>
        /// 현재 선의 각도를 Radian 값으로 가져오거나 설정합니다.
        /// </summary>
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
                    LineRotateTransform = new RotateTransform(deg, m_LineRotateTransform.CenterX, m_LineRotateTransform.CenterY);
                }
                RaisePropertyChanged("LineRotateTransform");
                SetValue(RotationProperty, deg);
                if (!m_IsCaptured && (this.Width != double.NaN || this.Width != 0)) this.UpdateCaliper();
            }
        }
        #endregion

        #endregion

        /// <summary>
        /// 이미지의 라인을 찾도록 돕는 사용자 컨트롤을 생성합니다.
        /// </summary>
        public LineSearcher()
        {
            InitializeComponent();
            DataContext = this;
            m_EdgeDetectCollection = new EdgeCollection();
        }

        #region Methods
        /// <summary>
        /// 현재 데이터를 바탕으로 캘리퍼를 설정합니다.
        /// </summary>
        private void UpdateCaliper()
        {
            m_LineWidth = this.Width;
            m_LineOriginX = this.OriginX;
            m_LineOriginY = this.OriginY;
            
            var caliperCenter = this.Width / CaliperCount;

            //캘리퍼마다 Origin  재설정 (후에 바인딩 등을 통해서 자동으로 할 수 있으면 그렇게 하도록 교체 예정)
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

        /// <summary>
        /// 현재 선 도형의 중심 좌표를 가져옵니다.
        /// </summary>
        /// <returns></returns>
        private Point GetCenter()
        {
            return new Point(this.OriginX + this.Width / 2, this.OriginY + this.Height / 2);
        }

        /// <summary>
        /// 현재 선의 회전 중심 축을 변경합니다.
        /// </summary>
        /// <param name="controlName">현재 클릭한 컨트롤의 이름.</param>
        private void MoveRotateOrigin(string controlName)
        {
            //기존에는 선의 시작점이 중심축이었고 이제 선의 시작점을 이동시켜야 할 경우
            if (LineRotateTransform.CenterX == 0 && controlName.Contains("Start"))
            {
                //새로 중심축이 될 좌표를 현재 중심축에서 로테이션한 좌표로 변환
                var newCenter = this.GetPointByRotation(new Point(this.Width, 0), m_Radian, new Point()) - new Point(-(this.OriginX), -(this.OriginY + m_LineThickness / 2));
                
                //중심 축 이동
                LineRotateTransform = new RotateTransform(Rotation, this.Width, m_LineThickness / 2);
                RaisePropertyChanged("LineRotateTransform");

                //변환한 중심축의 실좌표에서 도형 안의 중심축 값을 빼면 도형이 생각하는 원점 좌표가 나옴.
                this.OriginX = newCenter.X - LineRotateTransform.CenterX;
                this.OriginY = newCenter.Y - LineRotateTransform.CenterY;

            }
            //기존에는 선의 끝 점이 중심축이었고 이제 선의 끝점을 이동시켜야 할 경우
            else if (LineRotateTransform.CenterX != 0 && controlName.Contains("End"))
            {
                //새로 중심축이 될 좌표를 현재 중심축에서 로테이션한 좌표로 변환
                var newCenter = this.GetPointByRotation(new Point(-this.Width, 0), m_Radian, new Point()) - new Point(-(this.OriginX + this.Width), -(this.OriginY + m_LineThickness / 2));

                //중심 축 이동
                LineRotateTransform = new RotateTransform(Rotation, 0, m_LineThickness / 2);
                RaisePropertyChanged("LineRotateTransform");

                //변환한 중심축의 실좌표에서 도형 안의 중심축 값을 빼면 도형이 생각하는 원점 좌표가 나옴.
                this.OriginX = newCenter.X - LineRotateTransform.CenterX;
                this.OriginY = newCenter.Y - LineRotateTransform.CenterY;
            }
        }

        /// <summary>
        /// 목표점을 중심점 기준으로 회전이동변환한 점을 반환합니다. 
        /// </summary>
        /// <param name="rotatePoint">변환시킬 목표점.</param>
        /// <param name="rad">Radian 값 각도.</param>
        /// <param name="center">회전의 중심점.</param>
        /// <returns></returns>
        private Point GetPointByRotation(Point rotatePoint, double rad, Point center)
        {
            return new Point(Math.Cos(rad) * rotatePoint.X - Math.Sin(rad) * rotatePoint.Y - center.X, Math.Sin(rad) * rotatePoint.X + Math.Cos(rad) * rotatePoint.Y - center.Y);
        }
        #endregion

        #region Events
        private void LineSearcher_Loaded(object sender, RoutedEventArgs e)
        {
            //컨트롤의 기본값 설정
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
            //마우스 모양 원래대로 돌리기
            var control = sender as FrameworkElement;
            if (control != null)
            {
                this.Cursor = Cursors.Arrow;
            }
        }

        private void Line_MouseEnter(object sender, MouseEventArgs e)
        {
            //위치에 맞는 마우스 모양 변경
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
            //마우스 캡쳐가 가능한 컨트롤이면서 부모로 캔버스를 지니는 지 확인
            if (element != null && this.Parent != null && canvas != null)
            {
                //마우스 고정 (이후로 마우스이벤트는 이 컨트롤에 관한 것만 발생)
                element.CaptureMouse();
                m_IsCaptured = true;
                //점 위치 설정
                m_LastMovePoint = e.GetPosition(canvas);
                
                //컨트롤 이름 가져와서 중심축 체크
                var control = sender as FrameworkElement;
                if (control != null) this.MoveRotateOrigin(control.Name);

                //상위에 연결된 다른 MouseDown 실행시키지 않음
                e.Handled = true;
            }
        }

        private void Line_MouseUp(object sender, MouseButtonEventArgs e)
        {
            lock (m_MoveLock)
            {
                //캡쳐 해제
                Mouse.Capture(null);
                m_IsCaptured = false;
                //위치초기화
                m_LastMovePoint = new Point();

                //캘리퍼 업데이트
                this.UpdateCaliper();

                //상위에 연결된 다른 MouseUp 실행시키지 않음
                e.Handled = true;
            }
        }
        private void Line_MouseMove(object sender, MouseEventArgs e)
        {
            if (m_IsCaptured)
            {
                //MouseUp 이벤트가 끝나기도 전에 발생하지 않도록 임계구역 설정
                lock (m_MoveLock)
                {
                    var control = sender as FrameworkElement;

                    Canvas canvas = this.Parent as Canvas;
                    //본인이 컨트롤이고 부모로 캔버스를 가지고 있는지 확인
                    if (control != null && this.Parent != null && canvas != null)
                    {
                        //다른 MouseMove 실행시키지 않음
                        e.Handled = true;

                        //이름 확인
                        switch (control.Name)
                        {
                            //시작점일 경우
                            case "Point_Start":
                                //마우스 위치 증감에 따라 반대로 움직여야 함
                                m_Radian = Math.PI + Math.Atan2(e.GetPosition(canvas).Y - (LineRotateTransform.CenterY + this.OriginY), e.GetPosition(canvas).X - (LineRotateTransform.CenterX + this.OriginX));
                                
                                //실측 길이와 실제 너비의 차이
                                var offset = Math.Sqrt(Math.Pow(e.GetPosition(canvas).X - (LineRotateTransform.CenterX + this.OriginX), 2) + Math.Pow(e.GetPosition(canvas).Y - (LineRotateTransform.CenterY + this.OriginY), 2)) - this.Width;
                                
                                //너비 변화량만큼 원점에선 빼주고 기존 너비에선 더해줌
                                this.OriginX -= offset;
                                this.Width += offset;

                                //회전 및 중심축 재설정
                                LineRotateTransform = new RotateTransform(Rotation, this.Width, m_LineThickness / 2);
                                RaisePropertyChanged("LineRotateTransform");
                                break;
                            //끝점일 경우
                            case "Point_End":
                                m_Radian = Math.Atan2(e.GetPosition(canvas).Y - (LineRotateTransform.CenterY + this.OriginY), e.GetPosition(canvas).X - (LineRotateTransform.CenterX + this.OriginX));
                                
                                this.Width = Math.Sqrt(Math.Pow(e.GetPosition(canvas).X - (LineRotateTransform.CenterX + this.OriginX), 2) + Math.Pow(e.GetPosition(canvas).Y - (LineRotateTransform.CenterY + this.OriginY), 2));

                                //회전 재설정
                                LineRotateTransform = new RotateTransform(Rotation, 0, m_LineThickness / 2);
                                RaisePropertyChanged("LineRotateTransform");
                                break;
                            //선분일 경우
                            case "Segment":
                                //위치 변화량 구하기
                                Vector moveOffset = e.GetPosition(canvas) - m_LastMovePoint;
                                //원점에 변화량 더해줌
                                this.OriginX += moveOffset.X;
                                this.OriginY += moveOffset.Y;
                                //다음 위치 정보를 현재 위치로 변경
                                m_LastMovePoint = e.GetPosition(canvas);
                                break;
                        }
                    }
                }
            }
        }

        //외부에서 Width 변경 시 호출하도록 설정
        private void ContentControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!m_IsCaptured) this.UpdateCaliper();
        }

        //외부에서 Search Length, Projection Length 변경 시 호출하도록 설정
        private void SymmetryRectangle_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var xMargin = -ProjectionLength / 2;
            var yMargin = -SearchLength / 2;
            (sender as SymmetryRectangle).Margin = new Thickness(xMargin, yMargin, xMargin, yMargin);
            if (this.Width != double.NaN || this.Width != 0) this.UpdateCaliper();
        }
        #endregion


    }

    public class EdgeCollection : ObservableCollection<EdgeDetctor>
    {
        private CvsLineDetect m_LineDetect;
        
        public Point DetectLine(System.Drawing.Bitmap originImage)
        {
            if (this.Count == 0) return new Point(double.NaN, double.NaN);
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

    /// <summary>
    /// 에지 찾기 위한 도구 클래스.
    /// </summary>
    public class EdgeDetctor
    {
        /// <summary>
        /// 에지 찾는 역할 담당.
        /// </summary>
        private readonly CvsEdgeDetect m_Detect;
        /// <summary>
        /// 이미지에서 ROI 자르는 역할.
        /// </summary>
        private readonly CvsRectangleAffine m_Rect;

        /// <summary>
        /// 에지 찾기 위한 도구 클래스를 생성합니다.
        /// </summary>
        /// <param name="rect">ROI 영역.</param>
        /// <param name="contrastThreshold">대비 임계값.</param>
        /// <param name="halfPixelCount">절반 픽셀 수치.</param>
        /// <param name="edgeDirection">에지 방향.</param>
        public EdgeDetctor(CvsRectangleAffine rect, uint contrastThreshold, uint halfPixelCount, EDirection edgeDirection)
        {
            m_Rect = rect;
            m_Detect = new CvsEdgeDetect { EdgeDirection = edgeDirection, HalfPixelCount = halfPixelCount, ContrastThreshold = contrastThreshold };
        }

        /// <summary>
        /// 가진 데이터를 바탕으로 원본이미지에서 에지를 찾아냅니다.
        /// </summary>
        /// <param name="originImage">원본 이미지.</param>
        /// <returns></returns>
        public Point Detect(System.Drawing.Bitmap originImage)
        {
            m_Detect.DetectImage = m_Rect.GetCropImage(originImage);
            m_Detect.Detect();

            return m_Detect.Edge;
        }
    }
}
