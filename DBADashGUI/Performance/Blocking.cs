using DBADashGUI.Theme;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
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
        private long maxBlockedTime;
        private int databaseID;

        private List<DataRow> _rows;

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

            var fromTicks = DateRange.FromUTC.ToAppTimeZone().Ticks;
            var toTicks = Math.Min(DateHelper.AppNow.Ticks, DateRange.ToUTC.ToAppTimeZone().Ticks);

            // Create theme-aware paint for labels
            var labelPaint = CreateLabelPaint();

            // Configure axes first (even if no data) to show proper date labels
            chartBlocking.XAxes = new[]
            {
                new Axis
                {
                    Labeler = value => new DateTime((long)value).ToString(DateRange.DateFormatString),
                    MinLimit = fromTicks,
                    MaxLimit = toTicks,
                    LabelsPaint = labelPaint,
                    NamePaint = labelPaint
                }
            };

            chartBlocking.YAxes = new[]
            {
                new Axis
                {
                    Labeler = value => value.ToString("0"),
                    MinLimit = 0,
                    LabelsPaint = labelPaint,
                    NamePaint = labelPaint,
                    Name = "Blocked Sessions"
                }
            };

            if (dt.Rows.Count == 0)
            {
                chartBlocking.Series = Array.Empty<ISeries>();
                lblBlocking.Text = databaseID > 0 ? "Blocking: Database" : "Blocking: Instance";
                toolStrip1.Tag = databaseID > 0 ? "ALT" : null;
                toolStrip1.ApplyTheme(DBADashUser.SelectedTheme);
                return;
            }

            maxBlockedTime = 0;

            var rows = dt.Rows.Cast<DataRow>().ToList();
            _rows = rows;

            var points = rows
                .Select(r =>
                {
                    var dtm = ((DateTime)r["SnapshotDateUTC"]).ToAppTimeZone();
                    var blockedCnt = (int)r["BlockedSessionCount"];
                    var blockedTime = (long)r["BlockedWaitTime"];
                    var snapshotID = (int)r["BlockingSnapshotID"];

                    if (blockedTime > maxBlockedTime)
                    {
                        maxBlockedTime = blockedTime;
                    }

                    // chart model only holds X/Y; context goes on ChartPoint later
                    return new ObservablePoint
                    {
                        X = dtm.Ticks,
                        Y = blockedCnt
                    };
                })
                .ToArray();

            var scatter = new ScatterSeries<ObservablePoint>
            {
                Values = points,
                GeometrySize = MaxPointShapeDiameter,
                // Use YToolTipLabelFormatter and XToolTipLabelFormatter like in the docs
                XToolTipLabelFormatter = point =>
                {
                    // point.Model is ObservablePoint
                    var model = point.Model;
                    if (model == null || model.X == null) return string.Empty;

                    var snapshotDate = new DateTime((long)model.X).ToAppTimeZone();
                    return snapshotDate.ToString(DateRange.DateFormatString);
                },
                YToolTipLabelFormatter = point =>
                {
                    var index = point.Index;
                    if (index < 0 || index >= rows.Count) return string.Empty;

                    var row = rows[index];
                    var blockedCnt = (int)row["BlockedSessionCount"];
                    var blockedTime = (long)row["BlockedWaitTime"];

                    return $"{blockedCnt} sessions | {blockedTime}ms";
                }
            };

            chartBlocking.Series = new ISeries[] { scatter };

            // Update Y axis max based on actual data
            var yMax = Math.Max(100, rows.Max(r => (int)r["BlockedSessionCount"]));
            var yAxes = chartBlocking.YAxes.ToArray();
            yAxes[0].MaxLimit = yMax;
            chartBlocking.YAxes = yAxes;

            lblBlocking.Text = databaseID > 0 ? "Blocking: Database" : "Blocking: Instance";
            toolStrip1.Tag = databaseID > 0 ? "ALT" : null; // set tag to ALT to use the alternate menu renderer
            toolStrip1.ApplyTheme(DBADashUser.SelectedTheme);
        }

        private SolidColorPaint CreateLabelPaint()
        {
            return DBADashUser.SelectedTheme.ThemeIdentifier == ThemeType.Dark
                ? new SolidColorPaint(DashColors.White.ToSKColor())
                : new SolidColorPaint(DashColors.TrimbleBlueDark.ToSKColor());
        }

        private void Blocking_Load(object sender, EventArgs e)
        {
            chartBlocking.MouseDown += ChartBlocking_MouseDown;
        }

        private void TsClose_Click(object sender, EventArgs e)
        {
            Close?.Invoke(this, EventArgs.Empty);
        }

        private void TsUp_Click(object sender, EventArgs e)
        {
            MoveUp?.Invoke(this, EventArgs.Empty);
        }

        private void ChartBlocking_MouseDown(object sender, MouseEventArgs e)
        {
            if (_rows == null || _rows.Count == 0)
            {
                return;
            }

            // Ask LiveCharts which points are under the mouse
            var foundPoints = chartBlocking.GetPointsAt(
                new LiveChartsCore.Drawing.LvcPointD(e.Location.X, e.Location.Y));

            var firstPoint = foundPoints.FirstOrDefault();
            if (firstPoint is null)
            {
                return;
            }

            var index = firstPoint.Index;
            if (index < 0 || index >= _rows.Count)
            {
                return;
            }

            var row = _rows[index];
            var snapshotDateLocal = ((DateTime)row["SnapshotDateUTC"]).ToAppTimeZone();

            var frm = new RunningQueriesViewer
            {
                SnapshotDateFrom = snapshotDateLocal.AppTimeZoneToUtc(),
                SnapshotDateTo = snapshotDateLocal.AppTimeZoneToUtc(),
                InstanceID = InstanceID,
                ShowRootBlockers = true
            };
            frm.Show(this);
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