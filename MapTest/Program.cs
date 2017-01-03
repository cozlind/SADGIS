using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ESRI.ArcGIS.Geometry;

namespace MapTest
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            ESRI.ArcGIS.RuntimeManager.Bind(ESRI.ArcGIS.ProductCode.EngineOrDesktop);
            Application.Run(new MainForm());
        }
    #region 参数传递
        public static OptionData optionData = new OptionData(null, 112.414, 34.65, 500, true, true, Stability.STAB_A, 5.0, 0, 50, "秒", 100, false, 2100, 5, 5);
        #region 统计数据
        public static List<List<Position>> shpPoints = new List<List<Position>>();
        public static int posLayerNum = 0;
        #endregion
        #region 图层控制
        public static bool hasAnalysed = false;
        public static double xmin = double.PositiveInfinity;
        public static double ymin = double.PositiveInfinity;
        public static double xmax = double.NegativeInfinity;
        public static double ymax = double.NegativeInfinity;
        #endregion
        #region 时间控制参数
        public static readonly int interval = 300;//刷新间隔
        public static int timeStep = 5;//时间参数增长步长
        public static int curTime = 0;
        public static double limitMinC = 0.000000001;
        #endregion
    }
    public enum Stability { STAB_A, STAB_B, STAB_C, STAB_D, STAB_E, STAB_F };
    public struct OptionData
    {
        public string searchPollutionSource;
        public double sourceX, sourceY;
        public double sourceIntensity;

        public bool inCity;
        public bool isDay;
        public Stability atmosphericStability;
        public double windSpeed;
        public double windAngle;

        public double tunHeight;
        public string timeUnit;
        public double continueTime;
        public bool krikin;

        public double heatEmission;
        public double inradium;
        public double smokeVelocity;

        public OptionData(
            string a, 
            double bx,
            double by, 
            double d,

            bool f,
            bool day, 
            Stability g,
            double h,
            double i, 

            double j,
            string k,
            double l,
            bool m,

            double n,
            double o,
            double p
            )
        {
            searchPollutionSource = a;//查找污染源
            sourceX = bx;//事故点坐标x
            sourceY = by;//事故点坐标y
            sourceIntensity = d;//污染强度

            inCity = f;//地域//没用上
            isDay = day;//是否在白天
            atmosphericStability = g;//大气稳定度
            windSpeed = h;//气象站高度的风速
            windAngle = i;//风向弧度角

            tunHeight = j;//烟囱高度
            timeUnit = k;
            continueTime = l;//持续时间
            krikin = m;//插值方法

            heatEmission = n;//热排放率
            inradium = o;//烟囱内径
            smokeVelocity = p;//烟流出口流速
        }
    }
    #endregion
}
