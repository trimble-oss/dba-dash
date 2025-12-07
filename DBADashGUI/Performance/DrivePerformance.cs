using DBADashGUI.Pickers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using DBADashGUI.Theme;

namespace DBADashGUI.Performance
{
    public partial class DrivePerformance : UserControl, ISetContext, IRefreshData
    {
        public DrivePerformance()
        {
            InitializeComponent();
            DateHelper.AddDateGroups(tsDateGroup, TsDateGroup_Click);
        }

        private int mins;
        private int dateGrouping;
        private double LatencyLimit = 200;
        private int PointSize => dataPointsToolStripMenuItem.Checked ? 10 : 0;

        private void TsDateGroup_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            dateGrouping = Convert.ToInt32(ts.Tag);
            tsDateGroup.Text = DateHelper.DateGroupString(dateGrouping);

            foreach (var io in layout1.Controls.OfType<IOPerformance>())
            {
                io.DateGrouping = dateGrouping;
                io.RefreshData();
            }
        }

        private DBADashContext Context;

        private List<ISelectable> drives;

        public void SetContext(DBADashContext _context)
        {
            Context = _context;
            var dt = CommonData.GetMetricDrives(Context.InstanceID, _context.DriveName);
            drives = dt.AsEnumerable()
                .Select(row =>
                    new SelectableString(((string)row["Name"]) + (string.IsNullOrEmpty(Convert.ToString(row["Label"])) ? string.Empty : " | " + Convert.ToString(row["Label"]))) as ISelectable)
                .OrderBy(drive => drive.Name)
                .ToList();
            drives.Skip(Config.DrivePerformanceMaxDrives).ToList().ForEach(s => s.IsVisible = false);
            layout1.Controls.Clear();
            layout1.RowCount = 0;
            RefreshData();
        }

        private int DriveCount => drives.Count;

        public void RefreshData()
        {
            if (mins != DateRange.DurationMins)
            {
                dateGrouping = DateHelper.DateGrouping(DateRange.DurationMins, 200);
                tsDateGroup.Text = DateHelper.DateGroupString(dateGrouping);
                mins = DateRange.DurationMins;
            }

            if (layout1.Controls.Count == 0)
            {
                LoadData();
            }
            else
            {
                RefreshCharts();
            }
        }

        public void LoadData()
        {
            layout1.Controls.Clear();
            layout1.RowCount = 0;
            layout1.RowStyles.Clear();
            foreach (ISelectable drive in drives.Where(d => d.IsVisible))
            {
                var io = new IOPerformance()
                {
                    Drive = drive.Name.Split(" | ")[0],
                    DateGrouping = dateGrouping,
                    DriveVisible = false,
                    DateGroupingVisible = false,
                    SummaryVisible = false,
                    FileGroupVisible = false,
                    MeasuresVisible = false,
                    Dock = DockStyle.Fill,
                    CloseVisible = true,
                    MoveUpVisible = false,
                    OptionsVisible = false,
                    SmoothLines = smoothLinesToolStripMenuItem.Checked,
                    PointSize = PointSize
                };
                io.SetLatencyLimit(LatencyLimit);
                io.RefreshData(Context.InstanceID);
                io.Close += Io_Close;
                io.ApplyTheme();
                layout1.RowCount++;
                layout1.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                layout1.Controls.Add(io);
            }

            if (synchronizeAxisToolStripMenuItem.Checked)
            {
                SynchronizeAxis();
            }
            ShowNotice();
        }

        private void RefreshCharts()
        {
            foreach (IOPerformance io in layout1.Controls.OfType<IOPerformance>())
            {
                io.ApplyTheme();
                io.RefreshData();
            }
            if (synchronizeAxisToolStripMenuItem.Checked)
            {
                SynchronizeAxis();
            }
        }

