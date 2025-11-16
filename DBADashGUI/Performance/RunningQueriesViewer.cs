using Amazon.S3.Model.Internal.MarshallTransformations;
using DBADashGUI.Theme;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    public partial class RunningQueriesViewer : Form
    {
        public RunningQueriesViewer()
        {
            InitializeComponent();
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool ShowRootBlockers { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DateTime SnapshotDateFrom
        {
            get => runningQueries1.SnapshotDateFrom; set => runningQueries1.SnapshotDateFrom = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DateTime SnapshotDateTo
        {
            get => runningQueries1.SnapshotDateTo; set => runningQueries1.SnapshotDateTo = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int InstanceID
        {
            get => runningQueries1.InstanceID; set => runningQueries1.InstanceID = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Guid JobId
        {
            get => runningQueries1.JobId; set => runningQueries1.JobId = value;
        }

        public ThemedTabControl Tab;

        public void LoadSnapshots(List<RunningQueriesSnapshotInfo> snapshots)
        {
            if (snapshots.Count == 0) return;
            Tab = new ThemedTabControl() { Dock = DockStyle.Fill };
            Tab.SelectedIndexChanged += Tab_SelectedIndexChanged;
            var i = 0;
            foreach (var snapshot in snapshots)
            {
                var page = new TabPage(snapshot.Title);
                Tab.TabPages.Add(page);
                if (i == 0)
                {
                    page.Controls.Add(runningQueries1);
                    runningQueries1.SnapshotDateFrom = snapshot.SnapshotDateUtc;
                    runningQueries1.SnapshotDateTo = snapshot.SnapshotDateUtc;
                    runningQueries1.InstanceID = snapshot.InstanceID;
                }
                else
                {
                    var viewerControl = new RunningQueries()
                    {
                        SnapshotDateFrom = snapshot.SnapshotDateUtc,
                        SnapshotDateTo = snapshot.SnapshotDateUtc,
                        InstanceID = snapshot.InstanceID,
                        Dock = DockStyle.Fill,
                    };
                    page.Controls.Add(viewerControl);
                }
                i++;
            }
            Tab.ApplyTheme();
            this.Controls.Add(Tab);
        }

        public HashSet<int> ResizedTabs = new();
        public HashSet<int> LoadedTabs = new();

        private void Tab_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Tab.SelectedTab is null) return;
            if (ResizedTabs.Contains(Tab.SelectedIndex)) return;
            var runningQueries = Tab.SelectedTab.Controls.OfType<RunningQueries>().First();
            if (!LoadedTabs.Contains(Tab.SelectedIndex))
            {
                runningQueries.RefreshData();
                LoadedTabs.Add(Tab.SelectedIndex);
            }
            ResizedTabs.Add(Tab.SelectedIndex);
            runningQueries.AutoResizeColumnsWithMaxColumnWidth();
        }

        private void RunningQueriesViewer_Load(object sender, EventArgs e)
        {
            RefreshAll();
        }

        private void RefreshAll()
        {
            if (Tab != null)
            {
                const int maxTabs = 5; // Load max of 5 snapshots upfront, then load on demand
                for (var i = 0; i < maxTabs && i < Tab.TabPages.Count; i++)
                {
                    var viewerControl = Tab.TabPages[i].Controls.OfType<RunningQueries>().First();
                    viewerControl.RefreshData();
                    if (ShowRootBlockers)
                    {
                        viewerControl.ShowRootBlockers();
                    }

                    LoadedTabs.Add(i);
                }
            }
            else
            {
                runningQueries1.RefreshData();
                if (ShowRootBlockers)
                {
                    runningQueries1.ShowRootBlockers();
                }
            }
        }
    }
}