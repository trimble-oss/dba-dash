using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static DBADashGUI.Performance.Performance;
using Microsoft.Data.SqlClient;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Defaults;
using LiveCharts.Configurations;

namespace DBADashGUI.Performance
{
    public partial class Blocking : UserControl
    {
        public Blocking()
        {
            InitializeComponent();
        }
  
        Int32 InstanceID;
        double maxBlockedTime = 0;
        Int32 databaseID = 0;

        public void RefreshData(Int32 InstanceID, Int32 databaseID)
        {
            this.InstanceID = InstanceID;
            this.databaseID = databaseID;
            RefreshData();
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
                if (databaseID > 0)
                {
                    cmd.Parameters.AddWithValue("@DatabaseID", databaseID);
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
                cmd.CommandTimeout = Properties.Settings.Default.CommandTimeout;
                
                DataTable dt = new();
                da.Fill(dt);
                return dt;
            }           
        }

        double MaxPointShapeDiameter
        {
            get
            {

                if (maxBlockedTime > 3600000)
                {
                    return 60;
                }
                else if (maxBlockedTime > 600000)
                {
                    return 30;
                }
                else if (maxBlockedTime > 60000)
                {
                    return 10;
                }
                else
                {
                    return 5;
                }
            }
        }

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
                points[i] = new BlockingPoint(snapshotID, dtm.ToLocalTime(), blockedCnt, blockedTime);
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
                MinValue = DateRange.FromUTC.ToLocalTime().Ticks,
                MaxValue = DateRange.ToUTC.ToLocalTime().Ticks
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

        }


        private void ChartBlocking_DataClick(object sender, ChartPoint chartPoint)
        {
            var blockPoint = (BlockingPoint)chartPoint.Instance;
            RunningQueriesViewer frm = new()
            {
                SnapshotDateFrom = blockPoint.SnapshotDate.ToUniversalTime(),
                SnapshotDateTo = blockPoint.SnapshotDate.ToUniversalTime(),
                InstanceID = InstanceID,
                ShowRootBlockers= true,
            };
            frm.Show(this);
            
        }

        private void Blocking_Load(object sender, EventArgs e)
        {
            chartBlocking.DataClick += ChartBlocking_DataClick;
        }
    }
}

class BlockingPoint 
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