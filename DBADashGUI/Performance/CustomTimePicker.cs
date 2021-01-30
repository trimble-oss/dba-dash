using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            get
            {
                return time1.Value;
            }
            set
            {
                time1.Value = new DateTime(value.Year,value.Month,value.Day,value.Hour,value.Minute,0);
            }
        }

        public DateTime ToDate
        {
            get
            {
                return time2.Value;
            }
            set
            {
                time2.Value = new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0);
            }
        }

        private void bttnOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();

        }

        private void bttnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
