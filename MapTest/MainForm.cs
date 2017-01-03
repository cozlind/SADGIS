using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Timers;
using System.IO;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.DataSourcesRaster;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.Geoprocessing;
using ESRI.ArcGIS.Geoprocessor;
using ESRI.ArcGIS.GeoAnalyst;
using ESRI.ArcGIS.SpatialAnalyst;
using ESRI.ArcGIS.ADF.BaseClasses;
using ESRI.ArcGIS.ADF;
using ESRI.ArcGIS.ADF.Connection.Local;
using ESRI.ArcGIS.SystemUI;

namespace MapTest
{
    public partial class MainForm : Form
    {
        #region 时间控制
        public MainForm()
        {
            InitializeComponent();
            timeControl = new System.Timers.Timer(Program.interval);
            timeControl.Enabled = false;
            timeControl.Elapsed += new ElapsedEventHandler(OnTimerElapsed);
        }
        private System.Timers.Timer timeControl;//计时器
        private delegate void timeDelegate();//委托
        public bool timePermission=false;
        //每隔interval执行一次
        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (timePermission)
            {
                timePermission = false;
                Program.curTime += Program.timeStep;
                timeDelegate timedelegate = new timeDelegate(updateLayer);
                axMapControl1.BeginInvoke(timedelegate);
                if (Program.curTime > Program.optionData.continueTime)
                {
                    timeControl.Enabled = false;
                    return;
                }
            }
        }
        //动态开关
        private void toolStripSplitButton1_ButtonClick(object sender, EventArgs e)
        {
            Image playImage = Image.FromFile("./image/play.jpg");
            Image pauseImage = Image.FromFile("./image/pause.jpg");
            toolStripSplitButton1.Image = timeControl.Enabled ? playImage : pauseImage;
            dynamicEnable();
        }
        public void dynamicEnable()
        {
            timeControl.Enabled = !timeControl.Enabled;
            CustomedZoom(Program.optionData.sourceX, Program.optionData.sourceY, 0.02);
        }
        //更新分析、栅格、等值线图层
        private void updateLayer()
        {
            //更新界面元素
            if (Program.curTime > Program.optionData.continueTime)
            {
                toolStripProgressBar1.Value = 0;
                toolStripSplitButton1.Visible = false;
                toolStripStatusLabel3.Text = "模拟完毕";
                return;
            }
            Program.hasAnalysed = true;
            toolStripProgressBar1.Value = Convert.ToInt32(100 * Program.curTime / Program.optionData.continueTime);
            toolStripSplitButton1.Visible = true;
            toolStripStatusLabel3.Text = "当前时间 : " + Program.curTime.ToString() + " s";
            //更新参数
            height = Program.optionData.tunHeight;
            windspeed = Program.optionData.windSpeed;
            Q = Program.optionData.sourceIntensity;
            IPoint source = new PointClass();
            source.PutCoords(Program.optionData.sourceX, Program.optionData.sourceY);
            IPoint sourcePRJ = GCStoPRJ(source);
            double windDirec = Program.optionData.windAngle;//风向，弧度

            bool firstTime = false;
            if (CYDTable == null)
            {
                featureCla = createShp(esriGeometryType.esriGeometryPoint);
                CYDTable = featureCla as ITable;
                pOutRasterLayer = new RasterLayerClass();
                ILayerEffects lyrEffects = (ILayerEffects)pOutRasterLayer;
                lyrEffects.Transparency = 70;
                pOutRasterLayer.Name = "栅格图像";
                pFLayercontour = new FeatureLayerClass();
                pFLayercontour.Name = "等值线";
                pLayercontour = pFLayercontour as ILayer;
                pFeatureLayer = new FeatureLayerClass();
                pFeatureLayer.Name = "分析";
                firstTime = true;
            }
            CYDTable.DeleteSearchedRows(null);
            int shapeIndex = CYDTable.FindField("Shape");
            int concenIndex = CYDTable.FindField("Concen");
            IPoint tempPt = new PointClass();   //用于存储抽样点

            int minC_XLeft = 0;
            int stepX = 200;
            int stepY = 150;
            minC = Program.limitMinC;
            bool isFront = true;
            double concen = 0;
            for (int relateX = 1, i = 1; i <= 2; relateX += stepX)
            {
                int relateY = 0;
                double lastConcen = concen;
                concen = dynamicCalculate(relateX, relateY);   //Calculate为高斯模型计算
                if (isFront && lastConcen > concen)
                {
                    //isFront = false;
                    //minC = lastConcen / 10;
                    //relateX = 1;
                    if (lastConcen < Program.limitMinC)
                    {
                        Program.curTime = (int)Program.optionData.continueTime+1;
                        MessageBox.Show("全部浓度均低于最小显示浓度,请尝试调小最小显示浓度", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        toolStripProgressBar1.Value = 0;
                        toolStripSplitButton1.Visible = false;
                        toolStripStatusLabel3.Text = "模拟完毕";
                        return;
                    }
                    continue;
                }

                if (isFront && concen < minC)
                    continue;
                if (!isFront && concen < minC)
                {
                    minC_X = relateX - stepX;
                    i++;
                }
                if (isFront)
                {
                    if (relateX > 1)
                        relateX -= stepX;
                    concen = lastConcen;
                    minC_XLeft = relateX;
                }
                isFront = false;
                for (int j = 1; j <= 2; )
                {
                    //计算实际坐标，再转换成经纬度坐标
                    double x = relateX * Math.Cos(windDirec) - relateY * Math.Sin(windDirec) + sourcePRJ.X;
                    double y = relateX * Math.Sin(windDirec) + relateY * Math.Cos(windDirec) + sourcePRJ.Y;
                    tempPt = PRJtoGCS(x, y);
                    //写入文件
                    IRow CYDRow;
                    CYDRow = CYDTable.CreateRow();
                    CYDRow.set_Value(shapeIndex, tempPt);
                    CYDRow.set_Value(concenIndex, concen);
                    CYDRow.Store();
                    //对称写入下方的点
                    if (relateY != 0)
                    {
                        int relateY2 = -relateY;
                        x = relateX * Math.Cos(windDirec) - relateY2 * Math.Sin(windDirec) + sourcePRJ.X;
                        y = relateX * Math.Sin(windDirec) + relateY2 * Math.Cos(windDirec) + sourcePRJ.Y;
                        tempPt = PRJtoGCS(x, y);
                        CYDRow = CYDTable.CreateRow();
                        CYDRow.set_Value(shapeIndex, tempPt);
                        CYDRow.set_Value(concenIndex, concen);
                        CYDRow.Store();
                    }
                    relateY += stepY;
                    concen = dynamicCalculate(relateX, relateY);   //Calculate为高斯模型计算
                    if (concen < minC)
                    {
                        minC_Y = minC_Y >= relateY ? minC_Y : relateY;
                        j++;
                    }
                }
            }
            minC_Y -= stepY;
            pFeatureLayer.FeatureClass = featureCla;
            //创建栅格边界
            source = PRJtoGCS(sourcePRJ.X + minC_XLeft * Math.Cos(windDirec), sourcePRJ.Y + minC_XLeft * Math.Sin(windDirec));
            sourcePRJ = GCStoPRJ(source);
            IEllipticArc ell = new EllipticArcClass();
            IPoint majorPt = PRJtoGCS((minC_X - minC_XLeft) * Math.Cos(windDirec) + sourcePRJ.X, (minC_X - minC_XLeft) * Math.Sin(windDirec) + sourcePRJ.Y);
            double major = Math.Pow(Math.Pow(majorPt.X - source.X, 2) + Math.Pow(majorPt.Y - source.Y, 2), 0.5) / 2;
            double angle = (Math.Atan2(majorPt.Y - source.Y, majorPt.X - source.X) + 2 * Math.PI) % (2 * Math.PI);
            IPoint center = new PointClass();
            center.PutCoords((source.X + majorPt.X) / 2, (source.Y + majorPt.Y) / 2);
            double majorRatio = minC_Y * 2.0 / (minC_X - minC_XLeft);
            if (majorRatio < 1)
                ell.PutCoordsByAngle(false, center, 0, 2 * Math.PI, angle, major, majorRatio);
            else
                ell.PutCoordsByAngle(false, center, 0, 2 * Math.PI, angle + Math.PI / 2, major * majorRatio, 1 / majorRatio);
            ISegmentCollection pPolygon = new PolygonClass();
            pPolygon.AddSegment(ell as ISegment);
            //创建栅格
            IRaster pRaster = CreateRaster(pFeatureLayer, "Concen", (IPolygon)pPolygon);
            //分级渲染
            SetRsLayerClassifiedColor(pRaster, 10);
            pOutRasterLayer.CreateFromRaster(pRaster);
            pOutRasterLayer.Renderer = pClassRen as IRasterRenderer;
            if (firstTime){
                axMapControl1.AddLayer(pLayercontour);
                axMapControl1.Map.AddLayer(pFeatureLayer);
                axMapControl1.AddLayer(pOutRasterLayer);
            }
            else
            {
                for (int i = 0; i < axMapControl1.LayerCount; i++)
                    if (axMapControl1.get_Layer(i).Name.Equals("栅格图像"))
                        axMapControl1.DeleteLayer(i);
                axMapControl1.AddLayer(pOutRasterLayer);
            }
            //画等值线
            CreateContour(pRaster);
            axMapControl1.Refresh();

            timePermission = true;
        }
        #endregion
        #region 鼠标控制
        private void axMapControl1_OnMouseDown(object sender, IMapControlEvents2_OnMouseDownEvent e)
        {
            if (e.button == 2)
                this.axMapControl1.Extent = this.axMapControl1.TrackRectangle();
            //else if (e.button == 2)
            //    this.axMapControl1.Extent = this.axMapControl1.FullExtent;
            else if (e.button == 4)
                this.axMapControl1.Pan();
        }
        private void axMapControl1_OnMouseMove(object sender, IMapControlEvents2_OnMouseMoveEvent e)
        {
            toolStripStatusLabel5.Text = "比例尺 1:" + ((long)this.axMapControl1.MapScale).ToString();
            toolStripStatusLabel2.Text = "当前坐标 : X=" + e.mapX.ToString() + "°  Y=" + e.mapY.ToString() + "° ";
        }
        #endregion
        #region 窗体功能
        //帮助
        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(System.Windows.Forms.Application.StartupPath+"\\help\\Guidebook.pdf");
        }
        //导入shapefile
        string[] shpfilename = null;
        OpenFileDialog openFileDialog1 = new OpenFileDialog();
        private void 导入shapefile文件ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Program.shpPoints = new List<List<Position>>();
            dbffilename = new List<string>();

