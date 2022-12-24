using System;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    public partial class CustomTimePicker : Form
    {
        public CustomTimePicker()
        {
            InitializeComponent();
        }

        public DateTime FromDate
        {
            get => time1.Value; set => time1.Value = new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0);
        }

        public DateTime ToDate
        {
            get => time2.Value; set => time2.Value = new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0);
        }

        private void BttnOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BttnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}