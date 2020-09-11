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
            _1MIN,
            _10MIN,
            _60MIN,
            _120MIN,
            DAY
        }

        DateGroup dateGrp =  DateGroup._1MIN;
        Int32 mins=60;
        DateTime customFrom = DateTime.MinValue;
        DateTime customTo = DateTime.MinValue;
        public Int64 ObjectID { get; set; }
        public Int32 DatabaseID { get; set; } = 0;



        public DateGroup DateGrouping(Int32 Mins)
        {
            if (Mins < 181)
            {
                return  DateGroup._1MIN;
            }
            if (Mins < 2881)
            {
                return DateGroup._10MIN;
            }
            if (Mins < 11520)
            {
                return DateGroup._60MIN;
            }
            if (Mins < 28800)
            {
                return DateGroup._120MIN;
            }
            return DateGroup.DAY;
        }

        public void RefreshData(Int32 mins)
        {
            this.mins = mins;
            this.dateGrp = DateGrouping(mins);
            toggleTimer();
            RefreshData();
        }

        private void setDateGroup(DateGroup grp)
        {
            string itmName="Date Grouping";
            foreach(ToolStripMenuItem itm in tsGrouping.DropDownItems)
            {
                itm.Checked = (string)itm.Tag == grp.ToString();
                if (itm.Checked){ itmName = itm.Text; }
            }
            dateGrp = grp;
            tsGrouping.Text = itmName;
        }

        public void RefreshData(DateTime from, DateTime to)
        {
            dateGrp = DateGrouping((Int32)to.Subtract(from).TotalMinutes);          
            RefreshData(from, to, dateGrp);
        }

        public void RefreshData(DateTime from,DateTime to, DateGroup dateGrp)
        {
            setDateGroup(dateGrp);
            toggleTimer();
            if (ObjectID == 0)
            {
                ioPerformance1.RefreshData(InstanceID, from, to, ConnectionString, DatabaseID, dateGrp);
                cpu1.RefreshData(InstanceID, from, to, ConnectionString, dateGrp);
                waits1.RefreshData(InstanceID, from, to, ConnectionString, dateGrp);
                blocking1.RefreshData(InstanceID, from, to, ConnectionString,DatabaseID, dateGrp);
            }
            ioPerformance1.Visible = ObjectID == 0;
            cpu1.Visible = ObjectID == 0;
            waits1.Visible = ObjectID == 0;
            blocking1.Visible = ObjectID == 0;
        
            objectExecution1.RefreshData(InstanceID, from, to, ConnectionString, ObjectID, DatabaseID, dateGrp);
          
        }

        private void toggleTimer()
        {
            enableTimer(false);
            if (dateGrp == DateGroup._1MIN && mins > 0 && mins <= 180)
            {
                tsEnableTimer.Enabled = true;
            }
            else
            {
                tsEnableTimer.Enabled = false;
            }
        }

        public void RefreshData()
        {
            var from = DateTime.UtcNow.AddMinutes(-mins);
            var to = DateTime.UtcNow.AddMinutes(1);
            if (mins >= 1440)
            {
                from = DateTime.UtcNow.Date.AddMinutes(-mins);
            }
            if (mins == 0)
            {
                from = customFrom;
                to = customTo;
            }
            try
            {
              RefreshData(from, to,dateGrp);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Chart Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                toggleTimer();
                return;
            }

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
            foreach (var ts in tsTime.DropDownItems)
            {
                if (ts.GetType() == typeof(ToolStripMenuItem))
                {
                    ((ToolStripMenuItem)ts).Checked = false;
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {

            ioPerformance1.RefreshData();
            cpu1.RefreshData();
            waits1.RefreshData();
            blocking1.RefreshData();
            objectExecution1.RefreshData();
        }

        private void tsDisableTimer_Click(object sender, EventArgs e)
        {
            enableTimer(false);
        }

        private void tsEnableTimer_Click(object sender, EventArgs e)
        {
            enableTimer(true);
        }

        public void StopTimer()
        {
            enableTimer(false);
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
                if (mins >= 1440)
                {
                    frm.FromDate = DateTime.UtcNow.Date.AddMinutes(-mins);
                }
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
                this.dateGrp = DateGrouping((Int32)customTo.Subtract(customFrom).TotalMinutes);
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

        private void smoothLinesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ioPerformance1.SmoothLines = smoothLinesToolStripMenuItem.Checked;
            cpu1.SmoothLines = smoothLinesToolStripMenuItem.Checked;
        }

        private void tsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void tsDateGroup_Click(object sender, EventArgs e)
        {
            string tag = (string)((ToolStripMenuItem)sender).Tag;
            DateGroup grp =  (DateGroup)Enum.Parse(typeof(DateGroup),tag);
            var from = DateTime.UtcNow.AddMinutes(-mins);
            if (mins >= 1440)
            {
                from = DateTime.UtcNow.Date.AddMinutes(-mins);
            }
            var to = DateTime.UtcNow.AddMinutes(1);
            if (mins == 0)
            {
                from = customFrom;
                to = customTo;
            }
            RefreshData(from, to, grp);
        }
    }
}
