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


        public double OriginX
        {
            get { return (double)GetValue(OriginXProperty); }
            set
            {
                SetValue(OriginXProperty, value);
            }
        }

        public double OriginY
        {
            get { return (double)GetValue(OriginYProperty); }
            set { SetValue(OriginYProperty, value); }
        }

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
            if (IsGrouped) return;
            var control = sender as FrameworkElement;
            if (control != null)
            {
                this.Cursor = Cursors.Arrow;
            }
        }

        private void Rectangle_MouseEnter(object sender, MouseEventArgs e)
        {
            if (IsGrouped) return;
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
                            case "Size_NW":
                                if (m_RectWidth - 2 * sizeOffset.X > this.MinWidth) this.Width = m_RectWidth - 2 * sizeOffset.X;
                                else this.Width = this.MinWidth;
                                if (m_RectHeight - 2 * sizeOffset.Y > this.MinHeight) this.Height = m_RectHeight - 2 * sizeOffset.Y;
                                else this.Height = this.MinHeight;
                                break;

                            case "Size_NE":
                                if (m_RectWidth + 2 * sizeOffset.X > this.MinWidth) this.Width = m_RectWidth + 2 * sizeOffset.X;
                                else this.Width = this.MinWidth;
                                if (m_RectHeight - 2 * sizeOffset.Y > this.MinHeight) this.Height = m_RectHeight - 2 * sizeOffset.Y;
                                else this.Height = this.MinHeight;
                                break;

                            case "Size_SW":
                                if (m_RectWidth - 2 * sizeOffset.X > this.MinWidth) this.Width = m_RectWidth - 2 * sizeOffset.X;
                                else this.Width = this.MinWidth;
                                if (m_RectHeight + 2 * sizeOffset.Y > this.MinHeight) this.Height = m_RectHeight + 2 * sizeOffset.Y;
                                else this.Height = this.MinHeight;
                                break;

                            case "Size_SE":
                                if (m_RectWidth + 2 * sizeOffset.X > this.MinWidth) this.Width = m_RectWidth + 2 * sizeOffset.X;
                                else this.Width = this.MinWidth;
                                if (m_RectHeight + 2 * sizeOffset.Y > this.MinHeight) this.Height = m_RectHeight + 2 * sizeOffset.Y;
                                else this.Height = this.MinHeight;
                                break;

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

        private void ContentControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (m_RectWidth != 0 && m_RectHeight != 0)
            {
                this.OriginX = m_RectOriginX + (m_RectWidth - this.Width) / 2;
                this.OriginY = m_RectOriginY + (m_RectHeight - this.Height) / 2;
            }
        }

        #endregion

    }
}
