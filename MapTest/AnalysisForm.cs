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
    public partial class AnalysisForm : Form
    {
        private MainForm _form;

        public AnalysisForm(MainForm form)
        {
            InitializeComponent();
            _form = form;
            label2.Text = Program.optionData.timeUnit;
            label4.Text = Program.optionData.timeUnit;
        }
        //确认键
        private void button1_Click(object sender, EventArgs e)
        {
            Program.limitMinC = double.Parse(textBox3.Text);
            if (checkBox1.Checked)
            {
                if (Program.limitMinC < Program.optionData.sourceIntensity * 0.00001 / 500)
                {
                    DialogResult diaResult = MessageBox.Show("最小显示浓度值低于" + Program.optionData.sourceIntensity * 0.00001 / 500 + "mg,会使采样点过多导致渲染时间过长，是否继续?", "提醒", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (diaResult == DialogResult.No)
                        return;
                }
                _form.staticAnalysis();
            }
            else
            {
                try
                {
                    switch (Program.optionData.timeUnit)
                    {
                        case "秒":
                            Program.curTime = int.Parse(textBox1.Text);
                            Program.timeStep = int.Parse(textBox2.Text);
                            break;
                        case "分钟":
                            Program.curTime = 60 * int.Parse(textBox1.Text);
                            Program.timeStep = 60 * int.Parse(textBox2.Text);
                            break;
                        case "小时":
                            Program.curTime = 3600 * int.Parse(textBox1.Text);
                            Program.timeStep = 3600 * int.Parse(textBox2.Text);
                            break;
                    }
                    if (Program.curTime < 0 || Program.timeStep <= 0)
                    {
                        MessageBox.Show("已开始时间需为非负数，时间步长需为正数", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                catch (System.FormatException textFormatException)
                {
                    MessageBox.Show("请填写整数时间", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (Program.limitMinC < Program.optionData.sourceIntensity * 0.0000000009 / 500)
                {
                    DialogResult diaResult = MessageBox.Show("最小显示浓度值若设置过小,会使采样点过多导致渲染时间过长，是否继续?", "提醒", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (diaResult == DialogResult.No)
                        return;
                }
                _form.timePermission = true;
                _form.dynamicEnable();
                _form.toolStripDropDownButton4.Enabled = true;
            }
            Close();
        }
        //取消键
        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                textBox2.Enabled = false;
                label3.Enabled = false;
                label4.Enabled = false;
                textBox1.Enabled = false;
                label1.Enabled = false;
                label2.Enabled = false;
            }
            else
            {
                textBox2.Enabled = true;
                label3.Enabled = true;
                label4.Enabled = true;
                textBox1.Enabled = true;
                label1.Enabled = true;
                label2.Enabled = true;
            }
        }
    }
}