            string shpfilepath = "";
            openFileDialog1.Filter = "shapefiles(*.shp)|*.shp|All files(*.*)|*.*";//打开文件路径
            openFileDialog1.Multiselect = true;
            openFileDialog1.InitialDirectory = ".";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //shpfilename = openFileDialog1.FileName.Replace(shpfilepath, "");
                shpfilename = openFileDialog1.FileNames;
                foreach (String it in shpfilename)
                {
                    String eachName = it;

                    openFileDialog1.FileName = it;
                    BinaryReader br = new BinaryReader(openFileDialog1.OpenFile());
                    //读取文件过程
                    br.ReadBytes(24);
                    int FileLength = br.ReadInt32();//<0代表文件长度未知
                    int FileVersion = br.ReadInt32();//版本号
                    int ShapeType = br.ReadInt32();//几何类型
                    //几何所占范围
                    double xTemp = br.ReadDouble();
                    Program.xmin = xTemp < Program.xmin ? xTemp : Program.xmin;
                    double yTemp = br.ReadDouble();
                    Program.ymin = yTemp < Program.ymin ? yTemp : Program.ymin;
                    xTemp = br.ReadDouble();
                    Program.xmax = xTemp > Program.xmax ? xTemp : Program.xmax;
                    yTemp = br.ReadDouble();
                    Program.ymax = yTemp > Program.ymax ? yTemp : Program.ymax;

                    br.ReadBytes(32);
                    if (ShapeType == 1)//shp文件为Point类型
                    {
                        dbffilename.Add(eachName.Substring(0, eachName.LastIndexOf('.')) + ".dbf");

                        Program.shpPoints.Add(new List<Position>());
                        while (br.PeekChar() != -1)
                        {
                            Position position = new Position();
                            int startIndex = eachName.LastIndexOf('\\');
                            int endIndex = eachName.LastIndexOf('_');
                            position.shpname = eachName.Substring(startIndex + 1, endIndex - startIndex - 1);
                            uint RecordNum = br.ReadUInt32();
                            int DataLength = br.ReadInt32();

                            //读取第i个记录
                            br.ReadInt32();
                            position.PutCoords(br.ReadDouble(), br.ReadDouble());
                            Program.shpPoints[Program.posLayerNum].Add(position);
                        }
                        Program.posLayerNum++;
                    }
                    int nameIndex = eachName.LastIndexOf('\\');
                    shpfilepath = eachName.Substring(0, nameIndex + 1);
                    eachName = eachName.Substring(nameIndex);

                    axMapControl1.AddShapeFile(shpfilepath, eachName);
                }
            }

            loadDbf();
        }
        //新建
        private void 新建ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            清空图层ToolStripMenuItem_Click(sender, e);
            Program.optionData = new OptionData(null, 112, 34,  500, true, true, Stability.STAB_A, 5.0, 0, 50,"秒", 100, false, 2100, 5, 5);

        }
        //清空动态图层
        private void 清空动态图层ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            timeControl.Enabled = false;
            toolStripProgressBar1.Value = 0;
            toolStripSplitButton1.Image = Image.FromFile("./image/pause.jpg");
            toolStripSplitButton1.Visible = false;
            toolStripStatusLabel3.Text = "";
            Program.posLayerNum = 0;
            if (Program.hasAnalysed)
            {
                axMapControl1.DeleteLayer(0);
                axMapControl1.DeleteLayer(0);
                axMapControl1.DeleteLayer(0);

                Program.hasAnalysed = false;
                toolStripDropDownButton4.Enabled = false;
            }
            else
            {
                MessageBox.Show("无模拟图层", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        //清空所有图层
        private void 清空图层ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Program.posLayerNum = 0;
            timeControl.Enabled = false;
            toolStripProgressBar1.Value = 0;
            toolStripSplitButton1.Image = Image.FromFile("./image/pause.jpg");
            toolStripSplitButton1.Visible = false;
            toolStripStatusLabel3.Text = "";
            while (axMapControl1.LayerCount > 0)
            {
                axMapControl1.DeleteLayer(axMapControl1.LayerCount - 1);
            }
            axMapControl1.ClearLayers();
            Program.hasAnalysed = false;
            Program.xmin = double.PositiveInfinity;
            Program.ymin = double.PositiveInfinity;
            Program.xmax = double.NegativeInfinity;
            Program.ymax = double.NegativeInfinity;
            toolStripDropDownButton3.Enabled = false;
            toolStripDropDownButton4.Enabled = false;
        }
        //指定区域
        private void CustomedZoom(object sender, EventArgs e)
        {
            new ViewForm(this, true).Show();
        }
        //指定比例
        private void CustomedScale(object sender, EventArgs e)
        {
            new ViewForm(this, false).Show();
        }
        public void CustomedZoom(double x, double y, double scale = 0.008)
        {
            PolygonClass polygon = new PolygonClass();
            IPoint point = new PointClass();
            point.PutCoords(x - scale, y - scale);
            polygon.AddPoint(point);
            point.PutCoords(x - scale, y + scale);
            polygon.AddPoint(point);
            point.PutCoords(x + scale, y + scale);
            polygon.AddPoint(point);
            point.PutCoords(x + scale, y - scale);
            polygon.AddPoint(point);

            this.axMapControl1.Extent = polygon.Envelope;
        }
        //参数
        private void OptionButton_Click(object sender, EventArgs e)
        {
            toolStripDropDownButton3.Enabled = false;
            toolStripDropDownButton4.Enabled = false;
            new OptionForm(this).Show();
        }
        //分析
        private void AnalysisButton_Click(object sender, EventArgs e)
        {
            toolStripDropDownButton4.Enabled = false;
            new AnalysisForm(this).Show();
        }
        //导入dbf文件
        List<string> dbffilename = new List<string>();
        void loadDbf()
        {
            try
            {
                int index = 0;
                foreach (String it in dbffilename)
                {
                    String eachName = it;

                    BinaryReader br = new BinaryReader(File.Open(eachName, FileMode.Open), Encoding.Default);
                    //读取文件头 32bytes
                    br.ReadBytes(4);
                    int RecordNum = br.ReadInt32();
                    short HeaderByteNum = br.ReadInt16();
                    short RecordByteNum = br.ReadInt16();
                    br.ReadBytes(20);
                    //读取记录项 n*32bytes
                    int fieldscount = (HeaderByteNum - 32) / 32;
                    //读取记录项信息
                    br.ReadBytes(fieldscount * 32);
                    //读取文件头结束符
                    br.ReadByte();

                    //读取dbf文件记录 开始
                    for (int i = 0; i < RecordNum; i++)
                    {
                        byte[] bs = br.ReadBytes(61);
                        Char[] cs = Encoding.Default.GetChars(bs);
                        string id = new string(cs);

                        bs = br.ReadBytes(60);
                        cs = Encoding.Default.GetChars(bs);
                        string name = new string(cs);

                        Program.shpPoints[index][i].name = name;

                        bs = br.ReadBytes(931);
                        //cs = Encoding.Default.GetChars(bs);
                        //string o = new string(cs);
                    }
                    //读取dbf文件记录 结束
                    index++;
                    br.Close();
                }
            }
            catch (FileNotFoundException e)
            {
                string str = "无法关联到dbf文件，只能显示坐标信息，无法显示名称";
                MessageBox.Show(str, "提醒", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        //统计浓度
        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            if (Program.shpPoints == null || Program.shpPoints.Count == 0)
            {
                MessageBox.Show("无要素点数据", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            loadDbf();
            List<List<Position>> points;
            ContourStatistics(Program.shpPoints, pFLayercontour.FeatureClass, pFLayercontour.FeatureClass.FeatureCount(null), out points);
            //toolStripStatusLabel1.Text = test(pFLayercontour.FeatureClass).ToString() ;
            if (points == null || points.Count == 0)
            {
                MessageBox.Show("没有受污染的点", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            new StatisticForm(this, points, false).Show();
        }
        //统计范围
        private void toolStripMenuItem5_Click(object sender, EventArgs e)
        {
            if (Program.shpPoints == null || Program.shpPoints.Count == 0)
            {
                MessageBox.Show("无要素点数据", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            loadDbf();
            List<List<Position>> points;
            ContourStatistics(Program.shpPoints, pFLayercontour.FeatureClass, pFLayercontour.FeatureClass.FeatureCount(null), out points);
            //toolStripStatusLabel1.Text = test(pFLayercontour.FeatureClass).ToString() ;
            if (points == null || points.Count == 0)
            {
                MessageBox.Show("没有受污染的点", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            new StatisticForm(this, points, true).Show();
        }
        //统计数量
        private void 统计数量ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Program.shpPoints == null || Program.shpPoints.Count == 0)
            {
                MessageBox.Show("无要素点数据", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            loadDbf();
            List<List<Position>> points;
            ContourStatistics(Program.shpPoints, pFLayercontour.FeatureClass, pFLayercontour.FeatureClass.FeatureCount(null), out points);
            //toolStripStatusLabel1.Text = test(pFLayercontour.FeatureClass).ToString() ;
            if (points == null || points.Count == 0)
            {
                MessageBox.Show("没有受污染的点", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            } 
            new ChartForm(this, points).Show();
        }
        //退出
        private void 退出ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        #endregion
        #region 分析参数和模型
        //第一个参数是稳定度，第二个是0~1000,>1000
        private double[,] alpha1 = new double[,]{
            {0.901074,0.850934},
            {0.914370,0.865014},
            {0.924279,0.885157},
            {0.924418,0.888723},
            {0.920818,0.896864},
            {0.929418,0.888723}
        };
        private double[,] gamma1 = new double[,] { 
            { 0.425809, 0.602052 }, 
            { 0.281846, 0.396353 }, 
            { 0.177154, 0.232123 }, 
            { 0.110726, 0.146669 }, 
            { 0.086400, 0.101947 },
            { 0.055363, 0.073334 }
        };
        //第一个参数是稳定度，第二个是0~300,300~500,500~1000,1000~10000,>10000
        private double[,] alpha2 = new double[,]{
            {1.121540,1.523600,2.108810,2.108810,2.108810},
            {0.964435,0.964435,1.093560,1.093560,1.093560},
            {0.917595,0.917595,0.917595,0.917595,0.917595},
            {0.826212,0.826212,0.826212,0.632023,0.555360},
            {0.788370,0.788370,0.788370,0.565188,0.414743},
            {0.784400,0.784400,0.784400,0.525960,0.322659}
        };
        private double[,] gamma2 = new double[,]{
            {0.079990,0.008574,0.000211,0.000211,0.000211},
            {0.127190,0.127190,0.057025,0.057025,0.057025},
            {0.106803,0.106803,0.106803,0.106803,0.106803},
            {0.104634,0.104634,0.104634,0.400167,0.810763},
            {0.092752,0.092752,0.092752,0.433384,1.732410},
            {0.062076,0.062076,0.062076,0.370015,2.406910}
        };
        private double Py = 1.503;  //计算中使用的扩散参数Py、Qy、Pz、Qz 
        private double Qy = 0.833;
        private double Pz = 0.151;
        private double Qz = 1.219;
        private double windspeed = 5.0; //风速(m/s)
        private double height = 50.0;//排气筒距地面的几何高度（m）
        private double Q = 50000; //气载污染物源强，即释放率（mg/s）
        #region 坐标转换
        //平面直角坐标转大地坐标
        private IPoint PRJtoGCS(double x, double y)
        {
            IPoint pPoint = new PointClass();
            pPoint.PutCoords(x, y);
            ISpatialReferenceFactory pSRF = new SpatialReferenceEnvironmentClass();
            pPoint.SpatialReference = pSRF.CreateProjectedCoordinateSystem((int)esriSRProjCS4Type.esriSRProjCS_WGS1984_TM_116_SE);
            pPoint.Project(pSRF.CreateGeographicCoordinateSystem((int)esriSRGeoCSType.esriSRGeoCS_WGS1984));
            return pPoint;
        }
        //大地坐标转平面直角坐标
        private IPoint GCStoPRJ(IPoint pPoint)
        {
            IPoint tempPt = new PointClass();
            tempPt.PutCoords(pPoint.X, pPoint.Y);
            ISpatialReferenceFactory pSRF = new SpatialReferenceEnvironment();
            tempPt.SpatialReference = pSRF.CreateGeographicCoordinateSystem((int)esriSRGeoCSType.esriSRGeoCS_WGS1984);
            IProjectedCoordinateSystem pProjectCoodinateSys = pSRF.CreateProjectedCoordinateSystem((int)esriSRProjCS4Type.esriSRProjCS_WGS1984_TM_116_SE);
            ISpatialReference pSpatialReference = (ISpatialReference)pProjectCoodinateSys;
            //pSpatialReference.SetDomain(17352988.066800, 18230892.557100, 2326007.173500, 3237311.062300);
            tempPt.Project(pSpatialReference);
            return tempPt;
        }
        #endregion
        //得到Sigma
        private double getSigmaXY(double x)
        {
            if (x >= 0 && x <= 1000)
            {
                Qy = alpha1[Convert.ToInt32(Program.optionData.atmosphericStability), 0];
                Py = gamma1[Convert.ToInt32(Program.optionData.atmosphericStability), 0];
            }
            else
            {
                Qy = alpha1[Convert.ToInt32(Program.optionData.atmosphericStability), 1];
                Py = gamma1[Convert.ToInt32(Program.optionData.atmosphericStability), 1];
            }
            return this.Py * Math.Pow(x, Qy);
        }
        private double getSigmaZ(double x)
        {
            if (x >= 0 && x <= 300)
            {
                Qz = alpha2[Convert.ToInt32(Program.optionData.atmosphericStability), 0];
                Pz = gamma2[Convert.ToInt32(Program.optionData.atmosphericStability), 0];
            }
            else if (x > 300 && x <= 500)
            {
                Qz = alpha2[Convert.ToInt32(Program.optionData.atmosphericStability), 1];
                Pz = gamma2[Convert.ToInt32(Program.optionData.atmosphericStability), 1];
            }
            else if (x > 500 && x <= 1000)
            {
                Qz = alpha2[Convert.ToInt32(Program.optionData.atmosphericStability), 2];
                Pz = gamma2[Convert.ToInt32(Program.optionData.atmosphericStability), 2];
            }
            else if (x > 1000 && x < 10000)
            {
                Qz = alpha2[Convert.ToInt32(Program.optionData.atmosphericStability), 3];
                Pz = gamma2[Convert.ToInt32(Program.optionData.atmosphericStability), 3];
            }
            else
            {
                Qz = alpha2[Convert.ToInt32(Program.optionData.atmosphericStability), 4];
                Pz = gamma2[Convert.ToInt32(Program.optionData.atmosphericStability), 4];
            }
            return this.Pz * Math.Pow(x, Qz);
        }
        //得到抬升高度
        private double getUplift()
        {
            return (1.5 * Program.optionData.smokeVelocity * Program.optionData.inradium + 0.01 * Program.optionData.heatEmission) / windspeed;
        }
        //高斯模型
        //静态--高斯烟羽模型
        private double staticCalculate(double x, double y)
        {
            double _y, _z;
            _y = getSigmaXY(x);
            _z = getSigmaZ(x);
            double speed = Convert.ToDouble(this.windspeed);  //风速
            double _H = getUplift();   //烟囱抬升高度
            double He = Convert.ToDouble(this.height) + _H;   //计算有效排放高度
            double F = 2 * Math.Exp(-He * He / (2 * (_z * _z)));
            return (this.Q * Math.Exp(-y * y / (2 * (_y * _y))) * F) / (2 * Math.PI * _y * _z * speed);
        }
        //动态--高斯烟团模型
        private double dynamicCalculate(double x, double y)
        {
            double _x, _y, _z;
            _x = _y = getSigmaXY(x);
            _z = getSigmaZ(x);
            double speed = Convert.ToDouble(this.windspeed);  //风速
            double _H = getUplift();   //烟囱抬升高度
            double He = Convert.ToDouble(this.height) + _H;   //计算有效排放高度
            //double F = Math.Exp(-this.height * this.height / (2 * (_z * _z))) + Math.Exp(-4 * He * He / (2 * (_z * _z)));
            double F = 2 * Math.Exp(-He * He / (2 * (_z * _z)));
            double temp = (this.Q * Math.Exp(-(x - speed * Program.curTime) * (x - speed * Program.curTime) / (2 * (_x * _x))) * Math.Exp(-y * y / (2 * (_y * _y))) * F) / (Math.Pow(2 * Math.PI, 3 / 2) * _x * _y * _z);
            return temp;
        }
        private void staticBoundaryCal(out double maxC, out double minC, out int minX, out int minY)
        {
            Qy = alpha1[Convert.ToInt32(Program.optionData.atmosphericStability), 1];
            Py = gamma1[Convert.ToInt32(Program.optionData.atmosphericStability), 1];
            Qz = alpha2[Convert.ToInt32(Program.optionData.atmosphericStability), 4];
            Pz = gamma2[Convert.ToInt32(Program.optionData.atmosphericStability), 4];

            int maxX = (int)Math.Pow(Qz * (height + 10) * (height + 10) * (Qy + Qz) / Pz / Pz, 1.0 / 2.0 / Qz);
            maxC = Math.Pow(maxX, -Qy - Qz) / (Math.PI * Py * Pz * windspeed / Q) * Math.Exp(-(height + 10) * (height + 10) / 2 / Pz / Pz / Math.Pow(maxX, 2 * Qz));
            minC = maxC / 100.0;
            double target = minC * Math.PI * Py * Pz * windspeed / Q;
            minX = 200;
            double result = Math.Pow(minX, -Qy - Qz) * Math.Exp(-(height + 10) * (height + 10) / 2 / Pz / Pz / Math.Pow(minX, 2 * Qz));
            while (result > target)
            {
                minX += 200;
                result = Math.Pow(minX, -Qy - Qz) * Math.Exp(-(height + 10) * (height + 10) / 2 / Pz / Pz / Math.Pow(minX, 2 * Qz));
            }
            minY = (int)Math.Pow(-Math.Log(0.01) * 2 * Py * Py * Math.Pow(maxX, 2 * Qy), 0.5);
        }
        #endregion
        #region 绘图控制
        private IFeatureClass featureCla;
        private ITable CYDTable;
        private IFeatureLayer pFeatureLayer;
        private IRasterLayer pOutRasterLayer;
        private IFeatureLayer pFLayercontour;
        private ILayer pLayercontour;
        private IRasterClassifyColorRampRenderer pClassRen;
        private ISegmentCollection borderPolygon;
        private int minC_X, minC_Y = 0;
        private double minC = 0.000005;
        //静态绘制
        public void staticAnalysis()
        {
            int stepX = 150;
            int stepY = 100;
            windspeed = Program.optionData.windSpeed;
            Q = Program.optionData.sourceIntensity;
            IPoint source = new PointClass();
            source.PutCoords(Program.optionData.sourceX, Program.optionData.sourceY);
            IPoint sourcePRJ = GCStoPRJ(source);
            double windDirec = Program.optionData.windAngle;//风向，弧度
            minC = Program.limitMinC;

            bool firstTime = false;
            if (CYDTable == null)
            {
                featureCla = createShp(esriGeometryType.esriGeometryPoint);
                CYDTable = featureCla as ITable;
                pOutRasterLayer = new RasterLayerClass();
                ILayerEffects lyrEffects = (ILayerEffects)pOutRasterLayer;
                lyrEffects.Transparency = 70;
                pOutRasterLayer.Name = "栅格图像";
                pFLayercontour = new FeatureLayerClass();
                pFLayercontour.Name = "等值线";
                pLayercontour = pFLayercontour as ILayer;
                pFeatureLayer = new FeatureLayerClass();
                pFeatureLayer.Name = "分析";
                firstTime = true;
            }
            CYDTable.DeleteSearchedRows(null);
            int shapeIndex = CYDTable.FindField("Shape");
            int concenIndex = CYDTable.FindField("Concen");
            IPoint tempPt = new PointClass();   //用于存储抽样点


            for (int relateX = 1; ; )
            {
                int relateY = 0;
                double concen = staticCalculate(relateX, relateY);   //Calculate为高斯模型计算
                if (relateX != 1 && concen < minC)
                {
                    minC_X = relateX - stepX;
                    minC_Y -= stepY;
                    break;
                }
                while (true)
                {
                    //计算实际坐标，再转换成经纬度坐标
                    double x = relateX * Math.Cos(windDirec) - relateY * Math.Sin(windDirec) + sourcePRJ.X;
                    double y = relateX * Math.Sin(windDirec) + relateY * Math.Cos(windDirec) + sourcePRJ.Y;
                    tempPt = PRJtoGCS(x, y);
                    //写入文件
                    IRow CYDRow = CYDTable.CreateRow();
                    CYDRow.set_Value(shapeIndex, tempPt);
                    CYDRow.set_Value(concenIndex, concen);
                    CYDRow.Store();
                    //对称写入下方的点
                    if (relateY != 0)
                    {
                        int relateY2 = -relateY;
                        x = relateX * Math.Cos(windDirec) - relateY2 * Math.Sin(windDirec) + sourcePRJ.X;
                        y = relateX * Math.Sin(windDirec) + relateY2 * Math.Cos(windDirec) + sourcePRJ.Y;
                        tempPt = PRJtoGCS(x, y);
                        CYDRow = CYDTable.CreateRow();
                        CYDRow.set_Value(shapeIndex, tempPt);
                        CYDRow.set_Value(concenIndex, concen);
                        CYDRow.Store();
                    }
                    relateY += stepY;
                    concen = staticCalculate(relateX, relateY);   //Calculate为高斯模型计算
                    if (concen < minC)
                    {
                        minC_Y = minC_Y >= relateY ? minC_Y : relateY;
                        break;
                    }
                }
                relateX += stepX;
            }

            pFeatureLayer.FeatureClass = featureCla;
            //创建栅格边界
            IEllipticArc ell = new EllipticArcClass();
            IPoint majorPt = PRJtoGCS((minC_X) * Math.Cos(windDirec) + sourcePRJ.X, (minC_X) * Math.Sin(windDirec) + sourcePRJ.Y);
            double major = Math.Pow(Math.Pow(majorPt.X - source.X, 2) + Math.Pow(majorPt.Y - source.Y, 2), 0.5) / 2;
            double angle = (Math.Atan2(majorPt.Y - source.Y, majorPt.X - source.X) + 2 * Math.PI) % (2 * Math.PI);
            IPoint center = new PointClass();
            center.PutCoords((source.X + majorPt.X) / 2, (source.Y + majorPt.Y) / 2);
            double majorRatio = minC_Y * 2.0 / (minC_X);
            if (majorRatio < 1)
                ell.PutCoordsByAngle(false, center, 0, 2 * Math.PI, angle, major, majorRatio);
            else
                ell.PutCoordsByAngle(false, center, 0, 2 * Math.PI, angle + Math.PI / 2, major * majorRatio, 1 / majorRatio);
            borderPolygon = new PolygonClass();
            borderPolygon.AddSegment(ell as ISegment);
            //创建栅格
            IRaster pRaster = CreateRaster(pFeatureLayer, "Concen", (IPolygon)borderPolygon);
            //分级渲染
            SetRsLayerClassifiedColor(pRaster, 10);
            pOutRasterLayer.CreateFromRaster(pRaster);
            pOutRasterLayer.Renderer = pClassRen as IRasterRenderer;
            if (firstTime)
            {
                axMapControl1.AddLayer(pLayercontour);
                axMapControl1.AddLayer(pOutRasterLayer);
                axMapControl1.Map.AddLayer(pFeatureLayer);
            }
            else
            {
                for (int i = 0; i < axMapControl1.LayerCount; i++)
                    if (axMapControl1.get_Layer(i).Name.Equals("栅格图像"))
                        axMapControl1.DeleteLayer(i);
                axMapControl1.AddLayer(pOutRasterLayer);
            }
            //画等值线
            CreateContour(pRaster);
            axMapControl1.Refresh();
            //已经生成图层


            Program.hasAnalysed = true;
            CustomedZoom(center.X, center.Y, 0.02);
            toolStripProgressBar1.Value = 0;
            toolStripSplitButton1.Visible = false;
            toolStripStatusLabel3.Text = "模拟完毕";
            toolStripDropDownButton4.Enabled = true;
        }
        //创建shp文件及工作空间
        private IFeatureClass createShp(esriGeometryType geoType)
        {
            //==============打开工作空间========
            IWorkspaceFactory workspaceFactory = new InMemoryWorkspaceFactoryClass();
            IWorkspaceName workspaceName = workspaceFactory.Create("./temp/", "MyWorkspace", null, 0);
            IName name = (IName)workspaceName;
            IWorkspace inmemWor = (IWorkspace)name.Open();
            //IWorkspaceFactory pWF = new ShapefileWorkspaceFactoryClass();
            //IFeatureWorkspace pFWs = pWF.OpenFromFile(".", 0) as IFeatureWorkspace;

            IGeometryDef pGeometryDef = new GeometryDefClass();
            IGeometryDefEdit pGeometryDefEdit = pGeometryDef as IGeometryDefEdit;
            pGeometryDefEdit.GeometryType_2 = geoType;
            //============设置坐标系及边界========
            pGeometryDefEdit.SpatialReference_2 = axMapControl1.SpatialReference;
            ISpatialReferenceFactory2 ipSpaRefFa = new SpatialReferenceEnvironmentClass();
            IGeographicCoordinateSystem ipGeoCorSys = new GeographicCoordinateSystemClass();
            ipGeoCorSys = ipSpaRefFa.CreateGeographicCoordinateSystem((int)esriSRGeoCSType.esriSRGeoCS_WGS1984);    //设置坐标系
            ISpatialReference ipSpaRef = ipGeoCorSys;
            ipSpaRef.SetDomain(Program.xmin, Program.xmax, Program.ymin, Program.ymax);   //设置边界
            pGeometryDefEdit.SpatialReference_2 = ipSpaRef;
            //============开始添加字段========
            IFields pFields = new Fields();
            IFieldsEdit pFieldsEdit = pFields as IFieldsEdit;
            IField pField = new Field();
            //==============添加Shape字段========
            IFieldEdit pFieldEdit = pField as IFieldEdit;
            pFieldEdit.Type_2 = esriFieldType.esriFieldTypeGeometry;
            pFieldEdit.GeometryDef_2 = pGeometryDef;
            pFieldEdit.Name_2 = "Shape";
            pFieldsEdit.AddField(pField);
            //==============添加Concentration字段========
            pField = new Field();
            pFieldEdit = pField as IFieldEdit;
            pFieldEdit.Type_2 = esriFieldType.esriFieldTypeDouble;
            pFieldEdit.Name_2 = "Concen";
            pFieldEdit.AliasName_2 = "Concen";
            pFieldsEdit.AddField(pField);
            //=============创建shp文件===================
            IFeatureClass pFC;
            pFC = (inmemWor as IFeatureWorkspace).CreateFeatureClass("analyse.shp", pFields, null, null, esriFeatureType.esriFTSimple, "Shape", "");
            return pFC;
        }
        //插值创建栅格图像
        private IRaster CreateRaster(IFeatureLayer pFeatureLayer, String field, IPolygon barried)
        {
            IInterpolationOp pInterpolationOp = new RasterInterpolationOpClass();
            IGeoDataset pInputDataset = (IGeoDataset)pFeatureLayer.FeatureClass;
            IRasterRadius pRadius = new RasterRadiusClass();
            double s = 10;
            object obs = s;
            pRadius.SetVariable(35);
            //设置高程字段
            IFeatureClassDescriptor pFCDescriptor = new FeatureClassDescriptor() as IFeatureClassDescriptor;
            pFCDescriptor.Create(pFeatureLayer.FeatureClass, null, field);
            //设置像元大小和插值边界
            double dCellSize = 0.0001;
            object oCell = dCellSize;
            object extent = barried.Envelope;
            IRasterAnalysisEnvironment pEnv = (IRasterAnalysisEnvironment)pInterpolationOp;
            pEnv.SetCellSize(esriRasterEnvSettingEnum.esriRasterEnvValue, ref oCell);
            pEnv.SetExtent(esriRasterEnvSettingEnum.esriRasterEnvValue, ref extent);

            IRaster pOutRaster=new RasterClass();
            try
            {
                //进行差值
                if (Program.optionData.krikin)
                {
                    pOutRaster = pInterpolationOp.Krige(pFCDescriptor as IGeoDataset, esriGeoAnalysisSemiVariogramEnum.esriGeoAnalysisExponentialSemiVariogram, pRadius, false) as IRaster;
                }
                else
                {
                    pOutRaster = pInterpolationOp.IDW(pFCDescriptor as IGeoDataset, 2, pRadius) as IRaster;

                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "提醒", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            //用polygon对图像进行裁剪
            IExtractionOp2 extract = new RasterExtractionOpClass();
            IFeatureClass tempFeaCla = createShp(esriGeometryType.esriGeometryPolygon);
            IFeature fea = tempFeaCla.CreateFeature();
            fea.Shape = barried;
            fea.Store();
            pOutRaster = (IRaster)extract.Raster((IGeoDataset)pOutRaster, (IGeoDataset)tempFeaCla);

            return pOutRaster;
        }
        //对栅格文件分级上色
        public void SetRsLayerClassifiedColor(IRaster pRaster, int iColorRampSize)
        {
            //IRasterClassifyColorRampRenderer是用于对栅格图像进行分级渲染处理的接口，IRasterRenderer是对栅格图像进行渲染处理的接口           
            pClassRen = new RasterClassifyColorRampRendererClass();
            IRasterRenderer pRasRen = pClassRen as RasterClassifyColorRampRendererClass;
            //Set raster for the render and update
            pRasRen.Raster = pRaster;
            pClassRen.ClassCount = iColorRampSize;
            //pRasRen.Update();
            //定义起点和终点颜色
            IColor pFromColor = new RgbColorClass();
            IRgbColor pRgbColor = pFromColor as IRgbColor;
            pRgbColor.Red = 0;
            pRgbColor.Green = 255;
            pRgbColor.Blue = 0;
            IColor pToColor = new RgbColorClass();
            pRgbColor = pToColor as IRgbColor;
            pRgbColor.Red = 255;
            pRgbColor.Green = 0;
            pRgbColor.Blue = 0;
            //创建颜色分级，采用IAlgorithmicColorRamp进行分级设色，需要明确定义开始和结束的颜色           
            IAlgorithmicColorRamp pRamp = new AlgorithmicColorRampClass();
            pRamp.Size = iColorRampSize;
            pRamp.FromColor = pFromColor;
            pRamp.ToColor = pToColor;
            bool ok = true;
            pRamp.CreateRamp(out ok);
            //获得栅格统计数值
            IRasterBandCollection pRsBandCol = pRaster as IRasterBandCollection;
            IRasterBand pRsBand = pRsBandCol.Item(0);
            pRsBand.ComputeStatsAndHist();
            //Create symbol for the classes
            IFillSymbol pFSymbol = new SimpleFillSymbolClass();
            for (int i = 0; i <= pClassRen.ClassCount - 1; i++)
            {
                if (i == 0)
                {
                    IColor color = pRamp.get_Color(i);
                    color.NullColor = true;
                    pFSymbol.Color = color;
                    pClassRen.set_Symbol(i, pFSymbol as ISymbol);
                }
                else
                {
                    pFSymbol.Color = pRamp.get_Color(i);
                    pClassRen.set_Symbol(i, pFSymbol as ISymbol);
                }
            }
        }
        //根据栅格绘制等值线
        private void CreateContour(IRaster pOutRaster)
        {
            ISurfaceOp pSurfaceOp = new RasterSurfaceOp() as ISurfaceOp;
            object odbase = 0;

            IRasterBandCollection pRsBandCol = pOutRaster as IRasterBandCollection;
            IRasterBand pRsBand = pRsBandCol.Item(0);
            IRasterStatistics pRasterStatistic = pRsBand.Statistics;
            double dMaxValue = pRasterStatistic.Maximum;
            double dMinValue = pRasterStatistic.Minimum;
            try
            {
            IFeatureClass pOutLineFC = pSurfaceOp.Contour(pOutRaster as IGeoDataset, (dMaxValue - dMinValue) / 10, ref odbase) as IFeatureClass;
            ITable pTable = pOutLineFC as ITable;
            int contourIndex = pTable.FindField("Contour");
            for (int i = 1; i < pTable.RowCount(null); i++)
            {
                IRow pRow = pTable.GetRow(i);
                if ((double)pRow.get_Value(contourIndex) < 0)
                    pRow.Delete();
            }
            pFLayercontour.FeatureClass = pOutLineFC;
            }
            catch (Exception e)
            {
                MessageBox.Show("创建等值线失败！请删除软件目录下所有sha文件和ras文件夹重试。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }
        #endregion
        #region 统计数据
        private void ContourStatistics(List<List<Position>> multiPoints, IFeatureClass contourFeatureCla, int contourNum, out List<List<Position>> outPoint)
        {
            ITable pTable = contourFeatureCla as ITable;
            int contourIndex = pTable.FindField("Contour");

            outPoint = new List<List<Position>>();

            List<List<Position>> pointss = new List<List<Position>>();
            foreach (var points in multiPoints)
            {
                pointss.Add(new List<Position>(points));
            }

            IFeatureCursor featureCursor = contourFeatureCla.Search(null, false);
            IFeature feature;
            for (int i = 0; i < contourNum; i++)
            {
                IRow pRow = pTable.GetRow(i);
                double pollution = (double)pRow.get_Value(contourIndex);

                feature = featureCursor.NextFeature();
                IPolyline pPolyline = (IPolyline)feature.Shape;

                List<Position> outList = new List<Position>();

                //IPoint testPt = new PointClass();
                //testPt.PutCoords(112.418774, 34.650722);
                //只判断封闭的等值线
                if (pPolyline.IsClosed)
                {
                    //将polyline转化为polygon
                    ISegmentCollection pRing = new RingClass();
                    pRing.AddSegmentCollection(pPolyline as ISegmentCollection);
                    IGeometryCollection pPolygon = new PolygonClass();
                    pPolygon.AddGeometry(pRing as IGeometry);
                    IRelationalOperator pRelOperator = pPolygon as IRelationalOperator;

                    //检索图层中的点
                    foreach (var points in pointss)
                    {
                        List<Position> removeList = new List<Position>();
                        foreach (var point in points)
                        {
                            if (pRelOperator.Contains(point))
                            {
                                point.pollution=pollution;

                                outList.Add(point);
                                removeList.Add(point);
                            }
                        }
                        foreach (var point in removeList)
                        {
                            points.Remove(point);
                        }
                    }
                }
                //判断是否在边界圈内，作为最低浓度的等值线
                else if (i == contourNum - 1)
                {
                    IRelationalOperator barriedRelOperator = borderPolygon as IRelationalOperator;
                    
                    //检索图层中的点
                    foreach (var points in pointss)
                    {
                        List<Position> removeList = new List<Position>();
                        foreach (var point in points)
                        {
                            if (barriedRelOperator.Contains(point))
                            {
                                point.pollution = pollution;
                                outList.Add(point);
                                removeList.Add(point);
                            }
                        }
                        foreach (var point in removeList)
                        {
                            points.Remove(point);
                        }
                    }
                }
                if (outList.Count >0)
                {
                    outPoint.Add(outList);
                }
            }
        }
        #endregion


    }
}

