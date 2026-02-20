using DBADashGUI.Charts;
using Humanizer;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using Microsoft.Data.SqlClient;
using System;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    public partial class Waits : UserControl, IMetricChart
    {
        public Waits()
        {
            InitializeComponent();
        }

        public class DateModel
        {
            public DateTime DateTime { get; set; }
            public double Value { get; set; }
        }

        private int instanceID;
        private DateTime lastWait = DateTime.MinValue;
        private int dateGrouping;
        private int mins;
        private DataTable dt;
        private LegendPosition legendPosition = LegendPosition.Hidden;

        public event EventHandler<EventArgs> Close;

        public event EventHandler<EventArgs> MoveUp;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool CloseVisible
        {
            get => tsClose.Visible;
            set => tsClose.Visible = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string WaitType
        {
            get => Metric.WaitType;
            set
            {
                Metric.WaitType = value;
                SetMetric();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool MoveUpVisible
        {
            get => tsUp.Visible; set => tsUp.Visible = value;
        }

        private WaitMetric _metric = new();

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public WaitMetric Metric
        {
            get => _metric; set { _metric = value; SetMetric(); }
        }

        private void SetMetric()
        {
            criticalWaitsOnlyToolStripMenuItem.Checked = Metric.CriticalWaitsOnly;
            tsFilter.Text = Metric.WaitType == "" ? (criticalWaitsOnlyToolStripMenuItem.Checked ? "*Critical Waits*" : "") : Metric.WaitType;
        }

        IMetric IMetricChart.Metric => Metric;

        public void RefreshData(int instanceID)
        {
            this.instanceID = instanceID;

            if (mins != DateRange.DurationMins)
            {
                dateGrouping = DateHelper.DateGrouping(DateRange.DurationMins, 65);
                if (dateGrouping < 1)
                {
                    dateGrouping = 1;
                }
                tsDateGrouping.Text = DateHelper.DateGroupString(dateGrouping);
                mins = DateRange.DurationMins;
            }

            RefreshData();
        }

        private DataTable GetWaitsDT()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.Waits_Get", cn);
            using var da = new SqlDataAdapter(cmd);
            cn.Open();

            cmd.Parameters.AddWithValue("InstanceID", instanceID);
            cmd.Parameters.AddWithValue("FromDate", DateRange.FromUTC);
            cmd.Parameters.AddWithValue("ToDate", DateRange.ToUTC);
            cmd.Parameters.AddWithValue("DateGroupingMin", dateGrouping);
            cmd.Parameters.AddWithValue("CriticalWaitsOnly", criticalWaitsOnlyToolStripMenuItem.Checked);
            cmd.Parameters.AddStringIfNotNullOrEmpty("WaitType", Metric.WaitType);
            cmd.Parameters.AddWithValue("@UTCOffset", DateHelper.UtcOffset);
            if (DateRange.HasTimeOfDayFilter)
            {
                cmd.Parameters.AddWithValue("Hours", DateRange.TimeOfDay.AsDataTable());
            }
            if (DateRange.HasDayOfWeekFilter)
            {
                cmd.Parameters.AddWithValue("DaysOfWeek", DateRange.DayOfWeek.AsDataTable());
            }
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = Config.DefaultCommandTimeout;
            DataTable dt = new();
            da.Fill(dt);
            DateHelper.ConvertUTCToAppTimeZone(ref dt);
            return dt;
        }

        private void CalcTopWaits(ref DataTable dt)
        {
            // Get top 3 wait types, calculating total wait for period
            var topWaits = dt.AsEnumerable()
                .GroupBy(r => r["WaitType"])
                .OrderByDescending(g => g.Sum(s => (decimal)s["TotalWaitSec"]))
                .Take(3)
                .Select(g => new
                {
                    WaitType = ((string)g.Key),
                    IsCritical = g.Max(s => (bool)s["IsCriticalWait"]),
                    WaitTime = g.Sum(s => (decimal)s["TotalWaitSec"]),
                    WaitTimeMsPerSecond = g.Sum(s => Convert.ToDouble(s["TotalWaitSec"]) * 1000) / g.Max(s => Convert.ToDouble(s["TotalSampleDurationSec"])),
                    // Sample duration could be calculated from DateRange.TimeSpan.TotalSeconds. TotalSampleDurationSec should be more accurate and match Waits tab
                    WaitTimeMsPerSecondPerCore = g.Sum(s => Convert.ToDouble(s["TotalWaitSec"]) * 1000) / g.Max(s => Convert.ToDouble(s["TotalSampleDurationSec"])) / g.Max(s => (int)s["SchedulerCount"])
                }
                ).ToList();
            StringBuilder sb = new();
            // CSV list of top waits
            tsTopWaits.Text = "Top Waits: " + string.Join(", ", topWaits.Select(x => x.WaitType));
            // Tooltip with wait times
            tsTopWaits.ToolTipText = string.Join(Environment.NewLine,
                topWaits.Select(x => x.WaitType + " - "
                + TimeSpan.FromSeconds(Convert.ToDouble(x.WaitTime)).Humanize(2, minUnit: TimeUnit.Second)
                + " (" + x.WaitTimeMsPerSecond.ToString("N1") + "ms/sec" + ")"
                ));
            double threshold = 100; // To avoid highlighting with very small amounts of wait
            if (topWaits.Any(r => r.IsCritical && r.WaitTimeMsPerSecond > threshold)) // One of the top wait types is critical (RESOURCE_SEMAPHORE etc).  Red
            {
                tsTopWaits.ForeColor = DashColors.Fail;
                tsTopWaits.Font = new System.Drawing.Font(tsTopWaits.Font, System.Drawing.FontStyle.Bold);
            }
            else if (topWaits.Any(r => r.WaitType.StartsWith("LCK_") && r.WaitTimeMsPerSecond > threshold)) // One of the top wait types is blocking.  Amber
            {
                tsTopWaits.ForeColor = DashColors.Warning;
                tsTopWaits.Font = new System.Drawing.Font(tsTopWaits.Font, System.Drawing.FontStyle.Bold);
            }
            else
            {
                tsTopWaits.ForeColor = DashColors.TrimbleBlue;
                tsTopWaits.Font = new System.Drawing.Font(tsTopWaits.Font, System.Drawing.FontStyle.Regular);
            }
        }

        public void RefreshData()
        {
            dt = GetWaitsDT();
            CalcTopWaits(ref dt);
            RefreshChart();
        }

        public void RefreshChart()
        {
            if (dt == null || dt.Rows.Count == 0)
            {
                waitChart.Series = Array.Empty<ISeries>();
                waitChart.XAxes = Array.Empty<Axis>();
                waitChart.YAxes = Array.Empty<Axis>();
                return;
            }

            // Get the selected metric column
            var metricColumn = (string)tsMetric.Tag;

            // Use ChartHelper with SeriesColumn to group by WaitType
            var config = new ChartConfiguration
            {
                DateColumn = "Time",
                MetricColumn = metricColumn,
                SeriesColumn = "WaitType",  // Group data by WaitType - each becomes a series
                ChartType = ChartTypes.StackedColumn,
                ShowLegend = true,
                LegendPosition = legendPosition,
                XAxisMin = DateRange.FromUTC.ToAppTimeZone(),
                XAxisMax = DateRange.ToUTC.ToAppTimeZone(),
                YAxisLabel = Units,
                YAxisFormat = "0.0",
                YAxisMin = 0,
                DateUnit = TimeSpan.FromMinutes(dateGrouping)
            };

            ChartHelper.UpdateChart(waitChart, dt, config);
        }

        private string Units
        {
            get
            {
                if (msseccoreToolStripMenuItem.Checked || signalWaitMsseccoreToolStripMenuItem.Checked)
                {
                    return "ms/sec/core";
                }
                else if (waitmssecToolStripMenuItem.Checked || signalWaitMssecToolStripMenuItem.Checked)
                {
                    return "ms/sec";
                }
                else if (totalWaitsecToolStripMenuItem.Checked || signalWaitsecToolStripMenuItem.Checked)
                {
                    return "sec";
                }
                else
                {
                    return "";
                }
            }
        }

        private void Waits_Load(object sender, EventArgs e)
        {
            DateHelper.AddDateGroups(tsDateGrouping, TsDateGrouping_Click);
        }

        private void TsDateGrouping_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            dateGrouping = Convert.ToInt32(ts.Tag);
            tsDateGrouping.Text = DateHelper.DateGroupString(dateGrouping);
            RefreshData();
        }

        private void TsFilterWaitType_Click(object sender, EventArgs e)
        {
            var wt = Metric.WaitType;
            if (CommonShared.ShowInputDialog(ref wt, "Wait Type (LIKE):") == DialogResult.OK)
            {
                WaitType = wt.EndsWith('%') || wt.Length == 0 ? wt : wt + "%";
                RefreshData();
            }
        }

        private void StringFilterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var wt = Metric.WaitType;
            if (CommonShared.ShowInputDialog(ref wt, "Wait Type (LIKE):") == DialogResult.OK)
            {
                criticalWaitsOnlyToolStripMenuItem.Checked = false;
                Metric.CriticalWaitsOnly = false;
                WaitType = wt.EndsWith('%') || wt.Length == 0 ? wt : wt + "%";
                RefreshData();
            }
        }

        private void CriticalWaitsOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Metric.CriticalWaitsOnly = criticalWaitsOnlyToolStripMenuItem.Checked;
            WaitType = "";
            RefreshData();
        }

        private void TsClose_Click(object sender, EventArgs e)
        {
            Close?.Invoke(this, EventArgs.Empty);
        }

        private void TsUp_Click(object sender, EventArgs e)
        {
            MoveUp?.Invoke(this, EventArgs.Empty);
        }

        private void SelectMetric(object sender, EventArgs e)
        {
            var item = (ToolStripMenuItem)sender;
            tsMetric.CheckSingleItem(item);
            tsMetric.Text = item.Text;
            tsMetric.Tag = item.Tag;
            RefreshChart();
        }

        private async void TsTopWaits_Click(object sender, EventArgs e)
        {
            WaitSummaryDialog waitSummaryForm = new();
            waitSummaryForm.SetContext(new DBADashContext() { InstanceID = instanceID });
            await waitSummaryForm.ShowSingleInstanceAsync();
        }

        private void SetLegendPosition(object sender, EventArgs e)
        {
            var item = (ToolStripMenuItem)sender;
            Enum.TryParse(item.Tag.ToString(), out legendPosition);
            foreach (ToolStripMenuItem menuItem in tsLegend.DropDownItems.OfType<ToolStripMenuItem>())
            {
                menuItem.Checked = menuItem == item;
            }
            waitChart.LegendPosition = legendPosition;
        }
    }
}