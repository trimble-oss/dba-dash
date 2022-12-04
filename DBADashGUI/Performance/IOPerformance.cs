using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    public partial class IOPerformance : UserControl, IMetricChart
    {
        public IOPerformance()
        {
            InitializeComponent();
        }

        Int32 mins;
        DateTime ioTime = DateTime.MinValue;
        Int32 instanceID;
        private Int32 dateGrouping;
        bool smoothLines = true;
        Int32 databaseid = 0;
        public Int32 PointSize;
        string filegroup = "";

        public event EventHandler<EventArgs> Close;
        public event EventHandler<EventArgs> MoveUp;

        public bool CloseVisible
        {
            get
            {
                return tsClose.Visible;
            }
            set
            {
                tsClose.Visible = value;
            }
        }
        public string FileGroup
        {
            get
            {
                return filegroup;
            }
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
            get
            {
                return Metric.Drive;
            }
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
            get
            {
                return smoothLines;
            }
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
            get
            {
                return tsUp.Visible;
            }
            set
            {
                tsUp.Visible = value;
            }
        }

        private IOMetric _metric = new();
        public IOMetric Metric { get => _metric; set { _metric = value; SetMetric(); } }

        IMetric IMetricChart.Metric { get => Metric; }

        private readonly Dictionary<string, ColumnMetaData> columns = new()
            {
                {"MBsec", new ColumnMetaData{Alias="MB/sec",isVisible=true } },
                {"ReadMBsec", new ColumnMetaData{Alias="Read MB/sec",isVisible=false } },
                {"WriteMBsec", new ColumnMetaData{Alias="Write MB/sec",isVisible=false } },
                {"IOPs", new ColumnMetaData{Alias="IOPs",isVisible=true,axis=1 } },
                {"ReadIOPs", new ColumnMetaData{Alias="Read IOPs",isVisible=false,axis=1 } },
                {"WriteIOPs", new ColumnMetaData{Alias="Write IOPs",isVisible=false ,axis=1} },
                {"Latency", new ColumnMetaData{Alias="Latency",isVisible=true,axis=2 } },
                {"ReadLatency", new ColumnMetaData{Alias="Read Latency",isVisible=false,axis=2} },
                {"WriteLatency", new ColumnMetaData{Alias="Write Latency",isVisible=false,axis=2 } },
                {"MaxMBsec", new ColumnMetaData{Alias="Max MB/sec",isVisible=false } },
                {"MaxReadMBsec", new ColumnMetaData{Alias="Max Read MB/sec",isVisible=false } },
                {"MaxWriteMBsec", new ColumnMetaData{Alias="Max Write MB/sec",isVisible=false } },
                {"MaxIOPs", new ColumnMetaData{Alias="Max IOPs",isVisible=false,axis=1 } },
                {"MaxReadIOPs", new ColumnMetaData{Alias="Max Read IOPs",isVisible=false,axis=1 } },
                {"MaxWriteIOPs", new ColumnMetaData{Alias="Max Write IOPs",isVisible=false ,axis=1} },
                {"MaxLatency", new ColumnMetaData{Alias="Max Latency",isVisible=false,axis=2 } },
                {"MaxReadLatency", new ColumnMetaData{Alias="Max Read Latency",isVisible=false ,axis=2} },
                {"MaxWriteLatency", new ColumnMetaData{Alias="Max Write Latency",isVisible=false ,axis=2} }
            };

        private void SetMetric()
        {
            AddMeasures();
            foreach (ToolStripMenuItem mnu in tsMeasures.DropDownItems)
            {
                mnu.Checked = Metric.VisibleMetrics.Contains(mnu.Name);
            }
            Drive = Metric.Drive;
        }

        private void PopulateFileGroupFilter()
        {
            if (databaseid > 0)
            {
                tsFileGroup.DropDownItems.Clear();
                var dt = CommonData.GetFileGroups(databaseid);
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
            if (mnu.Checked)
            {
                FileGroup = mnu.Text;
            }
            else
            {
                FileGroup = "";
            }
            RefreshData();
        }

        private void GetDrives()
        {
            tsDrives.DropDownItems.Clear();
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("DriveLetters_Get", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
                cmd.Parameters.AddWithValue("@InstanceID", instanceID);
                using var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    var name = (string)rdr["Name"];
                    var label = rdr["Label"] == DBNull.Value ? "" : (string)rdr["Label"];
                    var ddDrive = tsDrives.DropDownItems.Add(name + "     |     " + label);
                    ddDrive.Tag = name;
                    ddDrive.Click += DdDrive_Click;
                }
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

        private DataTable IOStats(Int32 instanceid, DateTime from, DateTime to, Int32 DatabaseID, string drive)
        {
            var dt = new DataTable();

            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand(@"IOStats_Get", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();

                cmd.Parameters.AddWithValue("@InstanceID", instanceid);
                cmd.Parameters.AddWithValue("@FromDate", from);
                cmd.Parameters.AddWithValue("@ToDate", to);
                if (filegroup.Length > 0)
                {
                    cmd.Parameters.AddWithValue("@FileGroup", filegroup);
                }
                cmd.Parameters.AddWithValue("DateGroupingMin", dateGrouping);
                if (drive != "")
                {
                    cmd.Parameters.AddWithValue("Drive", drive);
                }
                if (DatabaseID > 0)
                {
                    cmd.Parameters.AddWithValue("@DatabaseID", DatabaseID);
                }
                cmd.Parameters.AddWithValue("@UTCOffset", DateHelper.UtcOffset);
                if (DateRange.HasTimeOfDayFilter)
                {
                    cmd.Parameters.AddWithValue("Hours", DateRange.TimeOfDay.AsDataTable());
                }
                if (DateRange.HasDayOfWeekFilter)
                {
                    cmd.Parameters.AddWithValue("DaysOfWeek", DateRange.DayOfWeek.AsDataTable());
                }
                cmd.CommandTimeout = Properties.Settings.Default.CommandTimeout;
                var da = new SqlDataAdapter(cmd);
                da.Fill(dt);
            }
            return dt;
        }

        public void AddMeasures()
        {
            if (tsMeasures.DropDownItems.Count == 0)
            {
                bool addVisible = Metric.VisibleMetrics.Count == 0;
                foreach (var c in columns)
                {
                    ToolStripMenuItem dd = new(c.Value.Alias)
                    {
                        Name = (string)c.Key,
                        CheckOnClick = true,
                        Checked = c.Value.isVisible
                    };
                    dd.Click += MeasureDropDown_Click;
                    tsMeasures.DropDownItems.Add(dd);
                    if (addVisible && c.Value.isVisible)
                    {
                        Metric.VisibleMetrics.Add(c.Key);
                    }
                }
            }
        }

        public void RefreshData(Int32 InstanceID, Int32 databaseID)
        {
            this.instanceID = InstanceID;
            this.databaseid = databaseID;
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
                dateGrouping = DateHelper.DateGrouping(DateRange.DurationMins, 200);
                tsDateGroup.Text = DateHelper.DateGroupString(dateGrouping);
                mins = DateRange.DurationMins;
            }

            string DateFormat = "HH:mm";
            if (mins > 1440)
            {
                DateFormat = "yyyy-MM-dd HH:mm";
            }
            var dt = IOStats(instanceID, DateRange.FromUTC, DateRange.ToUTC, databaseid, Metric.Drive);
            var cnt = dt.Rows.Count;



            foreach (ToolStripMenuItem ts in tsMeasures.DropDownItems)
            {
                columns[ts.Name].isVisible = ts.Checked;
            }
            foreach (var s in columns.Keys)
            {
                columns[s].Points = new DateTimePoint[cnt];
            }

            Int32 i = 0;
            foreach (DataRow r in dt.Rows)
            {
                foreach (string s in columns.Keys)
                {
                    var v = r[s] == DBNull.Value ? 0 : (double)(decimal)r[s];
                    ioTime = (DateTime)r["SnapshotDate"];
                    columns[s].Points[i] = new DateTimePoint(ioTime.ToAppTimeZone(), v);
                }
                i++;
            }

            SeriesCollection sc;

            sc = new SeriesCollection();
            chartIO.Series = sc;
            foreach (string s in columns.Keys)
            {
                sc.Add(new LineSeries
                {
                    Title = columns[s].Alias,
                    Tag = s,
                    ScalesYAt = columns[s].axis,
                    PointGeometrySize = cnt <= 100 ? PointSize : 0,
                    LineSmoothness = SmoothLines ? 1 : 0
                }
                );
            }
            chartIO.AxisX.Clear();
            chartIO.AxisY.Clear();
            chartIO.AxisX.Add(new Axis
            {
                Title = "Time",
                LabelFormatter = val => new System.DateTime((long)val).ToString(DateFormat)

            });
            chartIO.AxisY.Add(new Axis
            {
                Title = "MB/sec",
                LabelFormatter = val => val.ToString("0.0 MB"),
                MinValue = 0
            });
            chartIO.AxisY.Add(new Axis
            {
                Title = "IOPs",
                LabelFormatter = val => val.ToString("0.0 IOPs"),
                Position = AxisPosition.RightTop,
                MinValue = 0
            });
            chartIO.AxisY.Add(new Axis
            {
                Title = "Latency",
                LabelFormatter = val => val.ToString("0.0ms"),
                Position = AxisPosition.RightTop,
                MinValue = 0,
                MaxValue = 200
            });

            foreach (LineSeries s in chartIO.Series.Cast<LineSeries>())
            {
                var c = columns[(string)s.Tag];
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
                s.Visibility = c.isVisible ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;

            }
            if (chartIO.Series[0].Values.Count == 1)
            {
                chartIO.Series.Clear(); // fix tends to zero error
            }
            lblIOPerformance.Text = databaseid > 0 ? "IO Performance: Database" : "IO Performance: Instance";
        }

        private void MeasureDropDown_Click(object sender, EventArgs e)
        {
            var dd = (ToolStripMenuItem)sender;
            Metric.VisibleMetrics.Clear();
            foreach (LineSeries s in chartIO.Series.Cast<LineSeries>())
            {
                var metricName = (string)s.Tag;
                if (metricName == dd.Name)
                {
                    s.Visibility = dd.Checked ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
                }
                if (s.Visibility == System.Windows.Visibility.Visible)
                {
                    Metric.VisibleMetrics.Add(metricName);
                }
            }

        }

        private void IOPerformance_Load(object sender, EventArgs e)
        {
            DateHelper.AddDateGroups(tsDateGroup, TsDateGroup_Click);
            AddMeasures();
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
            var frm = new IOSummaryForm()
            {
                InstanceID = instanceID,
                DatabaseID = databaseid,
                FromDate = DateRange.FromUTC,
                ToDate = DateRange.ToUTC
            };
            frm.ShowDialog(this);
        }

        private void TsClose_Click(object sender, EventArgs e)
        {
            Close.Invoke(this, new EventArgs());
        }

        private void TsUp_Click(object sender, EventArgs e)
        {
            MoveUp.Invoke(this, new EventArgs());
        }


    }
}
