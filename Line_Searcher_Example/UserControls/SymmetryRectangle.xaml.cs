using System;
using System.Collections.Generic;
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
    /// SymmetryRectangle.xaml에 대한 상호 작용 논리
    /// </summary>

    public partial class SymmetryRectangle : UserControl, INotifyPropertyChanged
    {
        #region Fields
        private readonly object m_MoveLock = new object();

        private bool m_IsCaptured;
        private Point m_LastMovePoint;
        private Point m_LastSizePoint;

        private double m_RectOriginX;
        private double m_RectOriginY;
        private double m_RectWidth;
        private double m_RectHeight;

        #endregion

        #region Properties
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propName)
        {
            if (PropertyChanged != null) PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propName));
        }
        #region Common Properties


        #endregion

        #region Dependency Properties
        public static readonly DependencyProperty OriginXProperty =
            DependencyProperty.Register("OriginX", typeof(double), typeof(SymmetryRectangle));

        public static readonly DependencyProperty OriginYProperty =
            DependencyProperty.Register("OriginY", typeof(double), typeof(SymmetryRectangle));

        public static readonly DependencyProperty IsGroupedProperty =
            DependencyProperty.Register("IsGrouped", typeof(bool), typeof(SymmetryRectangle));

        /// <summary>
        /// 이 도형의 원점 X좌표를 가져오거나 설정합니다.
        /// </summary>
        public double OriginX
        {
            get { return (double)GetValue(OriginXProperty); }
            set
            {
                SetValue(OriginXProperty, value);
            }
        }

        /// <summary>
        /// 이 도형의 원점 Y좌표를 가져오거나 설정합니다.
        /// </summary>
        public double OriginY
        {
            get { return (double)GetValue(OriginYProperty); }
            set { SetValue(OriginYProperty, value); }
        }

        /// <summary>
        /// 이 도형이 여러개의 그룹으로 묶이는 지 아니면 단독으로 쓰이는 지 여부를 가져오거나 설정합니다.
        /// </summary>
        public bool IsGrouped
        {
            get { return (bool)GetValue(IsGroupedProperty); }
            set { SetValue(IsGroupedProperty, value); }
        }
        #endregion

        #endregion

        public SymmetryRectangle()
        {
            InitializeComponent();
            //DataContext = this;
        }

        #region Methods
        /// <summary>
        /// 이 도형을 업데이트합니다.
        /// </summary>
        private void UpdateRect()
        {
            m_RectWidth = this.Width;
            m_RectHeight = this.Height;
            m_RectOriginX = this.OriginX;
            m_RectOriginY = this.OriginY;
        }
        #endregion

        #region Events
        private void SymmetryRectangle_Loaded(object sender, RoutedEventArgs e)
        {
            this.UpdateRect();
        }

        private void Rectangle_MouseLeave(object sender, MouseEventArgs e)
        {
            //그룹으로 묶여있는 것이면 Mouse 동작 스킵
            if (IsGrouped) return;
            //커서 원래대로 돌리기
            var control = sender as FrameworkElement;
            if (control != null)
            {
                this.Cursor = Cursors.Arrow;
            }
        }

        private void Rectangle_MouseEnter(object sender, MouseEventArgs e)
        {
            //그룹으로 묶여있는 것이면 Mouse 동작 스킵
            if (IsGrouped) return;
            //위치에 맞는 커서로 변경
            var control = sender as FrameworkElement;
            if (control != null)
            {
                switch (control.Name)
                {
                    case "Size_NW":
                    case "Size_NE":
                    case "Size_SW":
                    case "Size_SE":
                        this.Cursor = Cursors.Cross;
                        break;
                    case "Movable_Grid":
                        this.Cursor = Cursors.SizeAll;
                        break;
                }
            }
        }
        private void Rectangle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //그룹으로 묶여있는 것이면 Mouse 동작 스킵
            if (IsGrouped) return;
            var element = sender as IInputElement;
            Canvas canvas = this.Parent as Canvas;
            if (element != null && this.Parent != null && canvas != null)
            {
                element.CaptureMouse();
                m_IsCaptured = true;
                m_LastMovePoint = e.GetPosition(canvas);
                m_LastSizePoint = e.GetPosition(canvas);
                this.UpdateRect();

                e.Handled = true;
            }
        }

        private void Rectangle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            //그룹으로 묶여있는 것이면 Mouse 동작 스킵
            if (IsGrouped) return;
            lock (m_MoveLock)
            {
                Mouse.Capture(null);
                m_IsCaptured = false;
                m_LastMovePoint = new Point();
                m_LastSizePoint = new Point();
                this.UpdateRect();

                e.Handled = true;
            }
        }
        private void Retangle_MouseMove(object sender, MouseEventArgs e)
        {
            //그룹으로 묶여있는 것이면 Mouse 동작 스킵
            if (IsGrouped) return;
            if (m_IsCaptured)
            {
                lock (m_MoveLock)
                {
                    var control = sender as FrameworkElement;

                    Canvas canvas = this.Parent as Canvas;
                    if (control != null && this.Parent != null && canvas != null)
                    {
                        e.Handled = true;
                        Vector sizeOffset = e.GetPosition(canvas) - m_LastSizePoint;
                        switch (control.Name)
                        {
                            // W, H 둘 중 하나만 변화되어도 밑에 있는 원점 변경 이벤트 호출됨
                            //왼쪽 상단을 이동시키면 - 변화량 으로 W, H 증감
                            case "Size_NW":
                                if (m_RectWidth - 2 * sizeOffset.X > this.MinWidth) this.Width = m_RectWidth - 2 * sizeOffset.X;
                                else this.Width = this.MinWidth;
                                if (m_RectHeight - 2 * sizeOffset.Y > this.MinHeight) this.Height = m_RectHeight - 2 * sizeOffset.Y;
                                else this.Height = this.MinHeight;
                                break;

                            //오른쪽 상단을 이동시키면 + 변화량으로 W 증감, - 변화량 으로 H 증감
                            case "Size_NE":
                                if (m_RectWidth + 2 * sizeOffset.X > this.MinWidth) this.Width = m_RectWidth + 2 * sizeOffset.X;
                                else this.Width = this.MinWidth;
                                if (m_RectHeight - 2 * sizeOffset.Y > this.MinHeight) this.Height = m_RectHeight - 2 * sizeOffset.Y;
                                else this.Height = this.MinHeight;
                                break;

                            //왼쪽 하단을 이동시키면 + 변화량으로 H 증감, - 변화량 으로 W 증감
                            case "Size_SW":
                                if (m_RectWidth - 2 * sizeOffset.X > this.MinWidth) this.Width = m_RectWidth - 2 * sizeOffset.X;
                                else this.Width = this.MinWidth;
                                if (m_RectHeight + 2 * sizeOffset.Y > this.MinHeight) this.Height = m_RectHeight + 2 * sizeOffset.Y;
                                else this.Height = this.MinHeight;
                                break;

                            //오른쪽 하단을 이동시키면 + 변화량 으로 W, H 증감
                            case "Size_SE":
                                if (m_RectWidth + 2 * sizeOffset.X > this.MinWidth) this.Width = m_RectWidth + 2 * sizeOffset.X;
                                else this.Width = this.MinWidth;
                                if (m_RectHeight + 2 * sizeOffset.Y > this.MinHeight) this.Height = m_RectHeight + 2 * sizeOffset.Y;
                                else this.Height = this.MinHeight;
                                break;

                            // 이동 변화량으로 원점 증감
                            case "Movable_Grid":
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

        //내,외부에서 Width, Height 변경할 시 동작
        private void ContentControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (m_RectWidth != 0 && m_RectHeight != 0)
            {
                this.OriginX = m_RectOriginX + (m_RectWidth - this.Width) / 2;
                this.OriginY = m_RectOriginY + (m_RectHeight - this.Height) / 2;
                this.UpdateRect();
            }
        }

        #endregion

    }
}
