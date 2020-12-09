using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Line_Searcher_Example.Inspect
{

    public class CvsLineDetect
    {
        private Random m_Rand;                       // jykim : Random()를 내부에서 선언하면 똑같은 난수값이 나옴..
        // 프로퍼티들..
        public List<Point> InputPoints { get; set; }                        // 받은 모든 점들
        public Point Coefficient { get; private set; }                       // 란삭으로 선택된 두 점으로 구한 직선의 방정식의 a, b 값
        public double ConsensusThresholdDistance { get; set; } // Consensus로 인정되는 기준 거리

        public TwoPoints SelectedPoints { get; private set; }                       // 란삭으로 선택된 두 점
        public List<RANSAC_Model> RANSAC_Models { get; private set; }  // 란삭 모델들

        // 함수들..

        // 생성자
        public CvsLineDetect(List<Point> InputPoints)
        {

            this.InputPoints = InputPoints;

            // 6 : 6픽셀
            this.ConsensusThresholdDistance = 6;

            // 모델들 구하기
            CalcModels();

            // SelectedPoints 구하기
            CalcSelectedPoints();

            // Coefficient 구하기
            CalcCoefficient();
        }

        // 모델 구하기
        public void CalcModels()
        {
            try
            {
                // 난수 발생 용
                m_Rand = new Random();
                RANSAC_Models = new List<RANSAC_Model>();

                for (int i = 0; i < 12; i++)
                {
                    // Model 구해서
                    RANSAC_Model Model = new RANSAC_Model();
                    Model.SelectedPoints = SelectTwoPoint(this.InputPoints);

                    CalcConsensusPoints(Model, this.ConsensusThresholdDistance);

                    // RANSAC_Models에 Add.
                    this.RANSAC_Models.Add(Model);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        // 랜덤으로 점 두개 뽑기
        TwoPoints SelectTwoPoint(List<Point> InputPoints)
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

                while (true)
                {
                    Rand_num1 = m_Rand.Next(Start, End);
                    Rand_num2 = m_Rand.Next(Start, End);
                    if (Rand_num1 != Rand_num2)
                        break;
                }

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
        public Double CalcSlope(TwoPoints TwoPoints)
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
        public void CalcConsensusPoints(RANSAC_Model Model, Double ConsensusThreshold)
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
        public void CalcCoefficient()
        {
            try
            {
                // 최대 컨센서스를 갖는 모델은?
                RANSAC_Model Model = new RANSAC_Model();
                foreach (var model in this.RANSAC_Models)
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
                    a = -9999;
                    b = -9999;
                }

                this.Coefficient = new Point(a, b);

            }
            catch (Exception)
            {
                throw;
            }
        }

        public void CalcSelectedPoints()
        {
            try
            {
                // 최대 컨센서스를 갖는 모델은?
                RANSAC_Model Model = new RANSAC_Model();
                foreach (var model in this.RANSAC_Models)
                {
                    if (Model.ConsensusPoints.Count < model.ConsensusPoints.Count)
                    {
                        Model = model;
                    }
                }

                this.SelectedPoints = Model.SelectedPoints;

            }
            catch (Exception)
            {
                throw;
            }
        }


        // 두 점을 갖는 클래스
        public class TwoPoints
        {
            public Point pt1 { get; set; }
            public Point pt2 { get; set; }
        }

        // 란삭 모델
        public class RANSAC_Model
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
