using DBADashGUI.Charts;
using DBADashGUI.Pickers;
using DBADashGUI.Theme;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Font = System.Drawing.Font;

namespace DBADashGUI.Performance
{
    public partial class IOPerformance : UserControl, IMetricChart
    {
        public IOPerformance()
        {
            InitializeComponent();
        }

        private int mins;
        private int instanceID;
        private int dateGrouping;
        private bool smoothLines = true;
        private int databaseID;
        public int PointSize;
        private string filegroup = "";
        private double LatencyLimit = 200;

        public Axis TimeAxis;
        public Axis MBsecAxis;
        public Axis IOPsAxis;
        public Axis LatencyAxis;

        public double IOPsAxisMaxValue { get; private set; }
        public double MBAxisMaxValue { get; private set; }

        public event EventHandler<EventArgs> Close;

        public event EventHandler<EventArgs> MoveUp;

        private Dictionary<string, string> drives = new();
        private LegendPosition legendPosition = LegendPosition.Hidden;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool CloseVisible
        {
            get => tsClose.Visible;
            set => tsClose.Visible = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int DateGrouping
        {
            get => dateGrouping;
            set
            {
                dateGrouping = value;
                tsDateGroup.Text = DateHelper.DateGroupString(dateGrouping);
                mins = DateRange.DurationMins;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string FileGroup
        {
            get => filegroup;
            set
            {
                filegroup = value;
                tsFileGroup.Text = filegroup.Length > 0 ? filegroup : "Filegroup";
                var style = filegroup.Length > 0 ? FontStyle.Bold : FontStyle.Regular;
                tsFileGroup.Font = new Font(tsFileGroup.Font, style);
                foreach (ToolStripMenuItem itm in tsFileGroup.DropDownItems)
                {
                    style = itm.Text == filegroup ? FontStyle.Bold : FontStyle.Regular;
                    itm.Font = new Font(itm.Font, style);
                    itm.Checked = itm.Text == filegroup;
                }
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Drive
        {
            get => Metric.Drive;
            set
            {
                tsDrives.Text = value;
                Metric.Drive = value;
                if (filegroup != "")
                {
                    FileGroup = "";
                }
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool SmoothLines
        {
            get => smoothLines;
            set
            {
                smoothLines = value;
                // Refresh data to apply smoothness changes
                if (chartIO.Series != null && chartIO.Series.Any())
                {
                    RefreshData();
                }
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool MoveUpVisible
        {
            get => tsUp.Visible; set => tsUp.Visible = value;
        }

        private IOMetric _metric = new();

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public IOMetric Metric
        { get => _metric; set { _metric = value; SetMetric(); } }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool DateGroupingVisible
        {
            get => tsDateGroup.Visible; set => tsDateGroup.Visible = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool DriveVisible
        {
            get => tsDrives.Visible; set => tsDrives.Visible = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool MeasuresVisible
        {
            get => tsMeasures.Visible; set => tsMeasures.Visible = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool SummaryVisible
        {
            get => tsIOSummary.Visible; set => tsIOSummary.Visible = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool FileGroupVisible
        {
            get => tsFileGroup.Visible; set => tsFileGroup.Visible = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool OptionsVisible
        {
            get => tsOptions.Visible; set => tsOptions.Visible = value;
        }

        IMetric IMetricChart.Metric => Metric;

        public static readonly Dictionary<string, ColumnMetaData> DefaultColumns = new()
        {
            { "MBsec", new ColumnMetaData { Name = "MB/sec", IsVisible = true, AxisName = "MBsec" } },
            { "ReadMBsec", new ColumnMetaData { Name = "Read MB/sec", IsVisible = false, AxisName = "MBsec" } },
            { "WriteMBsec", new ColumnMetaData { Name = "Write MB/sec", IsVisible = false, AxisName = "MBsec" } },
            { "IOPs", new ColumnMetaData { Name = "IOPs", IsVisible = true, AxisName = "IOPs" } },
            { "ReadIOPs", new ColumnMetaData { Name = "Read IOPs", IsVisible = false, AxisName = "IOPs" } },
            { "WriteIOPs", new ColumnMetaData { Name = "Write IOPs", IsVisible = false, AxisName = "IOPs" } },
            { "Latency", new ColumnMetaData { Name = "Latency", IsVisible = true, AxisName = "Latency" } },
            { "ReadLatency", new ColumnMetaData { Name = "Read Latency", IsVisible = false, AxisName = "Latency" } },
            { "WriteLatency", new ColumnMetaData { Name = "Write Latency", IsVisible = false, AxisName = "Latency" } },
            { "MaxMBsec", new ColumnMetaData { Name = "Max MB/sec", IsVisible = false, AxisName = "MBsec" } },
            { "MaxReadMBsec", new ColumnMetaData { Name = "Max Read MB/sec", IsVisible = false, AxisName = "MBsec" } },
            { "MaxWriteMBsec", new ColumnMetaData { Name = "Max Write MB/sec", IsVisible = false, AxisName = "MBsec" } },
            { "MaxIOPs", new ColumnMetaData { Name = "Max IOPs", IsVisible = false, AxisName = "IOPs" } },
            { "MaxReadIOPs", new ColumnMetaData { Name = "Max Read IOPs", IsVisible = false, AxisName = "IOPs" } },
            { "MaxWriteIOPs", new ColumnMetaData { Name = "Max Write IOPs", IsVisible = false, AxisName = "IOPs" } },
            { "MaxLatency", new ColumnMetaData { Name = "Max Latency", IsVisible = false, AxisName = "Latency" } },
            { "MaxReadLatency", new ColumnMetaData { Name = "Max Read Latency", IsVisible = false, AxisName = "Latency" } },
            { "MaxWriteLatency", new ColumnMetaData { Name = "Max Write Latency", IsVisible = false, AxisName = "Latency" } }
        };

        private Dictionary<string, ColumnMetaData> _columns = DefaultColumns;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Dictionary<string, ColumnMetaData> Columns
        {
            get => _columns;
            set { _columns = value; UpdateMetricVisibility(); }
        }

        private void SetMetric()
        {
            Drive = Metric.Drive;
        }

        private void PopulateFileGroupFilter()
        {
            if (databaseID > 0)
            {
                tsFileGroup.DropDownItems.Clear();
                var dt = CommonData.GetFileGroups(databaseID);
                foreach (DataRow r in dt.Rows)
                {
                    var fg = r["FileGroup"] == DBNull.Value ? "{NULL}" : (string)r["FileGroup"];

                    var mnu = new ToolStripMenuItem(fg)
                    {
                        Checked = fg == filegroup,
                        CheckOnClick = true
                    };
                    mnu.Click += Filegroup_Click;
                    tsFileGroup.DropDownItems.Add(mnu);
                }
                tsFileGroup.Visible = tsFileGroup.DropDownItems.Count > 0;
            }
            else
            {
                tsFileGroup.Visible = false;
            }
        }

        private void Filegroup_Click(object sender, EventArgs e)
        {
            var mnu = (ToolStripMenuItem)sender;
            FileGroup = mnu.Checked ? mnu.Text : "";
            RefreshData();
        }

        private void GetDrives()
        {
            drives = new();
            tsDrives.DropDownItems.Clear();

            var dtDrives = CommonData.GetMetricDrives(instanceID);

            foreach (DataRow drive in dtDrives.Rows)
            {
                var name = (string)drive["Name"];
                var label = drive["Label"] == DBNull.Value ? "" : (string)drive["Label"];
                label = name + (string.IsNullOrEmpty(label) ? string.Empty : "     |     " + label);
                drives.Add(name, label);
                var ddDrive = tsDrives.DropDownItems.Add(label);
                ddDrive.Tag = name;
                ddDrive.Click += DdDrive_Click;
            }

            var ddAll = tsDrives.DropDownItems.Add("{All}");
            ddAll.Tag = "";
            ddAll.Click += DdDrive_Click;
        }

        private void DdDrive_Click(object sender, EventArgs e)
        {
            Drive = (string)((ToolStripDropDownItem)sender).Tag;
            RefreshData();
        }

        private DataTable IOStats(int instanceid, DateTime from, DateTime to, int DatabaseID, string drive)
        {
            var dt = new DataTable();

            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand(@"IOStats_Get", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();

                cmd.Parameters.AddWithValue("@InstanceID", instanceid);
                cmd.Parameters.AddWithValue("@FromDate", from);
                cmd.Parameters.AddWithValue("@ToDate", to);
                cmd.Parameters.AddStringIfNotNullOrEmpty("@FileGroup", filegroup);
                cmd.Parameters.AddWithValue("DateGroupingMin", dateGrouping);
                cmd.Parameters.AddStringIfNotNullOrEmpty("Drive", drive);
                cmd.Parameters.AddIfGreaterThanZero("@DatabaseID", DatabaseID);

                cmd.Parameters.AddWithValue("@UTCOffset", DateHelper.UtcOffset);
                if (DateRange.HasTimeOfDayFilter)
                {
                    cmd.Parameters.AddWithValue("Hours", DateRange.TimeOfDay.AsDataTable());
                }
                if (DateRange.HasDayOfWeekFilter)
                {
                    cmd.Parameters.AddWithValue("DaysOfWeek", DateRange.DayOfWeek.AsDataTable());
                }
                cmd.CommandTimeout = Config.DefaultCommandTimeout;
                var da = new SqlDataAdapter(cmd);
                da.Fill(dt);
                DateHelper.ConvertUTCToAppTimeZone(ref dt);
            }
            return dt;
        }

        public void RefreshData(int InstanceID, int databaseID)
        {
            instanceID = InstanceID;
            this.databaseID = databaseID;
            FileGroup = "";
            PopulateFileGroupFilter();
            GetDrives();
            RefreshData();
        }

        public void RefreshData(int InstanceID)
        {
            RefreshData(InstanceID, -1);
        }

        public void RefreshData()
        {
            if (mins != DateRange.DurationMins)
            {
                DateGrouping = DateHelper.DateGrouping(DateRange.DurationMins, 200);
            }

            var dt = IOStats(instanceID, DateRange.FromUTC, DateRange.ToUTC, databaseID, Metric.Drive);
            var cnt = dt.Rows.Count;

            // Group visible columns by axis name
            var columnsByAxis = Columns
                .Where(c => c.Value.IsVisible)
                .GroupBy(c => c.Value.AxisName)
                .ToDictionary(g => g.Key, g => g.Select(c => c.Key).ToArray());

            // Calculate max values for axis synchronization (for specific axes only)
            IOPsAxisMaxValue = 0;
            MBAxisMaxValue = 0;
            foreach (DataRow r in dt.Rows)
            {
                foreach (var kvp in Columns.Where(c => c.Value.IsVisible))
                {
                    var v = r[kvp.Key] == DBNull.Value ? 0 : (double)(decimal)r[kvp.Key];
                    if (kvp.Value.AxisName == "IOPs")
                    {
                        IOPsAxisMaxValue = Math.Max(IOPsAxisMaxValue, v);
                    }
                    else if (kvp.Value.AxisName == "MBsec")
                    {
                        MBAxisMaxValue = Math.Max(MBAxisMaxValue, v);
                    }
                }
            }

            // Get all visible columns
            var allVisibleColumns = Columns.Where(c => c.Value.IsVisible).Select(c => c.Key).ToArray();

            if (allVisibleColumns.Length > 0)
            {
                // Create series names dictionary for friendly names
                var seriesNames = Columns.ToDictionary(c => c.Key, c => c.Value.Name);

                // Create column-to-axis-name mapping from ColumnMetaData
                var columnAxisNames = Columns
                    .Where(c => c.Value.IsVisible)
                    .ToDictionary(c => c.Key, c => c.Value.AxisName);

                // Setup Y-axes configurations with names - order determines rendering
                var yAxes = new List<YAxisConfiguration>();
                var axisNameToIndexMap = new Dictionary<string, int>();

                // Define axis order and properties
                var axisDefinitions = new[]
                {
                    new { Name = "MBsec", Label = "MB/sec", Position = AxisPosition.Start, MaxLimit = (double?)null },
                    new { Name = "IOPs", Label = "IOPs", Position = AxisPosition.End, MaxLimit = (double?)null },
                    new { Name = "Latency", Label = "Latency (ms)", Position = AxisPosition.End, MaxLimit = (double?)LatencyLimit }
                };

                // Add axes only if they have visible columns
                foreach (var axisDef in axisDefinitions)
                {
                    if (columnsByAxis.ContainsKey(axisDef.Name))
                    {
                        axisNameToIndexMap[axisDef.Name] = yAxes.Count;
                        yAxes.Add(new YAxisConfiguration
                        {
                            Name = axisDef.Name,  // Add axis name for name-based mapping
                            Label = axisDef.Label,
                            Format = "0.0",
                            MinLimit = 0,
                            MaxLimit = axisDef.MaxLimit,
                            Position = axisDef.Position
                        });
                    }
                }

                // Update chart using ChartHelper with name-based axis mapping
                var config = new ChartConfiguration
                {
                    DateColumn = "SnapshotDate",
                    MetricColumns = allVisibleColumns,
                    ChartType = ChartTypes.Line,
                    ShowLegend = true,
                    LegendPosition = legendPosition,
                    LineSmoothness = SmoothLines ? 1 : 0,
                    GeometrySize = cnt <= 200 ? PointSize : 0,
                    XAxisMin = DateRange.FromUTC.ToAppTimeZone(),
                    XAxisMax = DateRange.ToUTC.ToAppTimeZone(),
                    SeriesNames = seriesNames,
                    YAxes = yAxes.ToArray(),
                    ColumnAxisNames = columnAxisNames,
                    DateUnit = TimeSpan.FromMinutes(dateGrouping)
                };

                ChartHelper.UpdateChart(chartIO, dt, config);

                // Store references to the axes for external access (e.g., DrivePerformance synchronization)
                var yAxesArray = chartIO.YAxes.ToArray();
                TimeAxis = chartIO.XAxes.FirstOrDefault() as DateTimeAxis;
                MBsecAxis = axisNameToIndexMap.TryGetValue("MBsec", out var mbIdx) && mbIdx < yAxesArray.Length ? yAxesArray[mbIdx] as Axis : null;
                IOPsAxis = axisNameToIndexMap.TryGetValue("IOPs", out var iopsIdx) && iopsIdx < yAxesArray.Length ? yAxesArray[iopsIdx] as Axis : null;
                LatencyAxis = axisNameToIndexMap.TryGetValue("Latency", out var latIdx) && latIdx < yAxesArray.Length ? yAxesArray[latIdx] as Axis : null;
            }

            lblIOPerformance.Text = databaseID > 0 ? "IO Performance: Database" : (string.IsNullOrEmpty(Drive) ? "IO Performance: Instance" : "IO Performance: " + DriveLabel(Drive));
            toolStrip1.Tag = databaseID > 0 ? "ALT" : null; // set tag to ALT to use the alternate menu renderer
            toolStrip1.ApplyTheme(DBADashUser.SelectedTheme);
        }

        public string DriveLabel(string drive)
        {
            return drives.TryGetValue(Drive, out var label) ? label : drive;
        }

        private void UpdateMetricVisibility()
        {
            RefreshData();
        }

        private void IOPerformance_Load(object sender, EventArgs e)
        {
            DateHelper.AddDateGroups(tsDateGroup, TsDateGroup_Click);
        }

        private void TsDateGroup_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            dateGrouping = Convert.ToInt32(ts.Tag);
            tsDateGroup.Text = DateHelper.DateGroupString(dateGrouping);
            RefreshData();
        }

        private void TsIOSummary_Click(object sender, EventArgs e)
        {
            IOSummaryForm summaryForm = new()
            {
                InstanceID = instanceID,
                DatabaseID = databaseID,
                FromDate = DateRange.FromUTC,
                ToDate = DateRange.ToUTC
            };
            summaryForm.ShowSingleInstance();
        }

        private void TsClose_Click(object sender, EventArgs e)
        {
            Close.Invoke(this, EventArgs.Empty);
        }

        private void TsUp_Click(object sender, EventArgs e)
        {
            MoveUp.Invoke(this, EventArgs.Empty);
        }

        private void SetLatencyLimit(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            SetLatencyLimit(Convert.ToDouble(ts.Text));
        }

        public void SetLatencyLimit(double latencyLimit)
        {
            LatencyLimit = latencyLimit;
            if (LatencyAxis != null)
            {
                LatencyAxis.MaxLimit = latencyLimit;
            }
            foreach (ToolStripMenuItem itm in latencyLimitToolStripMenuItem.DropDownItems)
            {
                itm.Checked = Convert.ToInt64(itm.Text) == Convert.ToInt64(latencyLimit);
            }
        }

        private void TsMeasures_Click(object sender, EventArgs e)
        {
            var selectableMetrics = Columns.Values
                .Select(metric => (ISelectable)metric)
                .ToList();

            var colSelection = new SelectColumns() { Items = selectableMetrics, Text = "Select Measures" };
            colSelection.ApplyTheme(DBADashUser.SelectedTheme);
            colSelection.ShowDialog();
            if (colSelection.DialogResult == DialogResult.OK)
            {
                Columns.ApplyVisibility(colSelection.Items);
            }
            RefreshData();
        }

        private void SetLegendPosition(object sender, EventArgs e)
        {
            var item = (ToolStripMenuItem)sender;
            Enum.TryParse(item.Tag.ToString(), out legendPosition);
            foreach (ToolStripMenuItem menuItem in tsLegend.DropDownItems.OfType<ToolStripMenuItem>())
            {
                menuItem.Checked = menuItem == item;
            }
            chartIO.LegendPosition = legendPosition;
        }
    }
}