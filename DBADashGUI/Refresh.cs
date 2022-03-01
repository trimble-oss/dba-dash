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
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.DoubleBuffer, true);
        }

        private void reset()
        {
            this.BackColor = DashColors.TrimbleBlue;
            this.ForeColor = Color.White;
            lblRefresh.Text = baseText;
            timer1.Enabled = this.Visible;
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

        public void SetFailed(string message)
        {
            SetMessage(message, DashColors.Fail, Color.White);
        }

        public void SetMessage(string message,Color backColor,Color foreColor)
        {
            this.BackColor = backColor;
            this.ForeColor= foreColor;  
            lblRefresh.Text=message;
            timer1.Enabled = false;
        }

        private void Refresh_VisibilityChanged(object sender, EventArgs e)
        {
            reset();                       
        }
    }
}
