using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Line_Searcher_Example.Inspect
{

    public class CvsLineDetect
    {
        private Random m_Rand = new Random();                    // jykim : Random()를 내부에서 선언하면 똑같은 난수값이 나옴..
        private TwoPoints m_SelectedPoints;                       // 란삭으로 선택된 두 점
        private List<RANSAC_Model> m_RANSAC_Models;             // 란삭 모델들

        // 프로퍼티들..
        public List<Point> InputPoints { get; set; }                        // 받은 모든 점들
        public Point Coefficient { get; private set; }                       // 란삭으로 선택된 두 점으로 구한 직선의 방정식의 a, b 값
        public double ConsensusThresholdDistance { get; set; } // Consensus로 인정되는 기준 거리
        public DrawingImage Overlay { get; private set; }
        public double OverlayWidth { get; set; }
        public double OverlayHeight { get; set; }
        // 함수들..

        // 생성자
        public CvsLineDetect(List<Point> InputPoints, double imageWidth, double imageHeight)
        {
            this.InputPoints = InputPoints;
            // 6 : 6픽셀
            this.ConsensusThresholdDistance = 10;

            this.OverlayWidth = imageWidth;
            this.OverlayHeight = imageHeight;
        }

        public void Run()
        {
            // 모델들 구하기
            CalcModels();

            // SelectedPoints 구하기
            CalcSelectedPoints();

            // Coefficient 구하기
            CalcCoefficient();

            DrawingImage di = new DrawingImage(this.CreateGeometry());
            di.Freeze();
            Overlay = di;
        }

        // 모델 구하기
        private void CalcModels()
        {
            try
            {
                // 난수 발생 용
                m_RANSAC_Models = new List<RANSAC_Model>();

                for (int i = 0; i < 12; i++)
                {
                    // Model 구해서
                    RANSAC_Model Model = new RANSAC_Model();
                    Model.SelectedPoints = SelectTwoPoint(this.InputPoints);

                    CalcConsensusPoints(Model, this.ConsensusThresholdDistance);

                    // RANSAC_Models에 Add.
                    this.m_RANSAC_Models.Add(Model);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        // 랜덤으로 점 두개 뽑기
        private TwoPoints SelectTwoPoint(List<Point> InputPoints)
        {
            try
            {
                // 점이 한개 이하이면 안되
                if (InputPoints.Count <= 1)
                    throw new Exception("점이 한개 이하면 안되");

                // 결과 점 두개
                TwoPoints Result = new TwoPoints();

                // 변수들 초기화
                int Start, End, Size, Rand_num1, Rand_num2;
                Size = InputPoints.Count;
                Start = 0;
                End = Size - 1;

                // 겹치지 않는 난수 구해
                //Random Rand = new Random();

                do
                {
                    Rand_num1 = m_Rand.Next(Start, End);
                    Rand_num2 = m_Rand.Next(Start, End);
                } while (Rand_num1 == Rand_num2 && InputPoints.Count > 2);

                // 난수를 인덱스로 사용해서 pt1, pt2 구해
                Result.pt1 = InputPoints[Rand_num1];
                Result.pt2 = InputPoints[Rand_num2];

                // 결과 리턴
                return Result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        // 기울기 구하기
        private Double CalcSlope(TwoPoints TwoPoints)
        {
            try
            {
                Double Slope;
                Point ptOne = TwoPoints.pt1;
                Point ptTwo = TwoPoints.pt2;

                if (ptTwo.X - ptOne.X == 0)
                {
                    Slope = 0;
                }
                else
                {
                    Slope = (ptTwo.Y - ptOne.Y) / (ptTwo.X - ptOne.X);
                }

                return Slope;
            }
            catch (Exception)
            {
                throw;
            }
        }

        // 지지 점들 구하기
        private void CalcConsensusPoints(RANSAC_Model Model, Double ConsensusThreshold)
        {
            try
            {
                Model.ConsensusPoints.Clear();

                TwoPoints TwoPoints = Model.SelectedPoints;
                Double distance = 0;

                Point ptOne = TwoPoints.pt1;
                Point ptTwo = TwoPoints.pt2;

                for (int i = 0; i < this.InputPoints.Count; i++)
                {
                    Double x1 = this.InputPoints[i].X;
                    Double y1 = this.InputPoints[i].Y;

                    // 거리 구하기
                    if (ptTwo.X == ptOne.X)
                    {//x가 같으면
                        distance = Math.Abs(x1 - ptOne.X);
                    }
                    else if (ptTwo.Y == ptOne.Y)
                    {//y가 같으면
                        distance = Math.Abs(y1 - ptOne.Y);
                    }
                    else
                    {
                        // 점(ptOne.X,ptOne.Y)를 지나는 직선의 방정식 : mx-y-m*ptOne.X+ptOne.Y = 0

                        // =====점(x1,y1)과 직선(ax+by+c=0)의 거리 구하는 공식====
                        // |ax1+by1+c|
                        // ------------
                        // 루트(a제곱+b제곱)

                        Double Slope = CalcSlope(TwoPoints);
                        distance = Math.Abs(x1 * Slope + (-y1) + (-Slope * ptOne.X + ptOne.Y)) / (Math.Sqrt(Math.Pow(Slope, 2) + 1));
                    }

                    if (distance < ConsensusThreshold)
                    {
                        Model.ConsensusPoints.Add(this.InputPoints[i]);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        // Coefficient구하기
        private void CalcCoefficient()
        {
            try
            {
                // 최대 컨센서스를 갖는 모델은?
                RANSAC_Model Model = new RANSAC_Model();
                foreach (var model in this.m_RANSAC_Models)
                {
                    if (Model.ConsensusPoints.Count < model.ConsensusPoints.Count)
                    {
                        Model = model;
                    }
                }

                // a, b 구하기
                Point pt1 = Model.SelectedPoints.pt1;
                Point pt2 = Model.SelectedPoints.pt2;
                double a, b;
                if (pt1.X != pt2.X)
                {
                    a = (pt2.Y - pt1.Y) / (pt2.X - pt1.X);
                    b = (pt2.X * pt1.Y - pt1.X * pt2.Y) / (pt2.X - pt1.X);
                }
                else
                {
                    // X = C 직선 (y축과 평행한 직선)
                    a = double.NaN;
                    b = double.NaN;
                }

                this.Coefficient = new Point(a, b);

            }
            catch (Exception)
            {
                throw;
            }
        }

        private void CalcSelectedPoints()
        {
            try
            {
                // 최대 컨센서스를 갖는 모델은?
                RANSAC_Model Model = new RANSAC_Model();
                foreach (var model in this.m_RANSAC_Models)
                {
                    if (Model.ConsensusPoints.Count < model.ConsensusPoints.Count)
                    {
                        Model = model;
                    }
                }

                this.m_SelectedPoints = Model.SelectedPoints;

            }
            catch (Exception)
            {
                throw;
            }
        }

        private DrawingGroup CreateGeometry()
        {
            DrawingGroup dg = new DrawingGroup();
            GeometryDrawing overlay = new GeometryDrawing
            {
                Geometry = new RectangleGeometry(new Rect(0, 0, OverlayWidth, OverlayHeight)),
                Brush = Brushes.Transparent,
                Pen = new Pen(Brushes.Transparent, 1)
            };
            dg.Children.Add(overlay);

            GeometryDrawing graphic = new GeometryDrawing();
            GeometryGroup group = new GeometryGroup();



            foreach (var point in InputPoints)
            {
                group.Children.Add(new LineGeometry(new Point(point.X - 5, point.Y + 5), new Point(point.X + 5, point.Y - 5)));
                group.Children.Add(new LineGeometry(new Point(point.X - 5, point.Y - 5), new Point(point.X + 5, point.Y + 5)));
            }
            //var firstX = InputPoints.First().X;
            //var lastX = InputPoints.Last().X;
            //group.Children.Add(new LineGeometry(new Point(firstX, firstX * Coefficient.X + Coefficient.Y), new Point(lastX, lastX * Coefficient.X + Coefficient.Y)));

            Point startPoint = new Point(), endPoint = new Point();
            if (Coefficient.X == double.NaN)
            {
                startPoint = new Point(m_SelectedPoints.pt1.X, 0);
                endPoint = new Point(m_SelectedPoints.pt1.X, OverlayHeight);
            }
            else
            {
                if (Coefficient.Y > OverlayHeight || Coefficient.Y < 0)
                {
                    startPoint = new Point(OverlayWidth, OverlayWidth * Coefficient.X + Coefficient.Y);
                }
                else
                {
                    startPoint = new Point(0, Coefficient.Y);
                }
                if (Coefficient.X == 0)
                {
                    endPoint = new Point(OverlayWidth - startPoint.X, Coefficient.Y);
                }
                else if(-Coefficient.Y / Coefficient.X < 0 || -Coefficient.Y / Coefficient.X > OverlayWidth)
                {
                    endPoint = new Point((OverlayHeight - Coefficient.Y) / Coefficient.X , OverlayHeight);
                }
                else
                {
                    endPoint = new Point(-Coefficient.Y / Coefficient.X, 0);
                }
            }
            group.Children.Add(new LineGeometry(startPoint, endPoint));

            graphic.Geometry = group;
            graphic.Brush = Brushes.Transparent;
            graphic.Pen = new Pen(Brushes.LawnGreen, 1);
            graphic.Freeze();

            dg.Children.Add(graphic);

            return dg;
        }


        // 두 점을 갖는 클래스
        class TwoPoints
        {
            public Point pt1 { get; set; }
            public Point pt2 { get; set; }
        }

        // 란삭 모델
        class RANSAC_Model
        {
            // 프로퍼티들..

            // 선택된 두 점
            public TwoPoints SelectedPoints { get; set; }

            // 지지 점들
            public List<Point> ConsensusPoints { get; set; }


            // 함수들..

            // 생성자
            public RANSAC_Model()
            {
                SelectedPoints = new TwoPoints();
                ConsensusPoints = new List<Point>();
            }
        }
    }

}
