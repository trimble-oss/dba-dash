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
                time1.Value = value;
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
                time2.Value = value;
            }
        }

        private void bttnOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();

        }
    }
}
