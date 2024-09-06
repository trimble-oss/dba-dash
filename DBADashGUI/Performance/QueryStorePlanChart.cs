using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using DBADash.Messaging;
using DBADashGUI.Messaging;
using DBADashGUI.Theme;
using DocumentFormat.OpenXml.Office.PowerPoint.Y2021.M06.Main;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.VisualElements;
using LiveChartsCore.VisualElements;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using Padding = LiveChartsCore.Drawing.Padding;

namespace DBADashGUI.Performance
{
    public partial class QueryStorePlanChart : UserControl
    {
        public QueryStorePlanChart()
        {
            InitializeComponent();
        }

        private DateTimeOffset From;
        private DateTimeOffset To;
        private long QueryId;

        public async Task ShowChart(DBADashContext context, string db, long queryId, bool nearestInterval,
            DateTimeOffset from, DateTimeOffset to)
        {
            QueryId = queryId;
            From = from;
            To = to;
            Invoke(() =>
            {
                refresh1.ShowRefresh();
                planChart.Visible = false;
            });

            try
            {
                var message = new QueryStoreTopQueriesMessage
                {
                    CollectAgent = context.CollectAgent,
                    ImportAgent = context.ImportAgent,
                    Top = 1000,
                    DatabaseName = db,
                    SortColumn = "count_executions",
                    QueryID = queryId,
                    NearestInterval = nearestInterval,
                    GroupBy = QueryStoreTopQueriesMessage.QueryStoreGroupByEnum.date_bucket,
                    ConnectionID = context.ConnectionID,
                    From = from,
                    To = to,
                    IncludeWaits = false,
                    Lifetime = Config.DefaultCommandTimeout
                };
                await MessagingHelper.SendMessageAndProcessReply(message, context, (s, details, color) => { },
                    ProcessChart, Guid.NewGuid());
            }
            catch (Exception ex)
            {
                refresh1.SetFailed(ex.Message);
            }
        }

        private DataTable ChartData;
        private string MetricName => tsMeasure.Tag?.ToString();

