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
        public enum DateGroup
        {
            None,
            _10MIN,
            _60MIN,
            DAY
        }

        DateGroup dateGrp =  DateGroup.None;
        Int32 mins=60;
        DateTime customFrom = DateTime.MinValue;
        DateTime customTo = DateTime.MinValue;

        public void RefreshData(Int32 mins = 60)
        {
            this.mins = mins;
            this.dateGrp = DateGrouping(mins);
            var from = DateTime.UtcNow.AddMinutes(-mins);
            var to = DateTime.UtcNow.AddMinutes(1);
            uncheckTime();
            foreach(ToolStripMenuItem ts in tsTime.DropDownItems)
            {
                if (Int32.Parse((string)ts.Tag) == mins)
                {
                    ts.Checked = true;
                    break;
                }
            }
            RefreshData(from, to);
            if (dateGrp == DateGroup.None && mins<=180)
            {
                enableTimer(true);
            }
            else
            {
                enableTimer(false);
                tsEnableTimer.Enabled = false;
            }
        }

        public DateGroup DateGrouping(Int32 Mins)
        {
            if (Mins < 721)
            {
                return  DateGroup.None;
            }
            if (Mins < 2881)
            {
                return DateGroup._10MIN;
            }
            if (Mins < 46001)
            {
                return DateGroup._60MIN;
            }
            return DateGroup.DAY;
        }

        public void RefreshData(DateTime from,DateTime to)
        {
            dateGrp = DateGrouping((Int32)to.Subtract(from).TotalMinutes);
            enableTimer(false);
            ioPerformance1.RefreshData(InstanceID, from, to, ConnectionString, dateGrp);
            cpu1.RefreshData(InstanceID,from, to, ConnectionString, dateGrp);
            waits1.RefreshData(InstanceID, from, to, ConnectionString, dateGrp);
        }




        private void tsTime_Click(object sender, EventArgs e)
        {
            var itm = (ToolStripMenuItem)sender;
            RefreshData(Int32.Parse((string)itm.Tag));
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

            ioPerformance1.RefreshData();
            cpu1.RefreshData();
            waits1.RefreshData();
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
            if (mins > 0)
            {
                frm.FromDate = DateTime.Now.AddMinutes(-mins);
                frm.ToDate = DateTime.Now;
            }
            else
            {
                frm.FromDate = customFrom;
                frm.ToDate = customTo;
            }
            frm.ShowDialog();
            if(frm.DialogResult == DialogResult.OK)
            {
                customFrom = frm.FromDate;
                customTo = frm.ToDate;
                mins = 0;
                RefreshData(frm.FromDate.ToUniversalTime(), frm.ToDate.ToUniversalTime());
                uncheckTime();
                tsCustom.Checked = true;
                tsEnableTimer.Enabled = false;
            }
       
        }

        private void tsTime_Click_1(object sender, EventArgs e)
        {

        }
    }
}
