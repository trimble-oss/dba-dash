using DBADashGUI.Theme;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Wpf;
using Microsoft.Data.SqlClient;
using System;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    public partial class Blocking : UserControl, IMetricChart
    {
        public Blocking()
        {
            InitializeComponent();
        }

        private int InstanceID;
        private double maxBlockedTime;
        private int databaseID;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool CloseVisible
        {
            get => tsClose.Visible; set => tsClose.Visible = value;
        }

        public event EventHandler<EventArgs> Close;

        public event EventHandler<EventArgs> MoveUp;

        public void RefreshData(int InstanceID, int databaseID)
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
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.BlockingSnapshots_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
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

        private double MaxPointShapeDiameter => maxBlockedTime switch
        {
            > 3600000 => 60,
            > 600000 => 30,
            > 60000 => 10,
            _ => 5
        };

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool MoveUpVisible
        {
            get => tsUp.Visible; set => tsUp.Visible = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public BlockingMetric Metric { get; set; } = new();

        IMetric IMetricChart.Metric => Metric;

        public void RefreshData()
        {
            var dt = GetDT();

            chartBlocking.AxisX.Clear();
            chartBlocking.AxisY.Clear();
            chartBlocking.Series.Clear();
            maxBlockedTime = 0;

            var points = new BlockingPoint[dt.Rows.Count];
            var i = 0;
            double Ymax = 100;

            foreach (DataRow r in dt.Rows)
            {
                var dtm = (DateTime)r["SnapshotDateUTC"];
                var blockedCnt = (int)r["BlockedSessionCount"];
                var blockedTime = (long)r["BlockedWaitTime"];
                var snapshotID = (int)r["BlockingSnapshotID"];
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

            chartBlocking.AxisX.Add(new Axis
            {
                LabelFormatter = val => new DateTime((long)val).ToString(DateRange.DateFormatString),
                MinValue = DateRange.FromUTC.ToAppTimeZone().Ticks,
                MaxValue = Math.Min(DateHelper.AppNow.Ticks, DateRange.ToUTC.ToAppTimeZone().Ticks)
            });
            chartBlocking.AxisY.Add(new Axis
            {
                Title = "Blocked Sessions",
                LabelFormatter = val => val.ToString(CultureInfo.CurrentCulture),
                MinValue = 0,
                MaxValue = Ymax
            });
            chartBlocking.Series = s1;

            lblBlocking.Text = databaseID > 0 ? "Blocking: Database" : "Blocking: Instance";
            toolStrip1.Tag = databaseID > 0 ? "ALT" : null; // set tag to ALT to use the alternate menu renderer
            toolStrip1.ApplyTheme(DBADashUser.SelectedTheme);
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
            Close.Invoke(this, EventArgs.Empty);
        }

        private void TsUp_Click(object sender, EventArgs e)
        {
            MoveUp.Invoke(this, EventArgs.Empty);
        }
    }
}

internal class BlockingPoint
{
    public int SnapshotID { get; set; }

    public DateTime SnapshotDate { get; set; }

    public int BlockedSessions { get; set; }

    public long BlockedWaitTime { get; set; }

    public BlockingPoint(int snapshotID, DateTime snapshotDate, int blockedSessions, long blockedWaitTime)
    {
        SnapshotID = snapshotID;
        BlockedSessions = blockedSessions;
        BlockedWaitTime = blockedWaitTime;
        SnapshotDate = snapshotDate;
    }
}