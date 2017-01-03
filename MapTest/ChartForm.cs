using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
namespace MapTest
{
    public partial class ChartForm : Form
    {
        private MainForm _form;        
        private Dictionary<string, List<Position>> dicPoints = new Dictionary<string, List<Position>>();
        private List<List<Position>> Points;
        public ChartForm() 
        {
            InitializeComponent();
            loadData();
        }
        public ChartForm(MainForm form, List<List<Position>> ps)
        {
            InitializeComponent();
            _form = form;
            Points = ps;
            foreach (var pspoints in ps)
            {
                foreach (var pspoint in pspoints)
                {
                    if (dicPoints.ContainsKey(pspoint.shpname))
                    {
                        dicPoints[pspoint.shpname].Add(pspoint);
                    }
                    else
                    {
                        List<Position> newList=new List<Position>();
                        newList.Add(pspoint);
                        dicPoints.Add(pspoint.shpname, newList);
                    }
                }
            }
            loadData();
        }
        private void loadData()
        {
            List<double> counts1 = new List<double>();
            foreach (var key in dicPoints.Keys)
            {
                counts1.Add(dicPoints[key].Count);
            }
            chart1.Series[0].Points.DataBindXY(dicPoints.Keys, counts1);
            chart1.ChartAreas[0].AxisY.Title = "单位：个";  

            foreach (var list in Points)
            {
                chart2.Series[0].Points.AddXY(list[0].pollution.ToString(), list.Count);
            }
            chart2.ChartAreas[0].AxisY.Title = "单位：个";  
        }
        //保存图表1
        private void button1_Click(object sender, EventArgs e)
        {
            chart1.SaveImage(System.Windows.Forms.Application.StartupPath + "\\ChartTempFile.jpg", System.Windows.Forms.DataVisualization.Charting.ChartImageFormat.Jpeg);
            //临时文件
            Image image = Image.FromFile(System.Windows.Forms.Application.StartupPath + "\\ChartTempFile.jpg");
            SaveFileDialog savedialog = new SaveFileDialog();
            savedialog.Filter = "Jpg 图片|*.jpg|Bmp 图片|*.bmp|Gif 图片|*.gif|Png 图片|*.png|Wmf  图片|*.wmf";
            savedialog.FilterIndex = 0;
            savedialog.RestoreDirectory = true;
            savedialog.FileName = System.DateTime.Now.ToString("yyyyMMddHHmmss") + "按图层统计";
            if (savedialog.ShowDialog() == DialogResult.OK)
            {
                image.Save(savedialog.FileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                MessageBox.Show(this, "图片保存成功！", "提醒");
            }
        }
        //保存图表2
        private void button2_Click(object sender, EventArgs e)
        {
            chart2.SaveImage(System.Windows.Forms.Application.StartupPath + "\\ChartTempFile.jpg", System.Windows.Forms.DataVisualization.Charting.ChartImageFormat.Jpeg);
            //临时文件
            Image image = Image.FromFile(System.Windows.Forms.Application.StartupPath + "\\ChartTempFile.jpg");
            SaveFileDialog savedialog = new SaveFileDialog();
            savedialog.Filter = "Jpg 图片|*.jpg|Bmp 图片|*.bmp|Gif 图片|*.gif|Png 图片|*.png|Wmf  图片|*.wmf";
            savedialog.FilterIndex = 0;
            savedialog.RestoreDirectory = true;
            savedialog.FileName = System.DateTime.Now.ToString("yyyyMMddHHmmss") + "按等值线浓度统计";
            if (savedialog.ShowDialog() == DialogResult.OK)
            {
                image.Save(savedialog.FileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                MessageBox.Show(this, "图片保存成功！", "提醒");
            }
        }
    }
}