        private Task ProcessChart(ResponseMessage reply, Guid messageGroup)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => ProcessChart(reply, messageGroup)));
                return Task.CompletedTask;
            }

            if (reply.Type == ResponseMessage.ResponseTypes.Success)
            {
                ChartData = reply.Data.Tables.Count == 0 ? new DataTable() : reply.Data.Tables[0];
                ProcessChart();
            }
            else
            {
                refresh1.SetFailed(reply.Message);
            }

            return Task.CompletedTask;
        }

        private void ProcessChart()
        {
            refresh1.HideRefresh();
            planChart.Visible = true;

            var series = GetSeries(ChartData, MetricName);
            var labelPaint = DBADashUser.SelectedTheme.ThemeIdentifier == ThemeType.Dark
                ? new SolidColorPaint(DashColors.White.ToSKColor())
                : new SolidColorPaint(DashColors.TrimbleBlueDark.ToSKColor());
            planChart.Title = new LabelVisual { Text = $"Query {QueryId} {tsMeasure.Text}", TextSize = 20, Paint = labelPaint };

            var formatString = To.Subtract(From).TotalMinutes < 1440 ? "HH:mm" : "MM/dd HH:mm";
            var unit = TimeSpan.FromMinutes(Convert.ToInt64(To.DateTime.RoundDownToPreviousHour().AddHours(1)
                .Subtract(From.DateTime.RoundDownToPreviousHour()).TotalMinutes / 20));
            planChart.XAxes = new Axis[]
            {
                new DateTimeAxis(unit, date => FormatDateForChartLabel(date, To.Subtract(From)))
                {
                    LabelsPaint = labelPaint,
                    MinLimit = To.Subtract(From).TotalMinutes > 60
                        ? From.ToAppTimeZone().DateTime.RoundDownToPreviousHour().Ticks
                        : From.ToAppTimeZone().Ticks,
                }
            };
            planChart.YAxes = new Axis[]
            {
                new() { LabelsPaint = labelPaint, MinLimit = 0, }
            };
            planChart.TooltipFindingStrategy = TooltipFindingStrategy.CompareAllTakeClosest;
            planChart.LegendPosition = LegendPosition.Right;
            planChart.LegendTextPaint = labelPaint;
            planChart.Tooltip = new CustomTooltip();
            planChart.Series = series;
        }

        //public event EventHandler<PlanSelectedEventArgs>PlanSelected;

        // Note: Selecting points doesn't work well when there are multiple points sharing the same x-axis value.  Removed.
        //private void OnPointerDown(IChartView chart, ChartPoint<PlanMetrics, SVGPathGeometry, LabelGeometry>? point)
        //{
        //    if (point?.Visual is null) return;

        //    foreach (var s in planChart.Series)
        //    {
        //        s.GeometrySvg = s.Name != null && s.Name.Contains("forced") ? SVGPoints.Star : SVGPoints.Circle;
        //    }

        //    point.Context.Series.GeometrySvg = SVGPoints.Pin;
        //    chart.Invalidate();
        //    tsViewPlan.Text = $@"View Plan {point.Model.PlanID}";
        //    tsViewPlan.Visible = true;
        //}

        public static string FormatDateForChartLabel(DateTime date, TimeSpan duration, CultureInfo cultureInfo = null)
        {
            cultureInfo ??= CultureInfo.CurrentCulture;

            switch (duration.TotalDays)
            {
                case < 1:
                    // Less than 1 day: Show time only
                    return date.ToString(cultureInfo.DateTimeFormat.ShortTimePattern, cultureInfo);

                case < 7:
                    {
                        // Less than 7 days: Show day and time
                        var dayName = date.ToString("ddd", cultureInfo); // Short day name
                        return $"{dayName} {date.ToString(cultureInfo.DateTimeFormat.ShortTimePattern, cultureInfo)}";
                    }
                default:
                    {
                        // More than 7 days but within the current year: Show day and month
                        var dateFormat = duration.TotalDays <= 365
                            ? cultureInfo.DateTimeFormat.ShortDatePattern.Replace("yyyy", "").Replace("yy", "")
                                .Trim(',', '/', '-', '.')
                            :
                            // Different years: Show the full date
                            cultureInfo.DateTimeFormat.ShortDatePattern;
                        return date.ToString(dateFormat, cultureInfo).Trim();
                    }
            }
        }

        private static ISeries[] GetSeries(DataTable dt, string metricName)
        {
            if (dt.Rows.Count == 0)
            {
                return Array.Empty<ISeries>();
            }

            var series = new List<ISeries>();
            ISeries currentSeries = null;
            long lastPlanId = -1;
            List<PlanMetrics> points = new();
            var maxExecutions = (long)dt.Rows.Cast<DataRow>().OrderByDescending(r => (long)r["count_executions"]).First()["count_executions"];
            var minExecutions = (long)dt.Rows.Cast<DataRow>().OrderBy(r => (long)r["count_executions"]).First()["count_executions"];
            minExecutions = minExecutions == 0 ? 1 : minExecutions; // Just in case to prevent divide by zero
            maxExecutions = maxExecutions == 0 ? 1 : maxExecutions; // Just in case to prevent divide by zero
            foreach (var row in dt.Rows.Cast<DataRow>().OrderBy(r => (long)r["plan_id"]))
            {
                var planId = (long)row["plan_id"];
                var planForcingType = row["plan_forcing_type_desc"].ToString();
                if (planId != lastPlanId)
                {
                    if (currentSeries != null)
                    {
                        currentSeries.Values = points;
                        series.Add(currentSeries);
                    }

                    points = new();
                    // Add fake data points to ensure geometry size is consistent between series
                    var fakeMax = dt.NewRow();
                    var fakeMin = dt.NewRow();
                    fakeMin["bucket_start"] = DateTime.MinValue;
                    fakeMin["bucket_end"] = DateTime.MinValue;
                    fakeMin["count_executions"] = minExecutions;
                    fakeMax["bucket_start"] = DateTime.MinValue;
                    fakeMax["bucket_end"] = DateTime.MinValue;
                    fakeMax["count_executions"] = maxExecutions;
                    points.Add(new PlanMetrics(fakeMin));
                    points.Add(new PlanMetrics(fakeMax));

                    var minGeometrySize = ((minExecutions / Convert.ToDouble(maxExecutions)) * 25) + 5; // based on how close the min executions are to the max executions, adjust the min geometry size.
                    currentSeries = new ScatterSeries<PlanMetrics, SVGPathGeometry>
                    {
                        Values = new ObservableCollection<PlanMetrics>(),
                        Mapping = (metric, value) => new(metric.BucketMidpoint.Ticks, metric.MetricValue(metricName), metric.ExecutionCount),
                        Name = $"Plan {planId}" + (planForcingType == "NONE" ? "" : " (Forced)"),
                        GeometrySvg = planForcingType == "NONE" ? SVGPoints.Circle : SVGPoints.Star,
                        MinGeometrySize = minGeometrySize,
                        GeometrySize = 30,
                    };

                    // ((ScatterSeries< PlanMetrics, SVGPathGeometry> )currentSeries).ChartPointPointerDown  += OnPointerDown;
                    lastPlanId = planId;
                }

                points.Add(new PlanMetrics(row));
            }

            if (currentSeries != null)
            {
                currentSeries.Values = points;
                series.Add(currentSeries);
            }

            return series.ToArray();
        }

        private void Select_Measure(object sender, EventArgs e)
        {
            tsMeasure.Tag = ((ToolStripMenuItem)sender).Tag;
            tsMeasure.Text = ((ToolStripMenuItem)sender).Text;
            ProcessChart();
        }

        private void SetYAxisMaxToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var maxValue = string.Empty;
            if (CommonShared.ShowInputDialog(ref maxValue, "Enter the maximum value for the Y Axis") !=
                DialogResult.OK) return;
            if (double.TryParse(maxValue, out var max))
            {
                planChart.YAxes.First().MaxLimit = max;
            }
        }

        private void SetYAxisMinToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var minValue = string.Empty;
            if (CommonShared.ShowInputDialog(ref minValue, "Enter the minimum value for the Y Axis") !=
                DialogResult.OK) return;
            if (double.TryParse(minValue, out var min))
            {
                planChart.YAxes.First().MinLimit = min;
            }
        }

        private void SaveChart(object sender, EventArgs e)
        {
            planChart.SaveChartAs($"Query_{QueryId}_Plans.png");
        }

        private void CopyImage(object sender, EventArgs e)
        {
            planChart.CopyImage();
        }

        private void CopyData(object sender, EventArgs e)
        {
            using var dgv = new DataGridView() { AutoGenerateColumns = true, AllowUserToAddRows = false };
            dgv.DataSource = ChartData;
            // Force the DataGridView to create columns and bind the data immediately
            dgv.BindingContext = new BindingContext();
            dgv.Refresh();

            Common.CopyDataGridViewToClipboard(dgv);
        }
    }

    //public class PlanSelectedEventArgs : EventArgs
    //{
    //    public long PlanID { get; }

    //    public string DB { get; }

    //    public PlanSelectedEventArgs(long planID,string db)
    //    {
    //        PlanID = planID;
    //        DB = db;
    //    }
    //}

    public class PlanMetrics
    {
        private readonly DataRow row;

        public PlanMetrics(DataRow _row) => row = _row;

        public DateTime BucketStart => ((DateTime)row["bucket_start"]).ToAppTimeZone();
        public DateTime BucketEnd => ((DateTime)row["bucket_end"]).ToAppTimeZone();

        public DateTime BucketMidpoint => GetMidpoint(BucketStart, BucketEnd);

        public long PlanID => Convert.ToInt64(row["plan_id"].DBNullToNull());
        public double TotalDuration => Convert.ToDouble(row["total_duration_ms"].DBNullToNull());
        public double TotalCPU => Convert.ToDouble(row["total_cpu_time_ms"].DBNullToNull());
        public double AverageCPU => Convert.ToDouble(row["avg_cpu_time_ms"].DBNullToNull());

        public double AverageDuration => Convert.ToDouble(row["avg_duration_ms"].DBNullToNull());

        public long ExecutionCount => Convert.ToInt64(row["count_executions"].DBNullToNull());

        public string PlanForcingType => row["plan_forcing_type_desc"].DBNullToNull().ToString();

        public double TotalPhysicalReadsKB => Convert.ToDouble(row["total_physical_io_reads_kb"].DBNullToNull());

        public double AveragePhysicalReadsKB => Convert.ToDouble(row["avg_physical_io_reads_kb"].DBNullToNull());

        public double MaxMemoryGrantKB => Convert.ToDouble(row["max_memory_grant_kb"].DBNullToNull());

        public long AbortCount => Convert.ToInt64(row["abort_count"].DBNullToNull());

        public double AbortPct =>
            ExecutionCount == 0 ? 0 : Convert.ToDouble(AbortCount) / Convert.ToDouble(ExecutionCount);

        public double ExceptionPct =>
            ExecutionCount == 0 ? 0 : Convert.ToDouble(ExceptionCount) / Convert.ToDouble(ExecutionCount);

        public long ExceptionCount => Convert.ToInt64(row["exception_count"].DBNullToNull());

        public double MetricValue(string metric) => Convert.ToDouble(row[metric].DBNullToNull());

        private static DateTime GetMidpoint(DateTime start, DateTime end)
        {
            // Ensure start is always before end
            if (start > end)
            {
                (start, end) = (end, start);
            }

            return start.AddTicks((end.Ticks - start.Ticks) / 2);
        }
    }

    //public class PlanMetrics
    //{
    //    public DateTime BucketStart { get; set; }
    //    public DateTime BucketEnd { get; set; }

    //    public long PlanID { get; set; }
    //    public double TotalDuration { get; set; }
    //    public double TotalCPU { get; set; }

    //    public double AverageCPU { get; set; }

    //    public double AverageDuration { get; set; }

    //    public double ExecutionCount { get; set; }

    //    public string PlanForcingType { get; set; }
    //}

    // Custom Tooltip to show plan metrics
    public class CustomTooltip : IChartTooltip<SkiaSharpDrawingContext>
    {
        private StackPanel<RoundedRectangleGeometry, SkiaSharpDrawingContext> _stackPanel;
        private static readonly int s_zIndex = 10100;
        private readonly SolidColorPaint _backgroundPaint = new(DashColors.TrimbleBlueDark.ToSKColor());
        private readonly SolidColorPaint _fontPaint = new(new SKColor(230, 230, 230)) { ZIndex = s_zIndex + 1 };

        public void Show(IEnumerable<ChartPoint> foundPoints, Chart<SkiaSharpDrawingContext> chart)
        {
            _stackPanel ??= new StackPanel<RoundedRectangleGeometry, SkiaSharpDrawingContext>
            {
                Padding = new Padding(25),
                Orientation = ContainerOrientation.Horizontal,
                HorizontalAlignment = Align.Start,
                VerticalAlignment = Align.Middle,
                BackgroundPaint = _backgroundPaint
            };

            // clear the previous elements.

            foreach (var child in _stackPanel.Children.ToArray())
            {
                _ = _stackPanel.Children.Remove(child);
                chart.RemoveVisual(child);
            }

            var chartPoints = foundPoints.ToList();
            foreach (var point in chartPoints)
            {
                var sketch = ((IChartSeries<SkiaSharpDrawingContext>)point.Context.Series).GetMiniaturesSketch();
                var relativePanel = sketch.AsDrawnControl(s_zIndex);
                var planMetrics = point.Context.DataSource as PlanMetrics;

                var table =
                    new TableLayout<RoundedRectangleGeometry, SkiaSharpDrawingContext>();

                table.AddChild(CreateLabel("Plan ID:"), 0, 0, Align.Start);
                table.AddChild(CreateLabel(planMetrics?.PlanID.ToString()), 0, 1, Align.End);
                table.AddChild(CreateLabel("Total Duration:"), 1, 0, Align.Start);
                table.AddChild(CreateLabel($"{planMetrics?.TotalDuration:N0}ms"), 1, 1, Align.End);
                table.AddChild(CreateLabel("Total CPU:"), 2, 0, Align.Start);
                table.AddChild(CreateLabel($"{planMetrics?.TotalCPU:N0}ms"), 2, 1, Align.End);
                table.AddChild(CreateLabel("Avg CPU:"), 3, 0, Align.Start);
                table.AddChild(CreateLabel($"{planMetrics?.AverageCPU:N1}ms"), 3, 1, Align.End);
                table.AddChild(CreateLabel("Avg Duration:"), 4, 0, Align.Start);
                table.AddChild(CreateLabel($"{planMetrics?.AverageDuration:N1}ms"), 4, 1, Align.End);
                table.AddChild(CreateLabel("Executions:"), 5, 0, Align.Start);
                table.AddChild(CreateLabel($"{planMetrics?.ExecutionCount:N0}"), 5, 1, Align.End);

                table.AddChild(CreateLabel("Start:"), 6, 0, Align.Start);
                table.AddChild(CreateLabel($"{planMetrics?.BucketStart}"), 6, 1, Align.End);
                table.AddChild(CreateLabel("End:"), 7, 0, Align.Start);
                table.AddChild(CreateLabel($"{planMetrics?.BucketEnd}"), 7, 1, Align.End);
                table.AddChild(CreateLabel("Plan Forcing:"), 8, 0, Align.Start);
                table.AddChild(CreateLabel($"{planMetrics?.PlanForcingType}"), 8, 1, Align.End);
                table.AddChild(CreateLabel("Total Physical Reads:"), 9, 0, Align.Start);
                table.AddChild(CreateLabel($"{planMetrics?.TotalPhysicalReadsKB:N0}KB"), 9, 1, Align.End);
                table.AddChild(CreateLabel("Avg Physical Reads:"), 10, 0, Align.Start);
                table.AddChild(CreateLabel($"{planMetrics?.AveragePhysicalReadsKB:N1}KB"), 10, 1, Align.End);
                table.AddChild(CreateLabel("Max Memory Grant:"), 11, 0, Align.Start);
                table.AddChild(CreateLabel($"{planMetrics?.MaxMemoryGrantKB:N0}KB"), 11, 1, Align.End);
                table.AddChild(CreateLabel("Aborts:"), 12, 0, Align.Start);
                table.AddChild(CreateLabel($"{planMetrics?.AbortCount:N0} ({planMetrics?.AbortPct:P1})"), 12, 1,
                    Align.End);
                table.AddChild(CreateLabel("Exceptions:"), 13, 0, Align.Start);
                table.AddChild(CreateLabel($"{planMetrics?.ExceptionCount:N0} ({planMetrics?.ExceptionPct:P1})"), 13, 1,
                    Align.End);

                var sp = new StackPanel<RoundedRectangleGeometry, SkiaSharpDrawingContext>
                {
                    Padding = new Padding(0, 4),
                    VerticalAlignment = Align.Middle,
                    HorizontalAlignment = Align.Middle,
                    Children =
                    {
                        relativePanel,
                        table
                    }
                };

                _stackPanel?.Children.Add(sp);
            }

            var size = _stackPanel!.Measure(chart);

            var location = chartPoints.GetTooltipLocation(size, chart);

            _stackPanel.X = location.X;
            _stackPanel.Y = location.Y;

            chart.AddVisual(_stackPanel);
        }

        private LabelVisual CreateLabel(string text)
        {
            return new LabelVisual
            {
                Text = text,
                Paint = _fontPaint,
                TextSize = 15,
                Padding = new Padding(8, 0, 0, 0),
                ClippingMode = ClipMode.None, // required on tooltips
                VerticalAlignment = Align.Start,
                HorizontalAlignment = Align.Start
            };
        }

        public void Hide(Chart<SkiaSharpDrawingContext> chart)
        {
            if (chart is null || _stackPanel is null) return;
            chart.RemoveVisual(_stackPanel);
        }
    }
}