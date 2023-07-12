using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using DBADashGUI.Pickers;
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
        private DateTime ioTime = DateTime.MinValue;
        private int instanceID;
        private int dateGrouping;
        private bool smoothLines = true;
        private int databaseID;
        public int PointSize;
        private string filegroup = "";

        public event EventHandler<EventArgs> Close;

        public event EventHandler<EventArgs> MoveUp;

        private Dictionary<string, string> drives = new();

        public bool CloseVisible
        {
            get => tsClose.Visible;
            set => tsClose.Visible = value;
        }

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

        public bool SmoothLines
        {
            get => smoothLines;
            set
            {
                smoothLines = value;
                foreach (LineSeries s in chartIO.Series.Cast<LineSeries>())
                {
                    s.LineSmoothness = smoothLines ? 1 : 0;
                }
            }
        }

        public bool MoveUpVisible
        {
            get => tsUp.Visible; set => tsUp.Visible = value;
        }

        private IOMetric _metric = new();

        public IOMetric Metric
        { get => _metric; set { _metric = value; SetMetric(); } }

        public bool DateGroupingVisible
        {
            get => tsDateGroup.Visible; set => tsDateGroup.Visible = value;
        }

        public bool DriveVisible
        {
            get => tsDrives.Visible; set => tsDrives.Visible = value;
        }

        public bool MeasuresVisible
        {
            get => tsMeasures.Visible; set => tsMeasures.Visible = value;
        }

        public bool SummaryVisible
        {
            get => tsIOSummary.Visible; set => tsIOSummary.Visible = value;
        }

        public bool FileGroupVisible
        {
            get => tsFileGroup.Visible; set => tsFileGroup.Visible = value;
        }

        public bool OptionsVisible
        {
            get => tsOptions.Visible; set => tsOptions.Visible = value;
        }

        IMetric IMetricChart.Metric => Metric;

        public static readonly Dictionary<string, ColumnMetaData> DefaultColumns = new()
        {
            { "MBsec", new ColumnMetaData { Name = "MB/sec", IsVisible = true } },
            { "ReadMBsec", new ColumnMetaData { Name = "Read MB/sec", IsVisible = false } },
            { "WriteMBsec", new ColumnMetaData { Name = "Write MB/sec", IsVisible = false } },
            { "IOPs", new ColumnMetaData { Name = "IOPs", IsVisible = true, axis = 1 } },
            { "ReadIOPs", new ColumnMetaData { Name = "Read IOPs", IsVisible = false, axis = 1 } },
            { "WriteIOPs", new ColumnMetaData { Name = "Write IOPs", IsVisible = false, axis = 1 } },
            { "Latency", new ColumnMetaData { Name = "Latency", IsVisible = true, axis = 2 } },
            { "ReadLatency", new ColumnMetaData { Name = "Read Latency", IsVisible = false, axis = 2 } },
            { "WriteLatency", new ColumnMetaData { Name = "Write Latency", IsVisible = false, axis = 2 } },
            { "MaxMBsec", new ColumnMetaData { Name = "Max MB/sec", IsVisible = false } },
            { "MaxReadMBsec", new ColumnMetaData { Name = "Max Read MB/sec", IsVisible = false } },
            { "MaxWriteMBsec", new ColumnMetaData { Name = "Max Write MB/sec", IsVisible = false } },
            { "MaxIOPs", new ColumnMetaData { Name = "Max IOPs", IsVisible = false, axis = 1 } },
            { "MaxReadIOPs", new ColumnMetaData { Name = "Max Read IOPs", IsVisible = false, axis = 1 } },
            { "MaxWriteIOPs", new ColumnMetaData { Name = "Max Write IOPs", IsVisible = false, axis = 1 } },
            { "MaxLatency", new ColumnMetaData { Name = "Max Latency", IsVisible = false, axis = 2 } },
            { "MaxReadLatency", new ColumnMetaData { Name = "Max Read Latency", IsVisible = false, axis = 2 } },
            { "MaxWriteLatency", new ColumnMetaData { Name = "Max Write Latency", IsVisible = false, axis = 2 } }
        };

        private Dictionary<string, ColumnMetaData> _columns = DefaultColumns;

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
                    string fg = r["FileGroup"] == DBNull.Value ? "{NULL}" : (string)r["FileGroup"];

                    var mnu = new ToolStripMenuItem(fg)
                    {
                        Checked = fg == filegroup,
                        CheckOnClick = true
                    };
                    mnu.Click += Filegroup_Click; ;
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
            }
            return dt;
        }

        public void RefreshData(int InstanceID, int databaseID)
        {
            this.instanceID = InstanceID;
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

        private static readonly string DateFormat = "HH:mm";
        private static readonly string LongDateFormat = "yyyy-MM-dd HH:mm";

        public Axis TimeAxis = new()
        {
            Title = "Time",
            LabelFormatter = val => new DateTime((long)val).ToString(DateFormat)
        };

        public Axis MBsecAxis = new()
        {
            Title = "MB/sec",
            LabelFormatter = val => val.ToString("0.0 MB"),
            MinValue = 0
        };

        public Axis IOPsAxis = new()
        {
            Title = "IOPs",
            LabelFormatter = val => val.ToString("0.0 IOPs"),
            Position = AxisPosition.RightTop,
            MinValue = 0
        };

        public Axis LatencyAxis = new()
        {
            Title = "Latency",
            LabelFormatter = val => val.ToString("0.0ms"),
            Position = AxisPosition.RightTop,
            MinValue = 0,
            MaxValue = 200
        };

        private double iopsMax;
        private double mbMax;
        public double IOPsAxisMaxValue => iopsMax;

        public double MBAxisMaxValue => mbMax;

        public void RefreshData()
        {
            if (mins != DateRange.DurationMins)
            {
                DateGrouping = DateHelper.DateGrouping(DateRange.DurationMins, 200);
            }

            TimeAxis.LabelFormatter = val => new DateTime((long)val).ToString(mins > 1440 ? LongDateFormat : DateFormat);

            var dt = IOStats(instanceID, DateRange.FromUTC, DateRange.ToUTC, databaseID, Metric.Drive);
            var cnt = dt.Rows.Count;

            foreach (var s in Columns.Keys)
            {
                Columns[s].Points = new DateTimePoint[cnt];
            }

            int i = 0;
            iopsMax = 0;
            mbMax=0;
            foreach (DataRow r in dt.Rows)
            {
                foreach (string s in Columns.Keys)
                {
                    var v = r[s] == DBNull.Value ? 0 : (double)(decimal)r[s];
                    ioTime = (DateTime)r["SnapshotDate"];
                    Columns[s].Points[i] = new DateTimePoint(ioTime.ToAppTimeZone(), v);
                    if (Columns[s].IsVisible && Columns[s].axis == 1)
                    {
                        iopsMax = Math.Max(iopsMax, v);
                    }
                    else if (Columns[s].IsVisible && Columns[s].axis == 0)
                    {
                        mbMax = Math.Max(mbMax, v);
                    }
                }
                i++;
            }

            var sc = new SeriesCollection();
            chartIO.Series = sc;
            foreach (string s in Columns.Keys)
            {
                sc.Add(new LineSeries
                {
                    Title = Columns[s].Name,
                    Tag = s,
                    ScalesYAt = Columns[s].axis,
                    PointGeometrySize = cnt <= 200 ? PointSize : 0,
                    LineSmoothness = SmoothLines ? 1 : 0
                }
                );
            }

            foreach (LineSeries s in chartIO.Series.Cast<LineSeries>())
            {
                var c = Columns[(string)s.Tag];
                if (s.Values == null)
                {
                    var v = new ChartValues<DateTimePoint>();
                    v.AddRange(c.Points);
                    s.Values = v;
                }
                else
                {
                    s.Values.AddRange(c.Points);
                }
                s.Visibility = c.IsVisible ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
            }
            if (chartIO.Series[0].Values.Count == 1)
            {
                chartIO.Series.Clear(); // fix tends to zero error
            }
            lblIOPerformance.Text = databaseID > 0 ? "IO Performance: Database" : (string.IsNullOrEmpty(Drive) ? "IO Performance: Instance" : "IO Performance: " + DriveLabel(Drive));
        }

        public string DriveLabel(string drive)
        {
            return drives.TryGetValue(Drive, out var label) ? label : drive;
        }

        private void UpdateMetricVisibility()
        {
            foreach (LineSeries s in chartIO.Series.Cast<LineSeries>())
            {
                var metricName = (string)s.Tag;
                s.Visibility = Columns[metricName].IsVisible ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
            }
        }

        private void MeasureDropDown_Click(object sender, EventArgs e)
        {
            var dd = (ToolStripMenuItem)sender;
            Metric.VisibleMetrics.Clear();
            Columns[dd.Name].IsVisible = dd.Checked;
            if (dd.Checked)
            {
                Metric.VisibleMetrics.Add(dd.Name);
            }
            else
            {
                Metric.VisibleMetrics.Remove(dd.Name);
            }
            UpdateMetricVisibility();
        }

        private void IOPerformance_Load(object sender, EventArgs e)
        {
            DateHelper.AddDateGroups(tsDateGroup, TsDateGroup_Click);
            chartIO.AxisX.Clear();
            chartIO.AxisY.Clear();
            chartIO.AxisX.Add(TimeAxis);
            chartIO.AxisY.Add(MBsecAxis);
            chartIO.AxisY.Add(IOPsAxis);
            chartIO.AxisY.Add(LatencyAxis);
        }

        private void TsDateGroup_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            dateGrouping = Convert.ToInt32(ts.Tag);
            tsDateGroup.Text = DateHelper.DateGroupString(dateGrouping);
            RefreshData();
        }

        private static IOSummaryForm SummaryForm;

        private void TsIOSummary_Click(object sender, EventArgs e)
        {
            SummaryForm?.Close();
            SummaryForm = new IOSummaryForm()
            {
                InstanceID = instanceID,
                DatabaseID = databaseID,
                FromDate = DateRange.FromUTC,
                ToDate = DateRange.ToUTC
            };
            SummaryForm.FormClosed += delegate { SummaryForm = null; };

            SummaryForm.Show();
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
            LatencyAxis.MaxValue = latencyLimit;
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
            colSelection.ShowDialog();
            if (colSelection.DialogResult == DialogResult.OK)
            {
                Columns.ApplyVisibility(colSelection.Items);
            }
            RefreshData();
        }
    }
}