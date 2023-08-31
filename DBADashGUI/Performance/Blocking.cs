using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Wpf;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    public partial class Blocking : UserControl, IMetricChart
    {
        public Blocking()
        {
            InitializeComponent();
        }

        private Int32 InstanceID;
        private double maxBlockedTime = 0;
        private Int32 databaseID = 0;

        public bool CloseVisible
        {
            get => tsClose.Visible; set => tsClose.Visible = value;
        }

        public event EventHandler<EventArgs> Close;

        public event EventHandler<EventArgs> MoveUp;

        public void RefreshData(Int32 InstanceID, Int32 databaseID)
        {
            this.InstanceID = InstanceID;
            this.databaseID = databaseID;
            RefreshData();
        }

        public void RefreshData(int InstanceID)
        {
            RefreshData(InstanceID, -1);
        }

        private DataTable GetDT()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.BlockingSnapshots_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
                cmd.Parameters.AddWithValue("@InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("@FromDate", DateRange.FromUTC);
                cmd.Parameters.AddWithValue("@ToDate", DateRange.ToUTC);
                cmd.Parameters.AddIfGreaterThanZero("@DatabaseID", databaseID);
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

                DataTable dt = new();
                da.Fill(dt);
                return dt;
            }
        }

        private double MaxPointShapeDiameter => maxBlockedTime switch
        {
            > 3600000 => 60,
            > 600000 => 30,
            > 60000 => 10,
            _ => 5
        };

        public bool MoveUpVisible
        {
            get => tsUp.Visible; set => tsUp.Visible = value;
        }

        public BlockingMetric Metric { get; set; } = new();

        IMetric IMetricChart.Metric { get => Metric; }

        public void RefreshData()
        {
            var dt = GetDT();

            chartBlocking.AxisX.Clear();
            chartBlocking.AxisY.Clear();
            chartBlocking.Series.Clear();
            maxBlockedTime = 0;

            var points = new BlockingPoint[dt.Rows.Count];
            Int32 i = 0;
            double Ymax = 100;

            foreach (DataRow r in dt.Rows)
            {
                var dtm = (DateTime)r["SnapshotDateUTC"];
                var blockedCnt = (Int32)r["BlockedSessionCount"];
                var blockedTime = (Int64)r["BlockedWaitTime"];
                var snapshotID = (Int32)r["BlockingSnapshotID"];
                if (blockedTime > maxBlockedTime)
                {
                    maxBlockedTime = blockedTime;
                }
                points[i] = new BlockingPoint(snapshotID, dtm.ToAppTimeZone(), blockedCnt, blockedTime);
                Ymax = blockedCnt > Ymax ? blockedCnt : Ymax;
                i += 1;
            }

            var mapper = Mappers.Weighted<BlockingPoint>()
                .X(value => value.SnapshotDate.Ticks)
                .Y(value => value.BlockedSessions)
                .Weight(value => value.BlockedWaitTime);

            SeriesCollection s1 = new(mapper)
                {
                    new ScatterSeries
                    {
                    Title= "Blocking Snapshot",
                    Values = new ChartValues<BlockingPoint>(points),
                    MinPointShapeDiameter = 5,
                    MaxPointShapeDiameter = MaxPointShapeDiameter
                    }
                };

            string format = DateRange.DurationMins < 1440 ? "HH:mm" : "yyyy-MM-dd HH:mm";
            chartBlocking.AxisX.Add(new Axis
            {
                LabelFormatter = val => new System.DateTime((long)val).ToString(format),
                MinValue = DateRange.FromUTC.ToAppTimeZone().Ticks,
                MaxValue = DateRange.ToUTC.ToAppTimeZone().Ticks
            });
            chartBlocking.AxisY.Add(new Axis
            {
                Title = "Blocked Sessions",
                LabelFormatter = val => val.ToString(),
                MinValue = 0,
                MaxValue = Ymax
            });
            chartBlocking.Series = s1;

            lblBlocking.Text = databaseID > 0 ? "Blocking: Database" : "Blocking: Instance";
            toolStrip1.BackColor = databaseID > 0 ? DashColors.DatabaseLevelTitleColor : Control.DefaultBackColor;
            toolStrip1.ForeColor = toolStrip1.BackColor.ContrastColor();
        }

        private void ChartBlocking_DataClick(object sender, ChartPoint chartPoint)
        {
            var blockPoint = (BlockingPoint)chartPoint.Instance;
            RunningQueriesViewer frm = new()
            {
                SnapshotDateFrom = blockPoint.SnapshotDate.AppTimeZoneToUtc(),
                SnapshotDateTo = blockPoint.SnapshotDate.AppTimeZoneToUtc(),
                InstanceID = InstanceID,
                ShowRootBlockers = true,
            };
            frm.Show(this);
        }

        private void Blocking_Load(object sender, EventArgs e)
        {
            chartBlocking.DataClick += ChartBlocking_DataClick;
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

internal class BlockingPoint
{
    public Int32 SnapshotID { get; set; }

    public DateTime SnapshotDate { get; set; }

    public Int32 BlockedSessions { get; set; }

    public Int64 BlockedWaitTime { get; set; }

    public BlockingPoint(Int32 snapshotID, DateTime snapshotDate, Int32 blockedSessions, Int64 blockedWaitTime)
    {
        this.SnapshotID = snapshotID;
        this.BlockedSessions = blockedSessions;
        this.BlockedWaitTime = blockedWaitTime;
        this.SnapshotDate = snapshotDate;
    }
}