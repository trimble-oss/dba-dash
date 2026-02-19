using DBADash.Messaging;
using DBADashGUI.Messaging;
using DBADashGUI.Theme;
using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.VisualElements;
using SkiaSharp.Views.Desktop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static DBADashGUI.Messaging.MessagingHelper;
using Padding = LiveChartsCore.Drawing.Padding;

namespace DBADashGUI.Performance
{
    /// <summary>
    /// A Panel with double buffering enabled to reduce flicker during custom painting
    /// </summary>
    internal class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            DoubleBuffered = true;
        }
    }

    public partial class QueryStorePlanChart : UserControl
    {
        // Tooltip layout constants
        private const int TooltipLabelWidth = 25;

        private const int TooltipValueWidth = 20;
        private const int TooltipSeparatorWidth = 47;
        private const int TooltipIndicatorLeft = 35;
        private const int TooltipIndicatorTop = 5;
        private const int TooltipMouseOffset = 15;

        // Indicator drawing constants
        private const float IndicatorCenterX = 18f;

        private const float IndicatorCenterY = 15f;
        private const float IndicatorOuterRadius = 8f;
        private const float IndicatorInnerRadius = 3.5f;
        private const int IndicatorCircleSize = 16;
        private const int IndicatorCircleX = 10;
        private const int IndicatorCircleY = 7;

        private Panel tooltipPanel;
        private Label tooltipLabel;

        private DataTable chartData;
        private string MetricName => tsMeasure.Tag?.ToString();

        public QueryStorePlanChart()
        {
            InitializeComponent();
            InitializeCustomTooltip();

            // Setup custom tooltip events
            planChart.MouseMove += PlanChart_MouseMove;
            planChart.MouseLeave += PlanChart_MouseLeave;
        }

        private DateTimeOffset from;
        private DateTimeOffset to;
        private long queryId;

        private void InitializeCustomTooltip()
        {
            // Create custom tooltip panel with double buffering to reduce flicker
            tooltipPanel = new DoubleBufferedPanel
            {
                Visible = false,
                BackColor = Color.FromArgb(33, 66, 99),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new System.Windows.Forms.Padding(10),
                AutoSize = true,
                MaximumSize = new Size(500, 500)
            };

            tooltipLabel = new Label
            {
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Consolas", 10f), // Monospace for alignment
                BackColor = Color.Transparent,
                MaximumSize = new Size(480, 0),
                Location = new Point(TooltipIndicatorLeft, TooltipIndicatorTop) // Leave room for indicator
            };

            tooltipPanel.Controls.Add(tooltipLabel);
            Controls.Add(tooltipPanel);
            tooltipPanel.BringToFront();

            // Set up paint handler once during initialization
            tooltipPanel.Paint += TooltipPanel_Paint;
        }

        /// <summary>
        /// Displays the query store plan chart for the specified query.
        /// </summary>
        /// <param name="context">The database context</param>
        /// <param name="db">Database name</param>
        /// <param name="queryStoreQueryId">Query store query id</param>
        /// <param name="nearestInterval">Whether to use nearest interval</param>
        /// <param name="fromDate">Start date</param>
        /// <param name="toDate">End date</param>
        public async Task ShowChart(DBADashContext context, string db, long queryStoreQueryId, bool nearestInterval,
            DateTimeOffset fromDate, DateTimeOffset toDate)
        {
            queryId = queryStoreQueryId;
            from = fromDate;
            to = toDate;
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
                    ProcessChartResponse, Guid.NewGuid());
            }
            catch (Exception ex)
            {
                refresh1.SetFailed(ex.Message);
            }
        }

        private Task ProcessChartResponse(ResponseMessage reply, Guid messageGroup, SetStatusDelegate setStatus)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => ProcessChartResponse(reply, messageGroup, setStatus)));
                return Task.CompletedTask;
            }

            if (reply.Type == ResponseMessage.ResponseTypes.Success)
            {
                chartData = reply.Data.Tables.Count == 0 ? new DataTable() : reply.Data.Tables[0];
                RenderChart();
            }
            else
            {
                refresh1.SetFailed(reply.Message);
            }

            return Task.CompletedTask;
        }

        private void RenderChart()
        {
            refresh1.HideRefresh();
            planChart.Visible = true;

            var series = GetSeries(chartData, MetricName);
            var labelPaint = DBADashUser.SelectedTheme.ThemeIdentifier == ThemeType.Dark
                ? new SolidColorPaint(DashColors.White.ToSKColor())
                : new SolidColorPaint(DashColors.TrimbleBlueDark.ToSKColor());

            var title = new DrawnLabelVisual(
            new LabelGeometry
            {
                Text = $"Query {queryId} {tsMeasure.Text}",
                Paint = labelPaint,
                TextSize = 20,
                Padding = new Padding(0, 30, 0, 0)
            });
            planChart.Title = title;

            var unit = TimeSpan.FromMinutes(Convert.ToInt64(to.DateTime.RoundDownToPreviousHour().AddHours(1)
                .Subtract(from.DateTime.RoundDownToPreviousHour()).TotalMinutes / 20));
            var labelFontSize = DBADashUser.ChartAxisLabelFontSize;
            var nameFontSize = DBADashUser.ChartAxisNameFontSize;

            planChart.XAxes =
            [
                new DateTimeAxis(unit, date => Charts.ChartHelper.FormatDateForChartLabel(date, to.Subtract(from)))
                {
                    LabelsPaint = labelPaint,
                    TextSize = labelFontSize,
                    NamePaint = labelPaint,
                    NameTextSize = nameFontSize,
                    MinLimit = to.Subtract(from).TotalMinutes > 60
                        ? from.ToAppTimeZone().DateTime.RoundDownToPreviousHour().Ticks
                        : from.ToAppTimeZone().Ticks,
                }
            ];
            planChart.YAxes = new Axis[]
            {
                new() 
                { 
                    LabelsPaint = labelPaint, 
                    TextSize = labelFontSize,
                    NamePaint = labelPaint,
                    NameTextSize = nameFontSize,
                    MinLimit = 0, 
                }
            };
            planChart.LegendPosition = LegendPosition.Right;
            planChart.LegendTextPaint = labelPaint;
            planChart.LegendTextSize = labelFontSize;
            // Disable default tooltip - we'll use our custom one
            planChart.TooltipPosition = TooltipPosition.Hidden;
            planChart.Series = series;
        }

        private void PlanChart_MouseMove(object sender, MouseEventArgs e)
        {
            if (planChart.Series == null) return;

            var chartPoint = planChart.GetPointsAt(new LvcPointD(e.X, e.Y), FindingStrategy.CompareAllTakeClosest).FirstOrDefault();

            if (chartPoint != null)
            {
                if (chartPoint.Context.DataSource is PlanMetrics metric)
                {
                    var series = chartPoint.Context.Series;
                    var seriesColor = GetSeriesColor(series);
                    var isStarSeries = series?.Name?.Contains("(Forced)") ?? false;
                    // Convert mouse location from planChart coordinates to UserControl coordinates
                    var locationInUserControl = planChart.PointToScreen(e.Location);
                    locationInUserControl = PointToClient(locationInUserControl);
                    ShowCustomTooltip(metric, locationInUserControl, seriesColor, isStarSeries);
                    return;
                }
            }

            HideCustomTooltip();
        }

        private static Color GetSeriesColor(ISeries series)
        {
            SolidColorPaint paint = series switch
            {
                ScatterSeries<PlanMetrics, CircleGeometry> circleSeries => circleSeries.Fill as SolidColorPaint,
                ScatterSeries<PlanMetrics, StarGeometry> starSeries => starSeries.Fill as SolidColorPaint,
                _ => null
            };

            if (paint != null)
            {
                var skColor = paint.Color;
                return Color.FromArgb(skColor.Alpha, skColor.Red, skColor.Green, skColor.Blue);
            }

            return Color.DeepSkyBlue;
        }

        private void PlanChart_MouseLeave(object sender, EventArgs e) => HideCustomTooltip();

        private Color currentSeriesColor = Color.DeepSkyBlue;
        private bool currentIsStarSeries = false;
        private PlanMetrics currentMetric = null;
        private Point lastTooltipPosition;

        private void ShowCustomTooltip(PlanMetrics metric, Point mouseLocation, Color seriesColor, bool isStarSeries)
        {
            if (metric == null || tooltipPanel == null || tooltipLabel == null) return;

            // Check if we're hovering over the same data point (hot path optimization)
            // Use Ticks comparison for DateTime values - faster than DateTime comparison
            var metricChanged = currentMetric == null ||
                                currentMetric.PlanID != metric.PlanID ||
                                currentMetric.BucketStart.Ticks != metric.BucketStart.Ticks ||
                                currentMetric.BucketEnd.Ticks != metric.BucketEnd.Ticks;

            // Cache color and star status for paint handler
            var colorChanged = currentSeriesColor != seriesColor || currentIsStarSeries != isStarSeries;

            // Only rebuild tooltip content if the data point changed
            if (metricChanged)
            {
                currentMetric = metric;
                currentSeriesColor = seriesColor;
                currentIsStarSeries = isStarSeries;

                // Format with proper alignment using composite formatting
                // Use consistent widths: 25 chars for labels, 20 chars right-aligned for values
                var startDate = metric.BucketStart.ToString("yyyy-MM-dd HH:mm:ss");
                var endDate = metric.BucketEnd.ToString("yyyy-MM-dd HH:mm:ss");

                // Use StringBuilder for performance in this hot path (MouseMove event)
                var sb = new StringBuilder(600);
                sb.AppendLine($"{"Plan ID:",-TooltipLabelWidth} {metric.PlanID,TooltipValueWidth}");
                sb.AppendLine(new string('─', TooltipSeparatorWidth));
                sb.AppendLine($"{"Total Duration:",-TooltipLabelWidth} {$"{metric.TotalDuration:N0} ms",TooltipValueWidth}");
                sb.AppendLine($"{"Total CPU:",-TooltipLabelWidth} {$"{metric.TotalCPU:N0} ms",TooltipValueWidth}");
                sb.AppendLine($"{"Avg CPU:",-TooltipLabelWidth} {$"{metric.AverageCPU:N1} ms",TooltipValueWidth}");
                sb.AppendLine($"{"Avg Duration:",-TooltipLabelWidth} {$"{metric.AverageDuration:N1} ms",TooltipValueWidth}");
                sb.AppendLine($"{"Executions:",-TooltipLabelWidth} {metric.ExecutionCount,TooltipValueWidth:N0}");
                sb.AppendLine($"{"Start:",-TooltipLabelWidth} {startDate,TooltipValueWidth}");
                sb.AppendLine($"{"End:",-TooltipLabelWidth} {endDate,TooltipValueWidth}");
                sb.AppendLine($"{"Plan Forcing:",-TooltipLabelWidth} {metric.PlanForcingType,TooltipValueWidth}");
                sb.AppendLine($"{"Total Physical Reads:",-TooltipLabelWidth} {$"{metric.TotalPhysicalReadsKB:N0} KB",TooltipValueWidth}");
                sb.AppendLine($"{"Avg Physical Reads:",-TooltipLabelWidth} {$"{metric.AveragePhysicalReadsKB:N1} KB",TooltipValueWidth}");
                sb.AppendLine($"{"Max Memory Grant:",-TooltipLabelWidth} {$"{metric.MaxMemoryGrantKB:N0} KB",TooltipValueWidth}");
                sb.AppendLine($"{"Aborts:",-TooltipLabelWidth} {$"{metric.AbortCount:N0} ({metric.AbortPct:P1})",TooltipValueWidth}");
                sb.Append($"{"Exceptions:",-TooltipLabelWidth} {$"{metric.ExceptionCount:N0} ({metric.ExceptionPct:P1})",TooltipValueWidth}");

                tooltipLabel.Text = sb.ToString();
            }
            else if (colorChanged)
            {
                // Update color tracking even if metric didn't change
                currentSeriesColor = seriesColor;
                currentIsStarSeries = isStarSeries;
            }

            // Update position only when switching to a new data point
            // This keeps the tooltip stable while hovering over the same point
            if (metricChanged)
            {
                var newPosition = CalculateTooltipPosition(mouseLocation, tooltipPanel.Size, ClientRectangle);
                tooltipPanel.Location = newPosition;
                lastTooltipPosition = newPosition;
            }

            if (!tooltipPanel.Visible)
            {
                tooltipPanel.Visible = true;
            }
            else if (colorChanged)
            {
                // Only invalidate the indicator area if the color changed
                tooltipPanel.Invalidate(new Rectangle(0, 0, 35, 30));
            }
        }

        private void TooltipPanel_Paint(object sender, PaintEventArgs e) =>
            DrawColorIndicator(e.Graphics, currentSeriesColor, currentIsStarSeries);

        /// <summary>
        /// Calculates the optimal tooltip position ensuring it stays within chart bounds
        /// </summary>
        private Point CalculateTooltipPosition(Point mouseLocation, Size tooltipSize, Rectangle chartBounds)
        {
            var x = mouseLocation.X + TooltipMouseOffset;
            var y = mouseLocation.Y + TooltipMouseOffset;

            // Adjust for right/bottom edges - position on opposite side of cursor if needed
            if (x + tooltipSize.Width > chartBounds.Width)
                x = mouseLocation.X - tooltipSize.Width - TooltipMouseOffset;
            if (y + tooltipSize.Height > chartBounds.Height)
                y = mouseLocation.Y - tooltipSize.Height - TooltipMouseOffset;

            // Clamp to bounds to ensure tooltip never goes off screen
            return new Point(
                Math.Clamp(x, 0, Math.Max(0, chartBounds.Width - tooltipSize.Width)),
                Math.Clamp(y, 0, Math.Max(0, chartBounds.Height - tooltipSize.Height))
            );
        }

        private static void DrawColorIndicator(Graphics g, Color seriesColor, bool isStarSeries)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var brush = new SolidBrush(seriesColor))
            {
                if (isStarSeries)
                {
                    // Draw star
                    var points = new PointF[10];
                    for (int i = 0; i < 10; i++)
                    {
                        var angle = (float)(Math.PI / 5 * i - Math.PI / 2);
                        var radius = i % 2 == 0 ? IndicatorOuterRadius : IndicatorInnerRadius;
                        points[i] = new PointF(
                            IndicatorCenterX + radius * (float)Math.Cos(angle),
                            IndicatorCenterY + radius * (float)Math.Sin(angle)
                        );
                    }
                    g.FillPolygon(brush, points);
                }
                else
                {
                    // Draw circle
                    g.FillEllipse(brush, IndicatorCircleX, IndicatorCircleY, IndicatorCircleSize, IndicatorCircleSize);
                }
            }
        }

        private void HideCustomTooltip()
        {
            tooltipPanel.Visible = false;
            currentMetric = null; // Clear cached metric when hiding tooltip
        }

        private static ISeries[] GetSeries(DataTable dt, string metricName)
        {
            if (dt.Rows.Count == 0)
            {
                return [];
            }

            var series = new List<ISeries>();
            ISeries currentSeries = null;
            long lastPlanId = -1;
            List<PlanMetrics> points = new();

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

                    // Choose the appropriate series type based on the forcing type
                    currentSeries = CreatePlanSeries(planId, planForcingType, metricName);

                    lastPlanId = planId;
                }

                points.Add(new PlanMetrics(row));
            }

            if (currentSeries != null)
            {
                currentSeries.Values = points;
                series.Add(currentSeries);
            }

            return [.. series];
        }

        /// <summary>
        /// Creates a scatter series for plan metrics with appropriate geometry based on forcing type
        /// </summary>
        private static ISeries CreatePlanSeries(long planId, string planForcingType, string metricName)
        {
            var isForced = planForcingType != "NONE";

            if (isForced)
            {
                return CreateScatterSeries<StarGeometry>($"Plan {planId} (Forced)", metricName);
            }
            else
            {
                return CreateScatterSeries<CircleGeometry>($"Plan {planId}", metricName);
            }
        }

        /// <summary>
        /// Creates a scatter series with the specified geometry type and common configuration
        /// </summary>
        private static ScatterSeries<PlanMetrics, TGeometry> CreateScatterSeries<TGeometry>(string seriesName, string metricName)
            where TGeometry : BoundedDrawnGeometry, new()
        {
            return new ScatterSeries<PlanMetrics, TGeometry>
            {
                Values = new ObservableCollection<PlanMetrics>(),
                Mapping = (metric, value) => new Coordinate(
                    metric.BucketMidpoint.Ticks,
                    metric.MetricValue(metricName),
                    metric.ExecutionCount
                ),
                Name = seriesName,
                MinGeometrySize = 5,
                GeometrySize = 30,
                StackGroup = 1,
            };
        }

        private void Select_Measure(object sender, EventArgs e)
        {
            tsMeasure.Tag = ((ToolStripMenuItem)sender).Tag;
            tsMeasure.Text = ((ToolStripMenuItem)sender).Text;
            RenderChart();
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
            planChart.SaveChartAs($"Query_{queryId}_Plans.png");
        }

        private void CopyImage(object sender, EventArgs e)
        {
            planChart.CopyImage();
        }

        private void CopyData(object sender, EventArgs e)
        {
            using var dgv = new DataGridView() { AutoGenerateColumns = true, AllowUserToAddRows = false };
            dgv.DataSource = chartData;
            // Force the DataGridView to create columns and bind the data immediately
            dgv.BindingContext = new BindingContext();
            dgv.Refresh();

            Common.CopyDataGridViewToClipboard(dgv);
        }
    }

    /// <summary>
    /// Represents query store plan metrics mapped from a single <see cref="DataRow"/>.
    /// Used as a data model for charting and analyzing query store plan performance.
    /// </summary>
    /// <param name="row">The data row containing query store plan metric values.</param>
    public class PlanMetrics(DataRow row)
    {
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
}