        private void SynchronizeAxis()
        {
            if (!layout1.Controls.OfType<IOPerformance>().Any()) return;
            var iopsMax = layout1.Controls.OfType<IOPerformance>().Max(io => io.IOPsAxisMaxValue);
            var mbMax = layout1.Controls.OfType<IOPerformance>().Max(io => io.MBAxisMaxValue);

            foreach (var io in layout1.Controls.OfType<IOPerformance>())
            {
                io.IOPsAxis.MaxValue = synchronizeAxisToolStripMenuItem.Checked ? (1 + iopsMax * 1.05).RoundUpToSignificantFigures(2) : double.NaN;
                io.MBsecAxis.MaxValue = synchronizeAxisToolStripMenuItem.Checked ? (1 + mbMax * 1.05).RoundUpToSignificantFigures(2) : double.NaN;
            }
        }

        private void ShowNotice()
        {
            lblNotice.Text = $"Showing {layout1.RowCount} of {DriveCount} drives";
            lblNotice.ForeColor = layout1.RowCount == DriveCount ? DashColors.Success : DashColors.Warning;
            lblNotice.Visible = string.IsNullOrEmpty(Context.DriveName);
        }

        private void Io_Close(object sender, EventArgs e)
        {
            var io = (IOPerformance)sender;
            drives.First(d => d.Name.Split(" | ")[0] == io.Drive).IsVisible = false;
            layout1.Controls.Remove(io);
            layout1.RowCount--;
            ShowNotice();
            SynchronizeAxis();
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshCharts();
        }

        private void SynchronizeAxisToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SynchronizeAxis();
        }

        private readonly Dictionary<string, ColumnMetaData> measures = IOPerformance.DefaultColumns;

        private void TsCols_Click(object sender, EventArgs e)
        {
            var selectableMetrics = measures.Values
                .Select(metric => (ISelectable)metric)
                .ToList();

            var colSelection = new SelectColumns() { Items = selectableMetrics, Text = "Select Measures" };
            colSelection.ApplyTheme();
            colSelection.ShowDialog();
            if (colSelection.DialogResult == DialogResult.OK)
            {
                measures.ApplyVisibility(colSelection.Items);
                foreach (IOPerformance io in layout1.Controls.OfType<IOPerformance>())
                {
                    io.Columns = measures;
                    io.RefreshData();
                }
                SynchronizeAxis();
            }
        }

        private void TsDrives_Click(object sender, EventArgs e)
        {
            using var frm = new SelectColumns() { Items = drives, Text = "Select Drives" };
            frm.ApplyTheme();
            frm.ShowDialog(this);
            if (frm.DialogResult == DialogResult.OK)
            {
                drives = frm.Items;
                LoadData();
            }
        }

        private void TsSummary_Click(object sender, EventArgs e)
        {
            IOSummaryForm summaryForm = new IOSummaryForm()
            { InstanceID = Context.InstanceID, FromDate = DateRange.FromUTC, ToDate = DateRange.ToUTC, GroupBy = IOSummary.IOSummaryGroupByOptions.Drive };
            summaryForm.ApplyTheme();
            summaryForm.ShowSingleInstance();
        }

        private void SmoothLinesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var io in layout1.Controls.OfType<IOPerformance>())
            {
                io.SmoothLines = smoothLinesToolStripMenuItem.Checked;
            }
        }

        private void SetLatencyLimit(object sender, EventArgs e)
        {
            foreach (ToolStripMenuItem itm in latencyLimitToolStripMenuItem.DropDownItems)
            {
                itm.Checked = itm == sender;
            }

            LatencyLimit = Convert.ToDouble(((ToolStripMenuItem)sender).Text);
            foreach (var io in layout1.Controls.OfType<IOPerformance>())
            {
                io.SetLatencyLimit(LatencyLimit);
            }
        }

        private void DataPointsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var io in layout1.Controls.OfType<IOPerformance>())
            {
                io.PointSize = PointSize;
                io.RefreshData();
            }
        }
    }
}