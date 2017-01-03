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
    public partial class ViewForm : Form
    {
        private MainForm _form;
        public ViewForm(MainForm form,bool isArea)
        {
            InitializeComponent();
            _form = form;
            if (isArea)
            {
                groupBox1.Visible = true;
                groupBox2.Visible = false;
            }
            else
            {
                groupBox2.Visible = true;
                groupBox1.Visible = false;
            }
        }
        //确认键
        private void button1_Click(object sender, EventArgs e)
        {
            if (groupBox1.Visible)
            {
                double x = double.Parse(textBox1.Text);
                double y = double.Parse(textBox2.Text);
                _form.CustomedZoom(x, y);
            }
            if (groupBox2.Visible)
            {
                double x = Program.optionData.sourceX;
                double y = Program.optionData.sourceY;
                double scale = double.Parse(textBox4.Text)*0.005/6329;
                _form.CustomedZoom(x, y,scale);
            }
            Close();
        }
        //取消键
        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }

    }
}
