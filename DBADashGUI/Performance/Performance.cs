using System;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    public partial class Performance : UserControl, ISetContext, IRefreshData
    {

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

        public Performance()
        {
            InitializeComponent();
        }


        private DBADashContext context;

        private Int64 ObjectID;

        private int InstanceID { get => context.InstanceID; }

        private Int32 DatabaseID { get => context.DatabaseID; }

        private bool objectMode;

        public void SetContext(DBADashContext context)
        {
            this.context = context;
            objectMode = context.Type is not (SQLTreeItem.TreeType.Instance or SQLTreeItem.TreeType.Database or SQLTreeItem.TreeType.AzureDatabase or SQLTreeItem.TreeType.AzureInstance);
            ObjectID = objectMode ? context.ObjectID : 0; // ObjectID should be 0 if we are at Instance/DB level.  Database object can have ObjectID is schema snapshots are used
            ioPerformance1.Visible = !objectMode;
            cpu1.Visible = !objectMode;
            waits1.Visible = !objectMode;
            blocking1.Visible = !objectMode;
            this.tableLayoutPanel1.RowStyles[0].Height = objectMode ? 0 : 20;
            this.tableLayoutPanel1.RowStyles[1].Height = objectMode ? 0 : 20;
            this.tableLayoutPanel1.RowStyles[2].Height = objectMode ? 0 : 20;
            this.tableLayoutPanel1.RowStyles[3].Height = objectMode ? 0 : 20;
            this.tableLayoutPanel1.RowStyles[4].Height = objectMode ? 100 : 20;

            RefreshData();
        }


        public void RefreshData()
        {
            this.SuspendLayout();
            objectExecution1.SuspendLayout();
            ioPerformance1.SuspendLayout();
            cpu1.SuspendLayout();
            blocking1.SuspendLayout();
            waits1.SuspendLayout();

            if (!objectMode)
            {
                ioPerformance1.RefreshData(InstanceID, DatabaseID);
                cpu1.InstanceID = InstanceID;
                cpu1.RefreshData();
                waits1.RefreshData(InstanceID);
                blocking1.RefreshData(InstanceID, DatabaseID);
            }

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
