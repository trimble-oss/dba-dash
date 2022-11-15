using System;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    public partial class RunningQueriesViewer : Form
    {
        public RunningQueriesViewer()
        {
            InitializeComponent();
        }

        public bool ShowRootBlockers { get; set; }

        public DateTime SnapshotDateFrom
        {
            get
            {
                return runningQueries1.SnapshotDateFrom;
            }
            set
            {
                runningQueries1.SnapshotDateFrom = value;
            }
        }

        public DateTime SnapshotDateTo
        {
            get
            {
                return runningQueries1.SnapshotDateTo;
            }
            set
            {
                runningQueries1.SnapshotDateTo = value;
            }
        }

        public int InstanceID
        {
            get
            {
                return runningQueries1.InstanceID;
            }
            set
            {
                runningQueries1.InstanceID = value;
            }
        }

        private void RunningQueriesViewer_Load(object sender, EventArgs e)
        {
            runningQueries1.RefreshData();
            if (ShowRootBlockers)
            {
                runningQueries1.ShowRootBlockers();
            }
        }
    }
}
