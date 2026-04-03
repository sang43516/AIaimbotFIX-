using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AIMBOT_AI_SAFE
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void guna2GradientButton1_Click(object sender, EventArgs e)
        {
            AimbotVisibleScan.Start();
            lblStatus.Text = "Aimbot Applying!!";
        }

        private void guna2GradientButton2_Click(object sender, EventArgs e)
        {
            AimbotVisibleScan.Stop();
            lblStatus.Text = "Aimbot OFF";
        }
    }
}
