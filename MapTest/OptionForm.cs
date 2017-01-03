using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MapTest
{
    public partial class OptionForm : Form
    {
        private MainForm _form;
        private double[,] m = new double[,] { { 0.10, 0.15, 0.20, 0.25, 0.30, 0.30 }, { 0.07, 0.07, 0.10, 0.15, 0.25, 0.25 } };
        public OptionForm(MainForm form)
        {
            InitializeComponent();
            //复原数据
            textBox1.Text = Program.optionData.searchPollutionSource;
            textBox3.Text = Program.optionData.sourceX+"";
            textBox4.Text = Program.optionData.sourceY+"";
            textBox5.Text = Program.optionData.sourceIntensity + "";
            if(Program.optionData.inCity)
                radioButton1.Checked = true;
            else
                radioButton2.Checked = true;
            if(Program.optionData.isDay)
                radioButton4.Checked = true;
            else
                radioButton3.Checked = true;
            if (Program.optionData.atmosphericStability == Stability.STAB_A) radioButton6.Checked = true;
            else if (Program.optionData.atmosphericStability == Stability.STAB_B) radioButton5.Checked = true;
            else if (Program.optionData.atmosphericStability == Stability.STAB_C) radioButton7.Checked = true;
            else if (Program.optionData.atmosphericStability == Stability.STAB_D) radioButton8.Checked = true;
            else if (Program.optionData.atmosphericStability == Stability.STAB_E) radioButton9.Checked = true;
            else radioButton10.Checked = true;
            textBox6.Text=Program.optionData.tunHeight+"";

            if (radioButton4.Checked)
                textBox8.Text = Program.optionData.windSpeed / Math.Pow(double.Parse(textBox6.Text) / 10, m[0, Convert.ToInt32(Program.optionData.atmosphericStability)]) + "";
            else
                textBox8.Text = Program.optionData.windSpeed / Math.Pow(double.Parse(textBox6.Text) / 10, m[1, Convert.ToInt32(Program.optionData.atmosphericStability)]) + "";
            if (Program.optionData.windAngle == 0) radioButton16.Checked = true;
            else if (Program.optionData.windAngle == 0.25 * Math.PI) radioButton18.Checked = true;
            else if (Program.optionData.windAngle == 0.5 * Math.PI) radioButton14.Checked = true;
            else if (Program.optionData.windAngle == 0.75 * Math.PI) radioButton17.Checked = true;
            else if (Program.optionData.windAngle == 1 * Math.PI) radioButton12.Checked = true;
            else if (Program.optionData.windAngle == 1.5 * Math.PI) radioButton15.Checked = true;
            else if (Program.optionData.windAngle == 1.25 * Math.PI) radioButton13.Checked = true;
            else if (Program.optionData.windAngle == 1.75 * Math.PI) radioButton11.Checked = true;
            textBox9.Text = Program.optionData.heatEmission + "";
            textBox10.Text = Program.optionData.inradium + "";
            textBox11.Text = Program.optionData.smokeVelocity + "";
            textBox7.Text = Program.optionData.continueTime+"";
            radioButton20.Checked = Program.optionData.krikin;

            _form = form;
        }
        //确认键
        private void confirmButton_Click(object sender, EventArgs e)
        {
            try
            {
                //大气稳定度
                Stability stability;
                if (radioButton6.Checked) stability = Stability.STAB_A;
                else if (radioButton5.Checked) stability = Stability.STAB_B;
                else if (radioButton7.Checked) stability = Stability.STAB_C;
                else if (radioButton8.Checked) stability = Stability.STAB_D;
                else if (radioButton9.Checked) stability = Stability.STAB_E;
                else stability = Stability.STAB_F;
                //气象台风速
                double windSpeed;
                if (radioButton4.Checked) { windSpeed = double.Parse(textBox8.Text) * Math.Pow(double.Parse(textBox6.Text) / 10, m[0, Convert.ToInt32(stability)]); }
                else { windSpeed = double.Parse(textBox8.Text) * Math.Pow(double.Parse(textBox6.Text) / 10, m[1, Convert.ToInt32(stability)]); }
                //风向弧度角
                double windAngle = 0;
                if (radioButton16.Checked) windAngle = 0;
                else if (radioButton18.Checked) windAngle = 0.25 * Math.PI;
                else if (radioButton14.Checked) windAngle = 0.5 * Math.PI;
                else if (radioButton17.Checked) windAngle = 0.75 * Math.PI;
                else if (radioButton12.Checked) windAngle = 1 * Math.PI;
                else if (radioButton15.Checked) windAngle = 1.5 * Math.PI;
                else if (radioButton13.Checked) windAngle = 1.25 * Math.PI;
                else if (radioButton11.Checked) windAngle = 1.75 * Math.PI;
                //源强
                double intensity = 0;
                switch (comboBox1.Text)
                {
                    case "mg/s":
                        intensity = double.Parse(textBox5.Text);
                        break;
                    case "g/s":
                        intensity = 1000 * double.Parse(textBox5.Text);
                        break;
                    case "kg/s":
                        intensity = 1000000 * double.Parse(textBox5.Text);
                        break;
                }
                //时间
                double time = 0;
                switch (comboBox2.Text)
                {
                    case "秒":
                        time = double.Parse(textBox7.Text);
                        break;
                    case "分钟":
                        time = 60 * double.Parse(textBox7.Text);
                        break;
                    case "小时":
                        time = 3600 * double.Parse(textBox7.Text);
                        break;
                }
                //返回数据
                Program.optionData = new OptionData(
                        textBox1.Text,
                        double.Parse(textBox3.Text),
                        double.Parse(textBox4.Text),
                        intensity,

                        radioButton1.Checked,
                        radioButton4.Checked,
                        stability,
                        windSpeed,
                        windAngle,

                        double.Parse(textBox6.Text),
                        comboBox2.Text,
                        time,
                        radioButton20.Checked,

                        double.Parse(textBox9.Text),
                        double.Parse(textBox10.Text),
                        double.Parse(textBox11.Text)
                    );
                //检验数据合法性
                if (Program.xmin == double.PositiveInfinity)
                {
                    MessageBox.Show("请先加载地图", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Close();
                }
                else if (Program.optionData.sourceX < Program.xmin || Program.optionData.sourceX > Program.xmax || Program.optionData.sourceY < Program.ymin || Program.optionData.sourceY > Program.ymax)
                {
                    string str = "污染源坐标x必须在" + Program.xmin + "至" + Program.xmax + "之间\n污染源坐标y必须在" + Program.ymin + "至" + Program.ymax + "之间";
                    MessageBox.Show(str, "提醒", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else if (Program.optionData.sourceIntensity < 10 || Program.optionData.sourceIntensity > 20000000)
                {
                    MessageBox.Show("污染源强度必须在10mg/s至20000000mg/s之间", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else if (Program.optionData.tunHeight < 5 || Program.optionData.tunHeight > 80)
                {
                    MessageBox.Show("烟囱高度必须在5米至80米之间", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else if (Program.optionData.heatEmission < 300 || Program.optionData.heatEmission > 5000)
                {
                    MessageBox.Show("烟囱热排放率必须在300千瓦至5000千瓦之间", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else if (Program.optionData.inradium < 0.5 || Program.optionData.inradium > 50)
                {
                    MessageBox.Show("烟囱内径必须在0.5米至50米之间", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else if (Program.optionData.smokeVelocity < 0.5 || Program.optionData.smokeVelocity > 500)
                {
                    MessageBox.Show("烟流出口速度必须在0.5m/s至500m/s之间", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else if (Program.optionData.continueTime < 5)
                {
                    MessageBox.Show("持续时间必须大于5s", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    _form.toolStripDropDownButton3.Enabled = true;
                    Close();
                }
            }
            catch (ArgumentNullException ane)
            {
                if (!checkBox1.Checked)
                {
                    Program.optionData = new OptionData(null, 112, 34, 500, true, true, Stability.STAB_A, 5.0, 0, 50, "秒",100, false, 2100, 5, 5);
                    MessageBox.Show("参数不可为空", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (FormatException fe)
            {
                Program.optionData = new OptionData(null, 112, 34, 500, true, true, Stability.STAB_A, 5.0, 0, 50, "秒", 100, false, 2100, 5, 5);
                MessageBox.Show("参数只能为数字", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        //取消键
        private void cancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }
        //查找污染源
        private void searchButton_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            foreach (var points in Program.shpPoints)
            {
                foreach (var point in points)
                {
                    if (checkBox1.Checked)
                    {
                        if (point.name.Trim().Contains(textBox1.Text))
                        {
                            listBox1.Items.Add(point.name.Trim());
                        }
                    }
                    else
                    {
                        if (textBox1.Text.Equals(point.name.Trim()))
                        {
                            listBox1.Items.Add(point.name.Trim());
                        }
                    }
                }
            }
            if (string.IsNullOrEmpty(textBox1.Text))
            {
                MessageBox.Show("搜索框不可为空", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else if(listBox1.Items.Count==0)
            {
                MessageBox.Show("搜索完毕,无相应地点", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        //选中
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedName = listBox1.SelectedItem.ToString();
            foreach (var points in Program.shpPoints)
            {
                foreach (var point in points)
                {
                    if (selectedName.Equals(point.name.Trim()))
                    {
                        textBox3.Text = point.X.ToString();
                        textBox4.Text = point.Y.ToString();
                    }
                }
            }
        }
        //重置
        private void resetButton_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            textBox1.Clear();
        }
    }
}
