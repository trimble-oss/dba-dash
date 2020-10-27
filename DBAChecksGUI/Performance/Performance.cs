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

        private Int32 instanceID;

        public Int32 InstanceID {
            get
            {
                return instanceID;
            }
            set
            {
                if (instanceID != value)
                {
                    instanceID = value;
                    Reset();
                }
            }
        }


        public string ConnectionString;

        public Performance()
        {
            InitializeComponent();
        }


        public void Reset()
        {
            mins = 60;
            customFrom = DateTime.MinValue;
            customTo = DateTime.MinValue;
            checkTime();

        }


        Int32 mins=60;
        DateTime customFrom = DateTime.MinValue;
        DateTime customTo = DateTime.MinValue;
        Int32 databaseID = 0;
        Int64 objectID = 0;
        public Int64 ObjectID { 
            get {
                return objectID;
            }
            set {
                if (objectID != value)
                {
                    objectID = value;
                    Reset();
                }
            }
        }
        
        public Int32 DatabaseID { 
            get {
                return databaseID;
            }
            set {
                if (databaseID != value)
                {
                    databaseID = value;
                    Reset();
                }
            }
        }





        public void RefreshData(Int32 mins)
        {
            this.mins = mins;
            RefreshData();
        }

        //private void setDateGroup(Int32 grp)
        //{
        //    string itmName="Date Grouping";
        //    foreach(ToolStripMenuItem itm in tsGrouping.DropDownItems)
        //    {
        //        itm.Checked = Convert.ToInt32(itm.Tag) == grp;
        //        if (itm.Checked){ itmName = itm.Text; }
        //    }
        //    dateGrp = grp;
        //    tsGrouping.Text = itmName;
        //}

        //public void RefreshData(DateTime from, DateTime to)
        //{
        //    dateGrp = DateGrouping((Int32)to.Subtract(from).TotalMinutes);          
        //    RefreshData(from, to, dateGrp);
        //}

        public void RefreshData(DateTime from,DateTime to)
        {
            if (ObjectID == 0)
            {
                ioPerformance1.RefreshData(InstanceID, from, to, ConnectionString, DatabaseID);
                cpu1.RefreshData(InstanceID, from, to, ConnectionString);
                waits1.RefreshData(InstanceID, from, to, ConnectionString);
                blocking1.RefreshData(InstanceID, from, to, ConnectionString,DatabaseID);
            }
            this.SuspendLayout();
            objectExecution1.SuspendLayout();
            ioPerformance1.SuspendLayout();
            cpu1.SuspendLayout();
            blocking1.SuspendLayout();
            waits1.SuspendLayout();

            ioPerformance1.Visible = ObjectID == 0;
            cpu1.Visible = ObjectID == 0;
            waits1.Visible = ObjectID == 0;
            blocking1.Visible = ObjectID == 0;
          
            this.tableLayoutPanel1.RowStyles[0].Height = ObjectID == 0 ? 20 : 0;
            this.tableLayoutPanel1.RowStyles[1].Height = ObjectID == 0 ? 20 : 0;
            this.tableLayoutPanel1.RowStyles[2].Height = ObjectID == 0 ? 20 : 0;
            this.tableLayoutPanel1.RowStyles[3].Height = ObjectID == 0 ? 20 : 0;
            this.tableLayoutPanel1.RowStyles[4].Height = ObjectID == 0 ? 20 : 100;
    
            
        
            objectExecution1.RefreshData(InstanceID, from, to, ConnectionString, ObjectID, DatabaseID);
            this.ResumeLayout();
            objectExecution1.ResumeLayout();
            ioPerformance1.ResumeLayout();
            cpu1.ResumeLayout();
            blocking1.ResumeLayout();
            waits1.ResumeLayout();
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
              RefreshData(from, to);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Chart Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

        }


        private void tsTime_Click(object sender, EventArgs e)
        {
            var itm = (ToolStripMenuItem)sender;
            RefreshData(Int32.Parse((string)itm.Tag));
            checkTime();
      
        }

        private void checkTime()
        {
            Int32 tag = mins;
            if(customFrom>DateTime.MinValue | customTo> DateTime.MinValue)
            {
                tag = -1;
            }
            foreach (var ts in tsTime.DropDownItems)
            {
                if (ts.GetType() == typeof(ToolStripMenuItem))
                {
                    var mnu = (ToolStripMenuItem)ts;
                    mnu.Checked = Int32.Parse((string)mnu.Tag)==tag;
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
                mins = 0;
                RefreshData(frm.FromDate.ToUniversalTime(), frm.ToDate.ToUniversalTime());
                checkTime();
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

        private void Performance_Load(object sender, EventArgs e)
        {
 
        }
    }
}
