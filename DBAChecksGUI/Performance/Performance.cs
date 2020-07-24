using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Defaults;

namespace DBAChecksGUI.Performance
{
    public partial class Performance : UserControl
    {

        public Int32 InstanceID;
        public string ConnectionString;

        public Performance()
        {
            InitializeComponent();
        }

        public void Refresh(Int32 mins = 60)
        {
           //this.mins = mins;
            var from = DateTime.UtcNow.AddMinutes(-mins);
            var to = DateTime.UtcNow.AddMinutes(1);           
            Refresh(from, to);
            enableTimer(true);
        }

        public string DateGrouping(Int32 Mins)
        {
            if (Mins < 200)
            {
                return "None";
            }
            if (Mins < 2000)
            {
                return "10MIN";
            }
            if (Mins < 12000)
            {
                return "60MIN";
            }
            return "DAY";
        }

        public void Refresh(DateTime from,DateTime to)
        {
            var dateGrp = DateGrouping((Int32)to.Subtract(from).TotalMinutes);
            enableTimer(false);
            ioPerformance1.ConnectionString = ConnectionString;
            ioPerformance1.InstanceID = InstanceID;
            ioPerformance1.FromDate = from;
            ioPerformance1.ToDate = to;
            ioPerformance1.RefreshData();
            cpu1.RefreshData(from, to, ConnectionString, InstanceID,dateGrp);
        }




        private void tsTime_Click(object sender, EventArgs e)
        {
            var itm = (ToolStripMenuItem)sender;
            Refresh(Int32.Parse((string)itm.Tag));
            uncheckTime();
            itm.Checked = true;
        }

        private void uncheckTime()
        {
            var items = new List<ToolStripMenuItem>() { ts30Min, ts1Hr, ts2Hr, ts3Hr, ts6Hr, ts12Hr,tsCustom };
            foreach (var ts in items)
            {
                ts.Checked = false;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            
            //RefreshCPU(eventTime.AddSeconds(1), DateTime.UtcNow, true);
            ioPerformance1.RefreshData(true);
            cpu1.RefreshData();
            //RefreshIO(ioTime.AddSeconds(1), DateTime.UtcNow, true);
        }

        private void tsDisableTimer_Click(object sender, EventArgs e)
        {
            enableTimer(false);
        }

        private void tsEnableTimer_Click(object sender, EventArgs e)
        {
            enableTimer(true);
        }

        private void enableTimer(bool isEnabled)
        {
            timer1.Enabled = isEnabled;
            tsEnableTimer.Enabled = !isEnabled;
            tsDisableTimer.Enabled = isEnabled;
        }

        private void customToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var frm = new CustomTimePicker();
            frm.ShowDialog();
            if(frm.DialogResult == DialogResult.OK)
            {
                Refresh(frm.FromDate.ToUniversalTime(), frm.ToDate.ToUniversalTime());
                uncheckTime();
                tsCustom.Checked = true;
                tsEnableTimer.Enabled = false;
            }
       
        }
    }
}
