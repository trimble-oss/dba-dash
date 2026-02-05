using DBADashGUI.Theme;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WinForms;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.Charts
{
    internal class ChartHelper
    {
        /// <summary>
        /// Safely creates a DateTimePoint from a DataRow, returning null if conversion fails
        /// </summary>
        private static DateTimePoint TryCreateDateTimePoint(DataRow row, string dateColumn, string metricColumn)
        {
            try
            {
                var dateValue = row[dateColumn];
                var metricValue = row[metricColumn];

                // Convert date using pattern matching
                if (!TryConvertToDateTime(dateValue, out var date))
                {
                    return null;
                }

                // Convert metric using pattern matching
                if (!TryConvertToDouble(metricValue, out var metric))
                {
                    return null;
                }

                return new DateTimePoint(date, metric);
            }
            catch
            {
                // Suppress conversion errors and return null
                return null;
            }
        }

        /// <summary>
        /// Converts various date types to DateTime using pattern matching
        /// </summary>
        private static bool TryConvertToDateTime(object value, out DateTime result)
        {
            result = default;

            if (value is null or DBNull)
                return false;

            // Use pattern matching for type conversion
            return value switch
            {
                DateTime dt => SetResult(dt, out result),
                DateTimeOffset dto => SetResult(dto.DateTime, out result),
                string str when DateTime.TryParse(str, out var parsed) => SetResult(parsed, out result),
                _ => false
            };
        }

        /// <summary>
        /// Converts various numeric types to double using pattern matching
        /// </summary>
        private static bool TryConvertToDouble(object value, out double result)
        {
            result = default;

            if (value is null or DBNull)
                return false;

            // Use pattern matching for efficient type conversion
            var success = value switch
            {
                double d => SetResult(d, out result),
                float f => SetResult(f, out result),
                decimal dec => SetResult((double)dec, out result),
                int i => SetResult(i, out result),
                long l => SetResult(l, out result),
                short s => SetResult(s, out result),
                byte b => SetResult(b, out result),
                uint ui => SetResult(ui, out result),
                ulong ul when ul <= long.MaxValue => SetResult(ul, out result),
                ushort us => SetResult(us, out result),
                sbyte sb => SetResult(sb, out result),
                string str when double.TryParse(str, out var parsed) => SetResult(parsed, out result),
                _ => false
            };

            // Validate the result is a valid number
            if (success && (double.IsNaN(result) || double.IsInfinity(result)))
            {
                result = default;
                return false;
            }

            return success;
        }

        /// <summary>
        /// Helper method to set result value in pattern matching expressions
        /// </summary>
        private static bool SetResult<T>(T value, out T result)
        {
            result = value;
            return true;
        }

        private static ISeries CreateSeriesForGroup(string groupName, DateTimePoint[] values, ChartTypes chartType)
        {
            ArgumentNullException.ThrowIfNull(values);
            if (values.Length == 0)
                throw new ArgumentException("Values array cannot be empty", nameof(values));

            if (string.IsNullOrWhiteSpace(groupName))
                groupName = "Unknown"; // Provide default instead of throwing

            switch (chartType)
            {
                case ChartTypes.StackedArea:
                    return new StackedAreaSeries<DateTimePoint>()
                    {
                        Name = groupName,
                        Values = values,
                        GeometrySize = 0 // Hide points for cleaner area chart
                    };

                case ChartTypes.StackedColumn:
                    return new StackedColumnSeries<DateTimePoint>()
                    {
                        Name = groupName,
                        Values = values
                    };

                case ChartTypes.Line:
                    return new LineSeries<DateTimePoint>()
                    {
                        Name = groupName,
                        Values = values,
                        GeometrySize = 0 // Hide points for cleaner line chart
                    };

                default:
                    throw new ArgumentOutOfRangeException(nameof(chartType), $"Unsupported chart type: {chartType}");
            }
        }

        /// <summary>
        /// Creates a CartesianChart from a DataTable using the provided configuration
        /// </summary>
        /// <param name="dt">DataTable containing the chart data</param>
        /// <param name="config">Chart configuration settings</param>
        /// <returns>A configured CartesianChart control</returns>
        internal static CartesianChart GetChartFromDataTable(DataTable dt, ChartConfiguration config)
        {
            ValidateConfiguration(dt, config);

            var labelPaint = CreateLabelPaint();
            var series = CreateSeriesFromDataTable(dt, config);

            // Only extract date range from series if not fully specified in config
            DateTime minDate, maxDate;
            if (config.XAxisMin.HasValue && config.XAxisMax.HasValue)
            {
                minDate = config.XAxisMin.Value;
                maxDate = config.XAxisMax.Value;
            }
            else
            {
                (minDate, maxDate) = GetDateRangeFromSeries(series);
                minDate = config.XAxisMin ?? minDate;
                maxDate = config.XAxisMax ?? maxDate;
            }

            var unit = CalculateDateUnit(minDate, maxDate);
            var xAxes = CreateXAxes(unit, labelPaint, minDate, maxDate);
            var yAxes = CreateYAxes(config, labelPaint);

            var chart = new CartesianChart
            {
                Series = series,
                XAxes = xAxes,
                YAxes = yAxes,
                Location = new System.Drawing.Point(0, 0),
                Dock = DockStyle.Fill
            };

            if (config.ShowLegend)
            {
                chart.LegendPosition = config.LegendPosition;
                chart.LegendTextPaint = labelPaint;
            }
            else
            {
                chart.LegendPosition = LiveChartsCore.Measure.LegendPosition.Hidden;
            }

            return chart;
        }

        private static void ValidateConfiguration(DataTable dt, ChartConfiguration config)
        {
            ArgumentNullException.ThrowIfNull(dt);
            ArgumentNullException.ThrowIfNull(config);

            // Validate input parameters
            if (string.IsNullOrWhiteSpace(config.DateColumn))
                throw new ArgumentException("Date column name cannot be null or empty", nameof(config.DateColumn));

            if (string.IsNullOrWhiteSpace(config.MetricColumn))
                throw new ArgumentException("Metric column name cannot be null or empty", nameof(config.MetricColumn));

            // Validate required columns exist
            if (!dt.Columns.Contains(config.DateColumn))
                throw new ArgumentException($"Column '{config.DateColumn}' not found in DataTable", nameof(config.DateColumn));

            if (!dt.Columns.Contains(config.MetricColumn))
                throw new ArgumentException($"Column '{config.MetricColumn}' not found in DataTable", nameof(config.MetricColumn));

            // Validate series column if specified
            if (!string.IsNullOrWhiteSpace(config.SeriesColumn) && !dt.Columns.Contains(config.SeriesColumn))
                throw new ArgumentException($"Column '{config.SeriesColumn}' not found in DataTable", nameof(config.SeriesColumn));

            // Validate y_min and y_max relationship
            if (config.YAxisMin.HasValue && config.YAxisMax.HasValue && config.YAxisMin.Value > config.YAxisMax.Value)
                throw new ArgumentException("Y-axis minimum cannot be greater than maximum");
        }

        private static List<ISeries> CreateSeriesFromDataTable(DataTable dt, ChartConfiguration config)
        {
            var series = new List<ISeries>();

            if (string.IsNullOrWhiteSpace(config.SeriesColumn))
            {
                // Single series logic
                var values = ExtractDataPoints(dt.AsEnumerable(), config.DateColumn, config.MetricColumn);
                if (values.Length > 0)
                    series.Add(CreateSeriesForGroup("Data", values, config.ChartType));
            }
            else
            {
                // Multi-series logic
                var groupedData = dt.Rows.Cast<DataRow>()
                    .Where(row => row[config.SeriesColumn] != null && row[config.SeriesColumn] != DBNull.Value)
                    .GroupBy(row => row[config.SeriesColumn].ToString());

                foreach (var group in groupedData)
                {
                    var values = ExtractDataPoints(group, config.DateColumn, config.MetricColumn);
                    if (values.Length > 0)
                        series.Add(CreateSeriesForGroup(group.Key, values, config.ChartType));
                }
            }

            return series;
        }

        private static DateTimePoint[] ExtractDataPoints(IEnumerable<DataRow> rows, string dateColumn, string metricColumn)
        {
            return rows
                .Where(row => row[dateColumn] != null && row[dateColumn] != DBNull.Value &&
                              row[metricColumn] != null && row[metricColumn] != DBNull.Value)
                .Select(row => TryCreateDateTimePoint(row, dateColumn, metricColumn))
                .Where(point => point != null)
                .OrderBy(point => point.DateTime)
                .ToArray();
        }

        private static SolidColorPaint CreateLabelPaint()
        {
            return DBADashUser.SelectedTheme.ThemeIdentifier == ThemeType.Dark
                ? new SolidColorPaint(DashColors.White.ToSKColor())
                : new SolidColorPaint(DashColors.TrimbleBlueDark.ToSKColor());
        }

        private static Axis[] CreateXAxes(TimeSpan unit, SolidColorPaint labelPaint, DateTime minDate, DateTime maxDate)
        {
            var duration = maxDate - minDate;
            return new Axis[]
            {
                new DateTimeAxis(unit, date => FormatDateForChartLabel(date, duration))
                {
                    LabelsPaint = labelPaint,
                    MinLimit = minDate.Ticks
                }
            };
        }

        private static Axis[] CreateYAxes(ChartConfiguration config, SolidColorPaint labelPaint)
        {
            var yAxis = new Axis
            {
                LabelsPaint = labelPaint,
                MinLimit = 0,
                Name = config.YAxisLabel,
                NamePaint = labelPaint
            };

            if (config.YAxisMax.HasValue)
                yAxis.MaxLimit = config.YAxisMax.Value;

            if (config.YAxisMin.HasValue)
                yAxis.MinLimit = config.YAxisMin.Value;

            if (!string.IsNullOrEmpty(config.YAxisFormat))
                yAxis.Labeler = value => value.ToString(config.YAxisFormat);

            return new Axis[] { yAxis };
        }

        /// <summary>
        /// Extracts the min and max dates from all series in the chart
        /// </summary>
        /// <param name="series"></param>
        /// <returns>Tuple containing min and max DateTime values</returns>
        private static (DateTime minDate, DateTime maxDate) GetDateRangeFromSeries(List<ISeries> series)
        {
            var allDates = series.SelectMany(s => ((IEnumerable<DateTimePoint>)s.Values).Select(p => p.DateTime)).ToList();
            if (allDates.Count > 0)
            {
                return (allDates.Min(), allDates.Max());
            }
            else
            {
                var now = DateTime.Now;
                return (now.AddMinutes(-1), now);
            }
        }

        /// <summary>
        /// Calculate date unit for x-axis based on the range of dates in the series and a target number of points to display
        /// </summary>
        /// <param name="series"></param>
        /// <returns></returns>
        private static TimeSpan CalculateDateUnit(DateTime minDate, DateTime maxDate)
        {
            const int points = 200;
            const int chartMinGrouping = 1; // Minimum grouping of 1 minute
            var durationMins = (int)(maxDate - minDate).TotalMinutes;
            return TimeSpan.FromMinutes(DateHelper.DateGrouping(durationMins, points, chartMinGrouping));
        }

        internal static bool IsColorsReady(CartesianChart chart)
        {
            var colors = ExtractColorsFromChart(chart);
            return colors.Values.All(color => color != SKColors.Gray);
        }

        internal static async Task WaitForColorsToBeReady(CartesianChart chart, CancellationToken cts)
        {
            const int maxWaitMs = 2000; // Maximum 2 seconds wait
            const int pollIntervalMs = 50;
            var elapsed = 0;

            while (!IsColorsReady(chart) && elapsed < maxWaitMs)
            {
                await Task.Delay(pollIntervalMs, cts);
                elapsed += pollIntervalMs;
            }
        }

        internal static Dictionary<string, SKColor> ExtractColorsFromChart(CartesianChart chart)
        {
            if (chart == null)
                throw new ArgumentNullException(nameof(chart), "Chart cannot be null");

            var seriesColors = new Dictionary<string, SKColor>();

            if (chart.Series == null)
                return seriesColors;

            foreach (var series in chart.Series)
            {
                if (series?.Name == null)
                    continue;

                SKColor color = SKColors.Gray;

                // Extract color based on series type
                if (series is StackedAreaSeries<DateTimePoint> areaSeries)
                {
                    if (areaSeries.Fill is SolidColorPaint fillPaint)
                    {
                        color = fillPaint.Color;
                    }
                }
                else if (series is StackedColumnSeries<DateTimePoint> columnSeries)
                {
                    if (columnSeries.Fill is SolidColorPaint fillPaint)
                    {
                        color = fillPaint.Color;
                    }
                }
                else if (series is LineSeries<DateTimePoint> lineSeries)
                {
                    if (lineSeries.Stroke is SolidColorPaint strokePaint)
                    {
                        color = strokePaint.Color;
                    }
                }

                seriesColors[series.Name] = color;
            }
            return seriesColors;
        }

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
    }
}