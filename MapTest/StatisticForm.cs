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
using System.Data.Odbc;
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
    public partial class StatisticForm : Form
    {
        private MainForm _form;
        private List<List<Position>> Points;
        private Dictionary<string, List<Position>> dicPoints = new Dictionary<string, List<Position>>();
        private dynamic pageNum;
        private bool model;
        public StatisticForm(MainForm form, List<List<Position>> ps,bool isCalScale)
        {
            InitializeComponent();
            _form = form;
            model = isCalScale;
            if (model)
            {
                label1.Text = "等值线浓度";
                Points = ps;
            }
            else
            {
                label1.Text = "图层名称";
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
            }

            loadCombobox();
        }
        #region 下拉框控制
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (model)
            {
                pageNum = (int)comboBox1.Items.IndexOf(comboBox1.SelectedItem);
            }
            else
            {
                pageNum = (string)comboBox1.SelectedItem;
            }
            loadData();
        }
        //读取下拉框中数据
        public void loadCombobox()
        {
            if (model)
            {
                foreach (var posList in Points)
                {
                    comboBox1.Items.Add(posList[0].pollution);
                }
            }
            else
            {
                Dictionary<string, List<Position>>.KeyCollection keys = dicPoints.Keys;
                foreach (var key in keys)
                {
                    comboBox1.Items.Add(key);
                }
            }
        }
        #endregion 
        #region 表格控制
        //读取表格中数据
        public void loadData()
        {
            dataGridView1.Rows.Clear();
            if (model)
            {
                DataGridViewColumnCollection cs = dataGridView1.Columns;
                cs[3].HeaderText = "图层名称";
                foreach (var pos in Points[pageNum])
                    dataGridView1.Rows.Add(pos.name, pos.X, pos.Y, pos.shpname);
            }
            else
            {
                DataGridViewColumnCollection cs = dataGridView1.Columns;
                cs[3].HeaderText = "污染浓度";
                foreach (var pos in dicPoints[pageNum])
                    dataGridView1.Rows.Add(pos.name, pos.X, pos.Y, pos.pollution);
            }
        }
        #endregion

    }
}
