using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
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
            public System.DateTime DateTime { get; set; }
            public double Value { get; set; }
        }

        public DateTimePoint x;
        Int32 instanceID;
        DateTime lastWait = DateTime.MinValue;
        private Int32 dateGrouping;
        Int32 mins;
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

        public string WaitType
        {
            get
            {
                return Metric.WaitType;
            }
            set
            {
                Metric.WaitType = value;
                SetMetric();
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
        private WaitMetric _metric = new();
        public WaitMetric Metric { get => _metric; set { _metric = value; SetMetric(); } }

        private void SetMetric()
        {
            criticalWaitsOnlyToolStripMenuItem.Checked = Metric.CriticalWaitsOnly;
            tsFilter.Text = Metric.WaitType == "" ? (criticalWaitsOnlyToolStripMenuItem.Checked ? "*Critical Waits*" : "") : Metric.WaitType;
        }

        IMetric IMetricChart.Metric { get => Metric; }

        public void RefreshData(Int32 instanceID)
        {
            this.instanceID = instanceID;

            if (mins != DateRange.DurationMins)
            {
                dateGrouping = Common.DateGrouping(DateRange.DurationMins, 35);
                if (dateGrouping < 1)
                {
                    dateGrouping = 1;
                }
                tsDateGrouping.Text = Common.DateGroupString(dateGrouping);
                mins = DateRange.DurationMins;
            }

            RefreshData();
        }


        private DataTable GetWaitsDT()
        {

            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.Waits_Get", cn))
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();

                cmd.Parameters.AddWithValue("InstanceID", instanceID);
                cmd.Parameters.AddWithValue("FromDate", DateRange.FromUTC);
                cmd.Parameters.AddWithValue("ToDate", DateRange.ToUTC);
                cmd.Parameters.AddWithValue("DateGroupingMin", dateGrouping);
                cmd.Parameters.AddWithValue("CriticalWaitsOnly", criticalWaitsOnlyToolStripMenuItem.Checked);
                if (!String.IsNullOrEmpty(Metric.WaitType))
                {
                    cmd.Parameters.AddWithValue("WaitType", Metric.WaitType);
                }
                cmd.Parameters.AddWithValue("@UTCOffset", Common.UtcOffset);
                if (DateRange.HasTimeOfDayFilter)
                {
                    cmd.Parameters.AddWithValue("Hours", DateRange.TimeOfDay.AsDataTable());
                }
                if (DateRange.HasDayOfWeekFilter)
                {
                    cmd.Parameters.AddWithValue("DaysOfWeek", DateRange.DayOfWeek.AsDataTable());
                }
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = Properties.Settings.Default.CommandTimeout;
                DataTable dt = new();
                da.Fill(dt);
                return dt;
            }
        }

        public void RefreshData()
        {

            waitChart.Series.Clear();
            waitChart.AxisX.Clear();
            waitChart.AxisY.Clear();
            lastWait = DateTime.MinValue;

            var dt = GetWaitsDT();

            if (dt.Rows.Count == 0)
            {
                return;
            }
            var dPoints = new Dictionary<string, ChartValues<DateTimePoint>>();
            string current = string.Empty;
            ChartValues<DateTimePoint> values = new();
            foreach (DataRow r in dt.Rows)
            {
                var waitType = (string)r["WaitType"];
                var time = (DateTime)r["Time"];
                if (time > lastWait)
                {
                    lastWait = time;
                }
                if (current != waitType)
                {
                    if (values.Count > 0) { dPoints.Add(current, values); }
                    values = new ChartValues<DateTimePoint>();
                    current = waitType;
                }
                values.Add(new DateTimePoint(((DateTime)r["Time"]).ToLocalTime(), (double)(decimal)r["WaitTimeMsPerSec"]));
            }
            if (values.Count > 0)
            {
                dPoints.Add(current, values);
                values = new ChartValues<DateTimePoint>();
            }


            CartesianMapper<DateTimePoint> dayConfig = Mappers.Xy<DateTimePoint>()
.X(dateModel => dateModel.DateTime.Ticks / TimeSpan.FromMinutes(dateGrouping == 0 ? 1 : dateGrouping).Ticks)
.Y(dateModel => dateModel.Value);


            SeriesCollection s1 = new(dayConfig);
            foreach (var x in dPoints)
            {
                s1.Add(new StackedColumnSeries
                {
                    Title = x.Key,
                    Values = x.Value
                });
            }
            waitChart.Series = s1;

            string format = "t";
            if (dateGrouping >= 1440)
            {
                format = "yyyy-MM-dd";
            }
            else if (mins >= 1440)
            {
                format = "yyyy-MM-dd HH:mm";
            }
            waitChart.AxisX.Add(new Axis
            {
                LabelFormatter = value => new DateTime((long)(value * TimeSpan.FromMinutes(dateGrouping == 0 ? 1 : dateGrouping).Ticks)).ToString(format)
            });
            waitChart.AxisY.Add(new Axis
            {
                LabelFormatter = val => val.ToString("0ms/sec")

            });
        }


        private void Waits_Load(object sender, EventArgs e)
        {
            Common.AddDateGroups(tsDateGrouping, TsDateGrouping_Click);
        }

        private void TsDateGrouping_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            dateGrouping = Convert.ToInt32(ts.Tag);
            tsDateGrouping.Text = Common.DateGroupString(dateGrouping);
            RefreshData();
        }

        private void TsFilterWaitType_Click(object sender, EventArgs e)
        {
            string wt = Metric.WaitType;
            if (Common.ShowInputDialog(ref wt, "Wait Type (LIKE):") == DialogResult.OK)
            {
                WaitType = wt.EndsWith("%") || wt.Length == 0 ? wt : wt + "%";
                RefreshData();
            }
        }

        private void StringFilterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string wt = Metric.WaitType;
            if (Common.ShowInputDialog(ref wt, "Wait Type (LIKE):") == DialogResult.OK)
            {
                WaitType = wt.EndsWith("%") || wt.Length == 0 ? wt : wt + "%";
                criticalWaitsOnlyToolStripMenuItem.Checked = false;
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
            Close.Invoke(this, new EventArgs());
        }

        private void TsUp_Click(object sender, EventArgs e)
        {
            MoveUp.Invoke(this, new EventArgs());
        }
    }
}
