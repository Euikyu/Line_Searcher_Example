using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Point = System.Windows.Point;

namespace Line_Searcher_Example.Inspect
{
    public enum EDirection
    {
        Any,
        LightToDark,
        DarkToLight
    }
    public class CvsEdgeDetect
    {
        #region Fields
        private float[] m_SubPixelArray;
        private float[] m_ProjectionArray;

        private Bitmap m_DetectImage;
        private byte[] m_DetectRawImage;
        private int m_Width;
        private int m_Height;
        #endregion

        #region Properties
        /// <summary>
        /// Projection Array 의 변화량을 측정한 배열.
        /// </summary>
        public float[] SubPixelArray
        {
            get
            {
                if (m_SubPixelArray == null) this.CalculateSubPixelArray();
                return m_SubPixelArray;
            }
        }
        /// <summary>
        /// 2차원의 이미지를 1차원 배열로 Projection 한 배열.
        /// </summary>
        public float[] ProjectionArray
        {
            get
            {
                if (m_ProjectionArray == null) this.CalculateProjectionArray();
                return m_ProjectionArray;
            }
        }

        /// <summary>
        /// 검사할 이미지.
        /// </summary>
        public Bitmap DetectImage
        {
            get
            {
                return m_DetectImage;
            }

            set
            {
                if (value.PixelFormat == System.Drawing.Imaging.PixelFormat.Format8bppIndexed)
                {
                    m_Width = value.Width;
                    m_Height = value.Height;
                    var bdata = value.LockBits(new Rectangle(0, 0, m_Width, m_Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
                    m_ProjectionArray = null;
                    m_SubPixelArray = null;
                    m_DetectRawImage = new byte[m_Width * m_Height];
                    Marshal.Copy(bdata.Scan0, m_DetectRawImage, 0, m_Width * m_Height);
                    value.UnlockBits(bdata);
                    m_DetectImage = value;
                }
            }
        }

        /// <summary>
        /// 하나의 에지로 인식하기 위해 중간을 생략할 픽셀 개수.
        /// </summary>
        public uint HalfPixelCount
        {
            get; set;
        }
        /// <summary>
        /// 특정 이상의 변화량을 에지로 판단하기 위한 임계값. 
        /// </summary>
        public uint ContrastThreshold
        {
            get; set;
        }
        /// <summary>
        /// 에지를 감지할 방향.
        /// </summary>
        public EDirection EdgeDirection
        {
            get; set;
        }
        /// <summary>
        /// 검사 후 찾은 에지 좌표.
        /// </summary>
        public Point Edge
        {
            get; private set;
        }
        #endregion

        public CvsEdgeDetect()
        {
            //초기 설정값
            EdgeDirection = EDirection.Any;
            ContrastThreshold = 5;
            HalfPixelCount = 2;
        }

        #region Methods
        /// <summary>
        /// 이미지를 1차원으로 투사합니다.
        /// </summary>
        public void CalculateProjectionArray()
        {
            //이미지 없음
            if (m_DetectRawImage == null) throw new Exception("Image select first.");

            //width 방향으로 투사
            //그래서 배열의 크기는 높이가 됨.
            m_ProjectionArray = new float[m_Height];

            //이미지 투사
            for (int row = 0; row < m_Height; row++)
            {
                int sum = 0;
                for (int col = 0; col < m_Width; col++)
                {
                    //width 픽셀 더하기
                    sum += m_DetectRawImage[row * m_Width + col];
                }
                // 투사값은 width 픽셀 합의 평균
                m_ProjectionArray[row] = (float)sum / m_Width;
            }
        }
        /// <summary>
        /// 투사된 배열의 각 변화량을 계산합니다.
        /// </summary>
        public void CalculateSubPixelArray()
        {
            //이미지 투사가 진행되지 않았다면 선행
            if (m_ProjectionArray == null) this.CalculateProjectionArray();

            // 서브픽셀 배열도 프로젝션 배열과 같은 크기
            m_SubPixelArray = new float[m_Height];

            for (int i = 0; i < m_Height; i++)
            {
                //처음 값과 마지막 값은 항상 0
                if (i == 0 || i == m_Height - 1)
                {
                    m_SubPixelArray[i] = 0;
                    continue;
                }
                // 내 값을 기준으로 앞 뒤 값의 차를 구하여 변화량 측정
                m_SubPixelArray[i] = m_ProjectionArray[i - 1] - m_ProjectionArray[i + 1];
            }
        }
        /// <summary>
        /// 이미지의 에지를 검색합니다.
        /// </summary>
        public void Detect()
        {
            //이전 서브픽셀 값
            float lastValue = 0;

            //현재 서브픽셀 변화량의 합
            float currentSum_dx = 0;
            //현재 (서브픽셀 변화량 * 현재 픽셀 위치) 의 합
            float currentSum_y_mul_dx = 0;

            // 하프픽셀 필터에 걸치는 서브픽셀 변화량의 함
            float tmpSum_dx = 0;
            // 하프픽셀 필터에 걸치는 (서브픽셀 변화량 * 현재 픽셀 위치)의 함
            float tmpSum_y_mul_dx = 0;

            //에지 값이 여러개일 수도 있으니 리스트로 일단 선언
            List<float> edgeList = new List<float>();

            //하프픽셀 필터로 걸치는 변화량 개수
            uint inverseCount = 0;

            //서브픽셀 배열 개수
            int count = SubPixelArray.Length;

            //서브픽셀 한 바퀴 도는 동안
            for (int i = 0; i < count; i++)
            {
                // 현재 서브픽셀 값이 양수라면?
                if (m_SubPixelArray[i] > 0)
                {
                    //기존값과 부호가 반대라면
                    if (lastValue < 0)
                    {
                        //반대인 채로 허용 하프픽셀 개수를 넘어섰다면?
                        if (inverseCount > HalfPixelCount)
                        {
                            //현재합으로 에지 계산 -> 0일 경우 에지 추가 안함
                            if (this.CalculateEdgeByEdgeDirection(currentSum_dx, currentSum_y_mul_dx) is float res) edgeList.Add(res);

                            //임시로 쌓았던 것은 현재로 변경 및 하프픽셀 개수 초기화 
                            currentSum_dx = tmpSum_dx;
                            currentSum_y_mul_dx = tmpSum_y_mul_dx;
                            tmpSum_dx = 0;
                            tmpSum_y_mul_dx = 0;
                            inverseCount = 0;
                        }
                        //넘어서지 않았다면?
                        else
                        {
                            //하프픽셀 걸치는 개수 추가
                            inverseCount++;
                            // 대비 임계값을 넘어선 수치라면 임시 합에 더해줌
                            if (Math.Abs(m_SubPixelArray[i]) > ContrastThreshold)
                            {
                                tmpSum_dx += m_SubPixelArray[i];
                                tmpSum_y_mul_dx += i * m_SubPixelArray[i];
                            }
                            //기존 값 변경 안함
                            continue;
                        }
                    }
                    //기존값과 부호가 같다면
                    else
                    {
                        //하프픽셀 개수 초기화
                        inverseCount = 0;
                        // 대비 임계값을 넘어선 수치라면 현재 합에 더해줌
                        if (Math.Abs(m_SubPixelArray[i]) > ContrastThreshold)
                        {
                            currentSum_dx += m_SubPixelArray[i];
                            currentSum_y_mul_dx += i * m_SubPixelArray[i];
                        }
                    }
                }
                // 현재 서브픽셀 값이 음수라면? 양수와 반대로 동작함. -> 동작원리는 같음
                else if (m_SubPixelArray[i] < 0)
                {
                    if (lastValue > 0)
                    {
                        if (inverseCount > HalfPixelCount)
                        {
                            if (this.CalculateEdgeByEdgeDirection(currentSum_dx, currentSum_y_mul_dx) is float res) edgeList.Add(res);
                            currentSum_dx = tmpSum_dx;
                            currentSum_y_mul_dx = tmpSum_y_mul_dx;
                            tmpSum_dx = 0;
                            tmpSum_y_mul_dx = 0;
                            inverseCount = 0;
                        }
                        else
                        {
                            inverseCount++;
                            if (Math.Abs(m_SubPixelArray[i]) > ContrastThreshold)
                            {
                                tmpSum_dx += m_SubPixelArray[i];
                                tmpSum_y_mul_dx += i * m_SubPixelArray[i];
                            }
                            continue;
                        }
                    }
                    else
                    {
                        inverseCount = 0;
                        if (Math.Abs(m_SubPixelArray[i]) > ContrastThreshold)
                        {
                            currentSum_dx += m_SubPixelArray[i];
                            currentSum_y_mul_dx += i * m_SubPixelArray[i];
                        }
                    }
                }
                //변화량이 0이라면?
                else
                {
                    //하프픽셀 걸치는지 확인
                    if (inverseCount > HalfPixelCount)
                    {
                        //현재합으로 에지 계산 -> 0일 경우 에지 추가 안함
                        if (this.CalculateEdgeByEdgeDirection(currentSum_dx, currentSum_y_mul_dx) is float res) edgeList.Add(res);

                        //임시로 쌓았던 것은 현재로 변경 및 하프픽셀 개수 초기화 
                        currentSum_dx = tmpSum_dx;
                        currentSum_y_mul_dx = tmpSum_y_mul_dx;
                        tmpSum_dx = 0;
                        tmpSum_y_mul_dx = 0;
                        inverseCount = 0;
                    }
                    else
                    {
                        //하프픽셀 개수 추가
                        inverseCount++;
                        continue;
                    }
                }
                //같은 방향(-, +)의 제품이 들어왔다면 기존 값을 바꿔줌
                lastValue = m_SubPixelArray[i];
            }

            //반복문이 끝나고 에지 리스트 중 첫 번째 에지를 결과에지로 뿌려줌
            //나중에는 에지 스코어링 부분 추가하여 상황에 따라 첫 번째 에지를 찾을지, 대비값이 큰 에지를 찾을지 결정
            if (edgeList.Count > 0) Edge = new Point(m_Width / 2, edgeList.First());
            else Edge = new Point(0, 0);
        }

        /// <summary>
        /// 에지 방향 결정하는 함수.
        /// </summary>
        /// <param name="sum_dx">변화량의 합.</param>
        /// <param name="sum_y_mul_dx">(변화량 * 픽셀 위치) 의 합.</param>
        /// <returns></returns>
        private float? CalculateEdgeByEdgeDirection(float sum_dx, float sum_y_mul_dx)
        {
            //반환값이 null일 경우 에지 추가 안함
            if (sum_dx == 0) return null;
            var res = sum_y_mul_dx / sum_dx - (float)0.5;
            switch (EdgeDirection)
            {
                case EDirection.DarkToLight:
                    if (res > 0) return null;
                    else return res;
                case EDirection.LightToDark:
                    if (res < 0) return null;
                    else return res;
                case EDirection.Any:
                default:
                    return res;
            }
        }
        #endregion
    }
}
