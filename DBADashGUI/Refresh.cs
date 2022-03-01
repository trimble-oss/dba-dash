using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI
{
    public partial class Refresh : UserControl
    {
        public Refresh()
        {
            InitializeComponent();
            this.BackColor = DashColors.TrimbleBlue;
            this.ForeColor = Color.White;
        }

        private string baseText = "Refresh in progress";

        private void timer1_Tick(object sender, EventArgs e)
        {
            
            if (lblRefresh.Text.Length > baseText.Length + 20)
            {
                lblRefresh.Text = baseText;
            }
            else
            {
                lblRefresh.Text += ".";
            }
        }

        private void Refresh_VisibilityChanged(object sender, EventArgs e)
        {
             lblRefresh.Text = baseText;
             timer1.Enabled = this.Visible;            
        }
    }
}
