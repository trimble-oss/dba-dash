using DBADashGUI.Theme;
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    public partial class CustomTimePicker : Form
    {
        public CustomTimePicker()
        {
            InitializeComponent();
            this.ApplyTheme();
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DateTime FromDate
        {
            get => time1.Value; set => time1.Value = new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0);
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DateTime ToDate
        {
            get => time2.Value; set => time2.Value = new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0);
        }

        private void BttnOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void BttnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}