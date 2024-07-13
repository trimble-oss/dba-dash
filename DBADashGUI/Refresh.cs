using System;
using System.Drawing;
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

        private void Reset()
        {
            BackColor = DashColors.TrimbleBlue;
            ForeColor = Color.White;
            lblRefresh.Text = baseText;
            timer1.Enabled = Visible && Common.IsApplicationRunning;
        }

        public void ShowRefresh()
        {
            Visible = true;
            Reset();
        }

        public void HideRefresh()
        {
            timer1.Enabled = false;
            Visible = false;
        }

        private readonly string baseText = "Refresh in progress";

        private void Timer1_Tick(object sender, EventArgs e)
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

        public void SetMessage(string message, Color backColor, Color foreColor)
        {
            timer1.Enabled = false;
            BackColor = backColor;
            ForeColor = foreColor;
            lblRefresh.Text = message;
        }
    }
}
