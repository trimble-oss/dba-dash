using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Defaults;

namespace DBADashGUI.Performance
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
                }
            }
        }

        Int32 PointSize
        {
            get
            {
                if (dataPointsToolStripMenuItem.Checked)
                {
                    return 10;
                }
                else
                {
                    return 0;
                }
            }
        }

        public string ConnectionString;

        public Performance()
        {
            InitializeComponent();
        }


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
                }
            }
        }


        public void RefreshData()
        {
            if (ObjectID == 0)
            {
                ioPerformance1.RefreshData(InstanceID, DatabaseID);
                cpu1.InstanceID = InstanceID;
                cpu1.RefreshData();
                waits1.RefreshData(InstanceID);
                blocking1.RefreshData(InstanceID,DatabaseID);
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
    
            
        
            objectExecution1.RefreshData(InstanceID, ObjectID, DatabaseID);
            this.ResumeLayout();
            objectExecution1.ResumeLayout();
            ioPerformance1.ResumeLayout();
            cpu1.ResumeLayout();
            blocking1.ResumeLayout();
            waits1.ResumeLayout();
        }

  
        private void Timer1_Tick(object sender, EventArgs e)
        {

            ioPerformance1.RefreshData();
            cpu1.RefreshData();
            waits1.RefreshData();
            blocking1.RefreshData();
            objectExecution1.RefreshData();
        }

   
        private void SmoothLinesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ioPerformance1.SmoothLines = smoothLinesToolStripMenuItem.Checked;
            cpu1.SmoothLines = smoothLinesToolStripMenuItem.Checked;
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void Performance_Load(object sender, EventArgs e)
        {
            cpu1.PointSize = PointSize;
            ioPerformance1.PointSize = PointSize;
        }

        private void DataPointsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            cpu1.PointSize = PointSize;
            ioPerformance1.PointSize = PointSize;
            RefreshData();

        }
    }
}
