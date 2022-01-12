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
        DateTime eventTime = DateTime.MinValue;
        Int32 mins;

        string connectionString;
        Int32 InstanceID;
        DateTime fromDate;
        DateTime toDate;
        double maxBlockedTime = 0;
        Int32 databaseID = 0;

        public void RefreshData(Int32 InstanceID, DateTime fromDate, DateTime toDate, string connectionString, Int32 databaseID)
        {
            eventTime = DateTime.MinValue;
            mins = (Int32)toDate.Subtract(fromDate).TotalMinutes;
            this.InstanceID = InstanceID;
            this.fromDate = fromDate;
            this.toDate = toDate;
            this.connectionString = connectionString;
            this.databaseID = databaseID;
            refreshData(false);
        }

        public void RefreshData()
        {
            if (eventTime > DateTime.MinValue)
            {
                this.fromDate = eventTime.AddSeconds(1);
                this.toDate = DateTime.UtcNow.AddMinutes(1);
                refreshData(true);
            }
        }

        private DataTable getDT()
        {
            SqlConnection cn = new SqlConnection(connectionString);
            using (cn)
            {
                using (SqlCommand cmd = new SqlCommand("dbo.BlockingSnapshots_Get", cn) { CommandType = CommandType.StoredProcedure })
                {
                    cn.Open();
                    cmd.Parameters.AddWithValue("@InstanceID", InstanceID);
                    cmd.Parameters.AddWithValue("@FromDate", fromDate);
                    cmd.Parameters.AddWithValue("@ToDate", toDate);
                    if (databaseID > 0)
                    {
                        cmd.Parameters.AddWithValue("@DatabaseID", databaseID);
                    }
                    cmd.CommandTimeout = Properties.Settings.Default.CommandTimeout;
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
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

        private void refreshData(bool update)
        {
            var dt = getDT();
            if(!update)
            {
                chartBlocking.AxisX.Clear();
                chartBlocking.AxisY.Clear();
                chartBlocking.Series.Clear();
                eventTime = fromDate;
                maxBlockedTime = 0;
            }

            var points = new BlockingPoint[dt.Rows.Count];
            Int32 i=0;
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
                if (eventTime < dtm)
                {
                    eventTime = dtm;
                }
                points[i] = new BlockingPoint(snapshotID,dtm.ToLocalTime(), blockedCnt, blockedTime);
                Ymax = blockedCnt > Ymax ? blockedCnt : Ymax;
                i += 1;
            }

            if (update)
            {
                ScatterSeries ss = (ScatterSeries)chartBlocking.Series[0];
                ss.Values.AddRange(points);
                if (ss.Values.Count > 0)
                {
                    while (((BlockingPoint)ss.Values[0]).SnapshotDate < DateTime.UtcNow.AddMinutes(-mins))
                    {
                        ss.Values.RemoveAt(0);
                    }
                }
                Ymax = chartBlocking.AxisY[0].MaxValue > Ymax ? chartBlocking.AxisY[0].MaxValue : Ymax;
                chartBlocking.AxisX[0].MinValue = DateTime.Now.AddMinutes(-mins).Ticks;
                chartBlocking.AxisX[0].MaxValue = DateTime.Now.Ticks;
                chartBlocking.AxisY[0].MaxValue = Ymax;
                ss.MaxPointShapeDiameter = MaxPointShapeDiameter;
            }
            else
            {
                var mapper = Mappers.Weighted<BlockingPoint>()
                    .X(value => value.SnapshotDate.Ticks)
                    .Y(value => value.BlockedSessions)
                    .Weight(value => value.BlockedWaitTime);

                SeriesCollection s1 = new SeriesCollection(mapper)
                    {
                      new ScatterSeries
                      {
                        Title= "Blocking Snapshot",
                        Values = new ChartValues<BlockingPoint>(points),
                        MinPointShapeDiameter = 5,
                        MaxPointShapeDiameter = MaxPointShapeDiameter 
                   
                      }

                    };

                string format = mins < 1440 ? "HH:mm" : "yyyy-MM-dd HH:mm";
                chartBlocking.AxisX.Add(new Axis
                {
                    LabelFormatter = val => new System.DateTime((long)val).ToString(format),
                    MinValue = fromDate.ToLocalTime().Ticks,
                    MaxValue = toDate.ToLocalTime().Ticks
                });
                chartBlocking.AxisY.Add(new Axis
                {
                    Title="Blocked Sessions",
                    LabelFormatter = val => val.ToString(),
                    MinValue = 0,
                    MaxValue = Ymax
                });
                chartBlocking.Series = s1;

            }
            lblBlocking.Text = databaseID > 0 ? "Blocking: Database" : "Blocking: Instance";

        }
      

        private void ChartBlocking_DataClick(object sender, ChartPoint chartPoint)
        {
            var blockPoint = (BlockingPoint)chartPoint.Instance;
            RunningQueriesViewer frm = new RunningQueriesViewer
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