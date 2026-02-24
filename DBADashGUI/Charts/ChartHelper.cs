using DBADashGUI.Theme;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WinForms;
using SkiaSharp;
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
        private enum XAxisKind
        {
            DateTime,
            Numeric,
            Category
        }

        /// <summary>
        /// Returns either a Cartesian chart or a Pie chart control depending on the configuration.
        /// </summary>
        internal static Control GetChartControlFromDataTable(DataTable dt, ChartConfigurationBase config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (config.ChartType == ChartTypes.Pie)
            {
                if (config is PieChartConfiguration pieConfig)
                {
                    return GetPieChartFromDataTable(dt, pieConfig);
                }
                throw new ArgumentException("Pie charts require a PieChartConfiguration instance", nameof(config));
            }

            // For non-pie chart types we require a full ChartConfiguration instance
            if (config is ChartConfiguration cartesianConfig)
            {
                return GetChartFromDataTable(dt, cartesianConfig);
            }
            throw new ArgumentException("Non-pie charts require a ChartConfiguration instance", nameof(config));
        }

        /// <summary>
        /// Creates a PieChart (WinForms) from a DataTable using pie-specific configuration.
        /// Supports either MetricColumns (each column becomes a slice summed across rows)
        /// or CategoryColumn+ValueColumn (group by category and sum values).
        /// </summary>
        private static Control GetPieChartFromDataTable(DataTable dt, PieChartConfiguration config)
        {
            ArgumentNullException.ThrowIfNull(dt);
            ArgumentNullException.ThrowIfNull(config);

            // High-level validation
            config.Validate();

            var labelPaint = CreateLabelPaint();

            // Build raw slices as (name, value) pairs first
            var slices = new List<(string name, double value)>();

            // MetricColumns mode: each metric column becomes one slice (aggregated across rows)
            if (config.MetricColumns != null && config.MetricColumns.Length > 0)
            {
                foreach (var metricCol in config.MetricColumns)
                {
                    if (!dt.Columns.Contains(metricCol))
                        continue;

                    double sum = 0;
                    foreach (DataRow r in dt.Rows)
                    {
                        var val = r[metricCol];
                        if (TryConvertToDouble(val, out var d))
                        {
                            sum += d;
                        }
                    }

                    if (sum > 0)
                    {
                        var name = GetFriendlyColumnName(metricCol, config);
                        slices.Add((name, sum));
                    }
                }
            }
            else
            {
                // Category + value mode
                if (!dt.Columns.Contains(config.CategoryColumn) || !dt.Columns.Contains(config.ValueColumn))
                    throw new ArgumentException("CategoryColumn or ValueColumn not found in DataTable", nameof(config));

                var groups = dt.Rows.Cast<DataRow>()
                    .Where(r => r[config.CategoryColumn] != null && r[config.CategoryColumn] != DBNull.Value)
                    .GroupBy(r => r[config.CategoryColumn].ToString());

                foreach (var g in groups)
                {
                    double sum = 0;
                    foreach (var r in g)
                    {
                        var v = r[config.ValueColumn];
                        if (TryConvertToDouble(v, out var d))
                        {
                            sum += d;
                        }
                    }
                    if (sum > 0)
                    {
                        slices.Add((g.Key, sum));
                    }
                }
            }

            // Apply "Other" grouping if requested
            var total = slices.Sum(s => s.value);
            if (config.MinSlicePercent > 0 && total > 0)
            {
                var thresholdFraction = config.MinSlicePercent / 100.0;
                var small = slices.Where(s => (s.value / total) < thresholdFraction).ToList();
                if (small.Any())
                {
                    var otherSum = small.Sum(s => s.value);
                    slices = slices.Where(s => (s.value / total) >= thresholdFraction).ToList();
                    slices.Add((config.OtherLabel ?? "Other", otherSum));
                }
            }

            // Sort slices descending by value for consistent display
            slices = slices.OrderByDescending(s => s.value).ToList();

            // Convert to ISeries list. Don't attempt to compute pixel sizes yet - chart size is needed.
            var series = slices.Select(s => (ISeries)new PieSeries<ObservableValue>
            {
                Name = s.name,
                Values = new ObservableValue[] { new ObservableValue(s.value) },
                // initialize to unlimited; will be computed once the chart control has a size
                MaxRadialColumnWidth = double.MaxValue
            }).ToList();

            var chart = new PieChart
            {
                Location = new System.Drawing.Point(0, 0),
                Dock = DockStyle.Fill,
                Series = series
            };

            // If an InnerRadius fraction (0..1) is configured, create a manager to compute
            // the pixel thickness for the donut and apply it to each PieSeries. The manager
            // listens for size changes and disposes itself when the control is disposed.
            if (config.InnerRadius.HasValue)
            {
                _ = new PieDonutManager(chart, series, config.InnerRadius.Value);
            }

            chart.LegendPosition = config.LegendPosition;
            chart.LegendTextPaint = labelPaint;
            chart.LegendTextSize = DBADashUser.ChartAxisLabelFontSize;

            // PieChart currently doesn't use the Cartesian custom tooltip helper

            return chart;
        }

        // Keep for backward compatibility in case other code references it in the future
        // Currently not used; we rely on config.XColumn being set by the caller.

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

        private static ISeries CreateSeriesForGroup(string groupName, ObservablePoint[] values, ChartTypes chartType, double lineSmoothness = 0, double geometrySize = 0, bool lineFill = false)
        {
            ArgumentNullException.ThrowIfNull(values);
            if (values.Length == 0)
                throw new ArgumentException("Values array cannot be empty", nameof(values));

            if (string.IsNullOrWhiteSpace(groupName))
                groupName = "Unknown"; // Provide default instead of throwing

            switch (chartType)
            {
                case ChartTypes.StackedArea:
                    return new StackedAreaSeries<ObservablePoint>()
                    {
                        Name = groupName,
                        Values = values,
                        GeometrySize = geometrySize,
                        LineSmoothness = lineSmoothness
                    };

                case ChartTypes.StackedColumn:
                    return new StackedColumnSeries<ObservablePoint>()
                    {
                        Name = groupName,
                        Values = values
                    };

                case ChartTypes.Column:
                    return new ColumnSeries<ObservablePoint>()
                    {
                        Name = groupName,
                        Values = values
                    };

                case ChartTypes.Line:
                    var lineSeries = new LineSeries<ObservablePoint>()
                    {
                        Name = groupName,
                        Values = values,
                        GeometrySize = geometrySize,
                        LineSmoothness = lineSmoothness
                    };

                    // Only set Fill to null if lineFill is false (default behavior)
                    if (!lineFill)
                    {
                        lineSeries.Fill = null;
                    }

                    return lineSeries;

                default:
                    throw new ArgumentOutOfRangeException(nameof(chartType), $"Unsupported chart type: {chartType}");
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

        private static ISeries CreateSeriesForGroup(string groupName, DateTimePoint[] values, ChartTypes chartType, double lineSmoothness = 0, double geometrySize = 0, bool lineFill = false)
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
                        GeometrySize = geometrySize,
                        LineSmoothness = lineSmoothness
                    };

                case ChartTypes.StackedColumn:
                    return new StackedColumnSeries<DateTimePoint>()
                    {
                        Name = groupName,
                        Values = values
                    };

                case ChartTypes.Column:
                    return new ColumnSeries<DateTimePoint>()
                    {
                        Name = groupName,
                        Values = values
                    };

                case ChartTypes.Line:
                    var lineSeries = new LineSeries<DateTimePoint>()
                    {
                        Name = groupName,
                        Values = values,
                        GeometrySize = geometrySize,
                        LineSmoothness = lineSmoothness
                    };

                    // Only set Fill to null if lineFill is false (default behavior)
                    if (!lineFill)
                    {
                        lineSeries.Fill = null;
                    }

                    return lineSeries;

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
            var chart = new CartesianChart
            {
                Location = new System.Drawing.Point(0, 0),
                Dock = DockStyle.Fill
            };

            ConfigureChart(chart, dt, config);

            // Enable custom tooltips to prevent truncation at chart boundaries
            chart.EnableCustomTooltips();

            return chart;
        }

        /// <summary>
        /// Updates an existing CartesianChart with new data from a DataTable
        /// </summary>
        /// <param name="chart">The existing CartesianChart to update</param>
        /// <param name="dt">DataTable containing the chart data</param>
        /// <param name="config">Chart configuration settings</param>
        internal static void UpdateChart(CartesianChart chart, DataTable dt, ChartConfiguration config)
        {
            ArgumentNullException.ThrowIfNull(chart);
            ConfigureChart(chart, dt, config);

            // Ensure custom tooltips are enabled
            chart.EnableCustomTooltips();
        }

        /// <summary>
        /// Configures a CartesianChart with data and settings
        /// </summary>
        private static void ConfigureChart(CartesianChart chart, DataTable dt, ChartConfiguration config)
        {
            ValidateConfiguration(dt, config);

            var labelPaint = CreateLabelPaint();
            var series = CreateSeriesFromDataTable(dt, config, out var categories);

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

            // Determine X axis kind to create appropriate axes
            var xKind = DetectXAxisKind(dt, config.XColumn);
            Axis[] xAxes;
            if (xKind == XAxisKind.DateTime)
            {
                var unit = CalculateDateUnit(minDate, maxDate, config.ChartType, series, config);
                xAxes = CreateXAxes(unit, labelPaint, minDate, maxDate, config.XAxisLabel);
            }
            else if (xKind == XAxisKind.Numeric)
            {
                var xAxis = new Axis
                {
                    LabelsPaint = labelPaint,
                    TextSize = DBADashUser.ChartAxisLabelFontSize,
                    Name = config.XAxisLabel
                };
                xAxes = new Axis[] { xAxis };
            }
            else
            {
                // Category axis: set labels from extracted categories if available
                var xAxis = new Axis
                {
                    LabelsPaint = labelPaint,
                    TextSize = DBADashUser.ChartAxisLabelFontSize,
                    Labels = categories ?? Array.Empty<string>(),
                    Name = config.XAxisLabel
                };
                xAxes = new Axis[] { xAxis };
            }

            var yAxes = CreateYAxes(config, labelPaint);

            chart.Series = series;
            chart.XAxes = xAxes;
            chart.YAxes = yAxes;

            // Assign series to Y-axes
            // Priority: ColumnAxisNames (name-based) > MetricColumnAxisMap (index-based)
            if (config.ColumnAxisNames != null && config.MetricColumns != null)
            {
                AssignSeriesToYAxesByName(series, config, yAxes);
            }
            else if (config.MetricColumnAxisMap != null && config.MetricColumns != null)
            {
                AssignSeriesToYAxes(series, config);
            }

            chart.LegendPosition = config.LegendPosition;
            chart.LegendTextPaint = labelPaint;
            chart.LegendTextSize = DBADashUser.ChartAxisLabelFontSize;
        }

        private static void ValidateConfiguration(DataTable dt, ChartConfiguration config)
        {
            ArgumentNullException.ThrowIfNull(dt);
            ArgumentNullException.ThrowIfNull(config);

            // Validate high-level configuration rules first
            config.Validate();

            // X column is specified in configuration as XColumn
            var xCol = config.XColumn;
            // Validate input parameters
            if (string.IsNullOrWhiteSpace(xCol))
                throw new ArgumentException("X column name cannot be null or empty", nameof(config.XColumn));

            // Validate that either MetricColumn or MetricColumns is specified, but not both
            var hasMetricColumn = !string.IsNullOrWhiteSpace(config.MetricColumn);
            var hasMetricColumns = config.MetricColumns != null && config.MetricColumns.Length > 0;

            if (!hasMetricColumn && !hasMetricColumns)
                throw new ArgumentException("Either MetricColumn or MetricColumns must be specified", nameof(config.MetricColumn));

            if (hasMetricColumn && hasMetricColumns)
                throw new ArgumentException("Cannot specify both MetricColumn and MetricColumns", nameof(config.MetricColumn));

            // Validate that SeriesColumn is only used with MetricColumn
            if (hasMetricColumns && !string.IsNullOrWhiteSpace(config.SeriesColumn))
                throw new ArgumentException("SeriesColumn cannot be used with MetricColumns", nameof(config.SeriesColumn));

            // Validate required columns exist
            if (!dt.Columns.Contains(xCol))
                throw new ArgumentException($"Column '{xCol}' not found in DataTable", nameof(config));

            if (hasMetricColumn && !dt.Columns.Contains(config.MetricColumn))
                throw new ArgumentException($"Column '{config.MetricColumn}' not found in DataTable", nameof(config.MetricColumn));

            if (hasMetricColumns)
            {
                foreach (var metricCol in config.MetricColumns)
                {
                    if (!dt.Columns.Contains(metricCol))
                        throw new ArgumentException($"Column '{metricCol}' not found in DataTable", nameof(config.MetricColumns));
                }
            }

            // Validate series column if specified
            if (!string.IsNullOrWhiteSpace(config.SeriesColumn) && !dt.Columns.Contains(config.SeriesColumn))
                throw new ArgumentException($"Column '{config.SeriesColumn}' not found in DataTable", nameof(config.SeriesColumn));

            // Validate y_min and y_max relationship
            if (config.YAxisMin.HasValue && config.YAxisMax.HasValue && config.YAxisMin.Value > config.YAxisMax.Value)
                throw new ArgumentException("Y-axis minimum cannot be greater than maximum");
        }

        private static List<ISeries> CreateSeriesFromDataTable(DataTable dt, ChartConfiguration config, out string[] categories)
        {
            var series = new List<ISeries>();
            categories = null;
            // detect x-axis kind
            var xKind = DetectXAxisKind(dt, config.XColumn);

            // If category axis, build global deterministic categories from whole table
            if (xKind == XAxisKind.Category)
            {
                categories = BuildCategoriesFromTable(dt, config.XColumn);
            }

            // Check if using MetricColumns (multiple columns as series)
            if (config.MetricColumns != null && config.MetricColumns.Length > 0)
            {
                // Each metric column becomes its own series
                foreach (var metricColumn in config.MetricColumns)
                {
                    if (xKind == XAxisKind.DateTime)
                    {
                        var values = ExtractDateTimePoints(dt.AsEnumerable(), config.XColumn, metricColumn);
                        if (values.Length > 0)
                        {
                            var seriesName = GetFriendlyColumnName(metricColumn, config);
                            series.Add(CreateSeriesForGroup(seriesName, values, config.ChartType, config.LineSmoothness, config.GeometrySize, config.LineFill));
                        }
                    }
                    else if (xKind == XAxisKind.Numeric)
                    {
                        var values = ExtractNumericPoints(dt.AsEnumerable(), config.XColumn, metricColumn);
                        if (values.Length > 0)
                        {
                            var seriesName = GetFriendlyColumnName(metricColumn, config);
                            series.Add(CreateSeriesForGroup(seriesName, values, config.ChartType, config.LineSmoothness, config.GeometrySize, config.LineFill));
                        }
                    }
                    else
                    {
                        // category: use index as X value and label categories on axis
                        var values = ExtractCategoryPoints(dt.AsEnumerable(), config.XColumn, metricColumn, categories);
                        if (values.Length > 0)
                        {
                            var seriesName = GetFriendlyColumnName(metricColumn, config);
                            series.Add(CreateSeriesForGroup(seriesName, values, config.ChartType, config.LineSmoothness, config.GeometrySize, config.LineFill));
                        }
                    }
                }
            }
            else if (string.IsNullOrWhiteSpace(config.SeriesColumn))
            {
                // Single series logic
                if (xKind == XAxisKind.DateTime)
                {
                    var values = ExtractDateTimePoints(dt.AsEnumerable(), config.XColumn, config.MetricColumn);
                    if (values.Length > 0)
                    {
                        var seriesName = GetFriendlyColumnName(config.MetricColumn, config);
                        series.Add(CreateSeriesForGroup(seriesName, values, config.ChartType, config.LineSmoothness, config.GeometrySize, config.LineFill));
                    }
                }
                else if (xKind == XAxisKind.Numeric)
                {
                    var values = ExtractNumericPoints(dt.AsEnumerable(), config.XColumn, config.MetricColumn);
                    if (values.Length > 0)
                    {
                        var seriesName = GetFriendlyColumnName(config.MetricColumn, config);
                        series.Add(CreateSeriesForGroup(seriesName, values, config.ChartType, config.LineSmoothness, config.GeometrySize, config.LineFill));
                    }
                }
                else
                {
                    var values = ExtractCategoryPoints(dt.AsEnumerable(), config.XColumn, config.MetricColumn, categories);
                    if (values.Length > 0)
                    {
                        var seriesName = GetFriendlyColumnName(config.MetricColumn, config);
                        series.Add(CreateSeriesForGroup(seriesName, values, config.ChartType, config.LineSmoothness, config.GeometrySize, config.LineFill));
                    }
                }
            }
            else
            {
                // Multi-series logic using SeriesColumn
                var groupedData = dt.Rows.Cast<DataRow>()
                    .Where(row => row[config.SeriesColumn] != null && row[config.SeriesColumn] != DBNull.Value)
                    .GroupBy(row => row[config.SeriesColumn].ToString());

                foreach (var group in groupedData)
                {
                    if (xKind == XAxisKind.DateTime)
                    {
                        var values = ExtractDateTimePoints(group, config.XColumn, config.MetricColumn);
                        if (values.Length > 0)
                            series.Add(CreateSeriesForGroup(group.Key, values, config.ChartType, config.LineSmoothness, config.GeometrySize, config.LineFill));
                    }
                    else if (xKind == XAxisKind.Numeric)
                    {
                        var values = ExtractNumericPoints(group, config.XColumn, config.MetricColumn);
                        if (values.Length > 0)
                            series.Add(CreateSeriesForGroup(group.Key, values, config.ChartType, config.LineSmoothness, config.GeometrySize, config.LineFill));
                    }
                    else
                    {
                        var values = ExtractCategoryPoints(group, config.XColumn, config.MetricColumn, categories);
                        if (values.Length > 0)
                            series.Add(CreateSeriesForGroup(group.Key, values, config.ChartType, config.LineSmoothness, config.GeometrySize, config.LineFill));
                    }
                }
            }

            return series;
        }

        /// <summary>
        /// Converts a column name to a friendly display name by adding spaces before capitals
        /// and handling common abbreviations (e.g., "SizeGB" -> "Size (GB)", "UsedMB" -> "Used (MB)")
        /// </summary>
        private static string GetFriendlyColumnName(string columnName, ChartConfigurationBase config)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                return columnName;

            // Check if there's an override in the configuration
            if (config.SeriesNames != null && config.SeriesNames.TryGetValue(columnName, out var customName))
                return customName;

            var result = columnName;

            // Add space before capital letter that is followed by a lowercase letter (word boundary)
            // This prevents splitting "GB" into "G B"
            // Pattern explanation:
            // (?<!^) = not at start
            // (?<=[a-z]) = preceded by lowercase (or)
            // ([A-Z]) = capital letter
            // (?=[A-Z][a-z]|[^A-Z]) = followed by capital+lowercase OR followed by non-capital
            result = System.Text.RegularExpressions.Regex.Replace(result, "(?<!^)(?<=[a-z])([A-Z])", " $1");

            // Also add space before a capital that follows multiple capitals and precedes lowercase
            // e.g., "IOPerformance" -> "IO Performance"
            result = System.Text.RegularExpressions.Regex.Replace(result, "(?<!^)([A-Z])(?=[a-z])", " $1");

            // Now handle common unit patterns - wrap in parentheses
            result = result.Replace(" GB", " (GB)")
                          .Replace(" MB", " (MB)")
                          .Replace(" KB", " (KB)")
                          .Replace(" TB", " (TB)")
                          .Replace(" Pct", " (%)")
                          .Replace(" Gb", " (Gb)")
                          .Replace(" Mb", " (Mb)")
                          .Replace(" Kb", " (Kb)")
                          .Replace(" Tb", " (Tb)");

            return result.Trim();
        }

        private static DateTimePoint[] ExtractDateTimePoints(IEnumerable<DataRow> rows, string xColumn, string metricColumn)
        {
            return rows
                .Where(row => row[xColumn] != null && row[xColumn] != DBNull.Value &&
                              row[metricColumn] != null && row[metricColumn] != DBNull.Value)
                .Select(row => TryCreateDateTimePoint(row, xColumn, metricColumn))
                .Where(point => point != null)
                .OrderBy(point => point.DateTime)
                .ToArray();
        }

        private static ObservablePoint[] ExtractNumericPoints(IEnumerable<DataRow> rows, string xColumn, string metricColumn)
        {
            var list = new List<ObservablePoint>();
            foreach (var row in rows)
            {
                var xVal = row[xColumn];
                var yVal = row[metricColumn];
                if (xVal == null || xVal == DBNull.Value || yVal == null || yVal == DBNull.Value)
                    continue;

                if (TryConvertToDouble(xVal, out var x) && TryConvertToDouble(yVal, out var y))
                {
                    list.Add(new ObservablePoint(x, y));
                }
            }
            return list.OrderBy(p => p.X).ToArray();
        }

        private static ObservablePoint[] ExtractCategoryPoints(IEnumerable<DataRow> rows, string xColumn, string metricColumn, string[] categories)
        {
            // Use provided global categories if available, otherwise build a deterministic
            // sorted list of distinct categories from the provided rows.
            var distinctCats = (categories != null)
                ? categories.ToList()
                : rows
                    .Select(r => r[xColumn])
                    .Where(v => v != null && v != DBNull.Value)
                    .Select(v => v.ToString())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Distinct()
                    .OrderBy(s => s, StringComparer.Ordinal)
                    .ToList();

            var list = new List<ObservablePoint>();

            foreach (var row in rows)
            {
                var xVal = row[xColumn];
                var yVal = row[metricColumn];
                if (xVal == null || xVal == DBNull.Value || yVal == null || yVal == DBNull.Value)
                    continue;

                var cat = xVal.ToString();
                var xIndex = distinctCats.IndexOf(cat);

                if (xIndex < 0)
                    continue; // skip unknown category

                if (TryConvertToDouble(yVal, out var y))
                {
                    list.Add(new ObservablePoint(xIndex, y));
                }
            }

            return list.OrderBy(p => p.X).ToArray();
        }

        private static string[] BuildCategoriesFromTable(DataTable dt, string xColumn)
        {
            return dt.Rows.Cast<DataRow>()
                .Select(r => r[xColumn])
                .Where(v => v != null && v != DBNull.Value)
                .Select(v => v.ToString())
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToArray();
        }

        private static XAxisKind DetectXAxisKind(DataTable dt, string xColumn)
        {
            if (!dt.Columns.Contains(xColumn))
                return XAxisKind.Category;

            var col = dt.Columns[xColumn];
            // Check column data type
            if (col.DataType == typeof(DateTime) || col.DataType == typeof(DateTimeOffset))
                return XAxisKind.DateTime;

            if (col.DataType == typeof(byte) || col.DataType == typeof(short) || col.DataType == typeof(int) || col.DataType == typeof(long)
                || col.DataType == typeof(float) || col.DataType == typeof(double) || col.DataType == typeof(decimal))
                return XAxisKind.Numeric;

            // Fallback: scan some rows to see if they parse as dates or numbers
            int maxScan = Math.Min(50, dt.Rows.Count);
            int dateCount = 0, numCount = 0;
            for (int i = 0; i < maxScan; i++)
            {
                var v = dt.Rows[i][xColumn];
                if (v == null || v == DBNull.Value) continue;
                if (TryConvertToDateTime(v, out _)) dateCount++;
                if (TryConvertToDouble(v, out _)) numCount++;
            }
            if (dateCount > numCount && dateCount > 0) return XAxisKind.DateTime;
            if (numCount > 0 && numCount >= dateCount) return XAxisKind.Numeric;
            return XAxisKind.Category;
        }

        private static SolidColorPaint CreateLabelPaint()
        {
            return DBADashUser.SelectedTheme.ThemeIdentifier == ThemeType.Dark
                ? new SolidColorPaint(DashColors.White.ToSKColor())
                : new SolidColorPaint(DashColors.TrimbleBlueDark.ToSKColor());
        }

        private static Axis[] CreateXAxes(TimeSpan unit, SolidColorPaint labelPaint, DateTime minDate, DateTime maxDate, string label)
        {
            var duration = maxDate - minDate;
            var labelFontSize = DBADashUser.ChartAxisLabelFontSize;
            var nameFontSize = DBADashUser.ChartAxisNameFontSize;
            return new Axis[]
            {
                new DateTimeAxis(unit, date => FormatDateForChartLabel(date, duration))
                {
                    LabelsPaint = labelPaint,
                    TextSize = labelFontSize,
                    NamePaint = labelPaint,
                    NameTextSize = nameFontSize,
                    MinLimit = minDate.Ticks,
                    Name = label
                }
            };
        }

        private static Axis[] CreateYAxes(ChartConfiguration config, SolidColorPaint labelPaint)
        {
            var labelFontSize = DBADashUser.ChartAxisLabelFontSize;
            var nameFontSize = DBADashUser.ChartAxisNameFontSize;

            // If multiple Y-axes are configured, use them
            if (config.YAxes != null && config.YAxes.Length > 0)
            {
                var axes = new Axis[config.YAxes.Length];
                for (int i = 0; i < config.YAxes.Length; i++)
                {
                    var axisConfig = config.YAxes[i];
                    var axis = new Axis
                    {
                        LabelsPaint = labelPaint,
                        TextSize = labelFontSize,
                        MinLimit = axisConfig.MinLimit ?? 0,
                        Name = axisConfig.Label,
                        NamePaint = labelPaint,
                        NameTextSize = nameFontSize,
                        Position = axisConfig.Position
                    };

                    if (axisConfig.MaxLimit.HasValue)
                        axis.MaxLimit = axisConfig.MaxLimit.Value;

                    if (!string.IsNullOrEmpty(axisConfig.Format))
                    {
                        // Capture format locally to avoid closure issues and guard against invalid format strings
                        var fmt = axisConfig.Format;
                        axis.Labeler = value =>
                        {
                            try
                            {
                                return value.ToString(fmt, CultureInfo.InvariantCulture);
                            }
                            catch (FormatException)
                            {
                                // Fall back to a safe invariant format if the provided format is invalid
                                return value.ToString(CultureInfo.InvariantCulture);
                            }
                        };
                    }

                    axes[i] = axis;
                }
                return axes;
            }

            // Otherwise, create a single Y-axis using legacy properties
            var yAxis = new Axis
            {
                LabelsPaint = labelPaint,
                TextSize = labelFontSize,
                MinLimit = 0,
                Name = config.YAxisLabel,
                NamePaint = labelPaint,
                NameTextSize = nameFontSize
            };

            if (config.YAxisMax.HasValue)
                yAxis.MaxLimit = config.YAxisMax.Value;

            if (config.YAxisMin.HasValue)
                yAxis.MinLimit = config.YAxisMin.Value;

            if (!string.IsNullOrEmpty(config.YAxisFormat))
            {
                var fmt = config.YAxisFormat;
                yAxis.Labeler = value =>
                {
                    try
                    {
                        return value.ToString(fmt, CultureInfo.InvariantCulture);
                    }
                    catch (FormatException)
                    {
                        return value.ToString(CultureInfo.InvariantCulture);
                    }
                };
            }

            return new Axis[] { yAxis };
        }

        /// <summary>
        /// Assigns series to the correct Y-axis based on MetricColumnAxisMap
        /// </summary>
        private static void AssignSeriesToYAxes(List<ISeries> series, ChartConfiguration config)
        {
            foreach (var s in series)
            {
                if (s.Name == null) continue;

                // Find the metric column name by looking up in SeriesNames (reverse lookup)
                string metricColumn = null;
                if (config.SeriesNames != null)
                {
                    // Series name might be a friendly name, find the original column name
                    metricColumn = config.SeriesNames.FirstOrDefault(kvp => kvp.Value == s.Name).Key;
                }

                // If not found in SeriesNames, the series name might be the column name itself
                if (string.IsNullOrEmpty(metricColumn))
                {
                    metricColumn = s.Name;
                }

                // Look up the axis index for this metric column
                if (config.MetricColumnAxisMap.TryGetValue(metricColumn, out var axisIndex))
                {
                    // In LiveChartsCore, assign using the ScalesYAt property
                    // Set the property via reflection
                    var seriesType = s.GetType();
                    var property = seriesType.GetProperty("ScalesYAt");
                    if (property != null && property.CanWrite)
                    {
                        property.SetValue(s, axisIndex);
                    }
                }
            }
        }

        /// <summary>
        /// Assigns series to Y-axes using name-based mapping (more intuitive than index-based)
        /// </summary>
        private static void AssignSeriesToYAxesByName(List<ISeries> series, ChartConfiguration config, Axis[] yAxes)
        {
            // Build a map from axis name to physical index
            var axisNameToIndex = new Dictionary<string, int>();
            for (int i = 0; i < config.YAxes.Length; i++)
            {
                if (!string.IsNullOrEmpty(config.YAxes[i].Name))
                {
                    axisNameToIndex[config.YAxes[i].Name] = i;
                }
            }

            foreach (var s in series)
            {
                if (s.Name == null) continue;

                // Find the metric column name
                string metricColumn = null;
                if (config.SeriesNames != null)
                {
                    metricColumn = config.SeriesNames.FirstOrDefault(kvp => kvp.Value == s.Name).Key;
                }
                if (string.IsNullOrEmpty(metricColumn))
                {
                    metricColumn = s.Name;
                }

                // Look up the axis name for this column
                if (config.ColumnAxisNames.TryGetValue(metricColumn, out var axisName))
                {
                    // Convert axis name to physical index
                    if (axisNameToIndex.TryGetValue(axisName, out var axisIndex))
                    {
                        // Assign the series to the axis
                        var seriesType = s.GetType();
                        var property = seriesType.GetProperty("ScalesYAt");
                        if (property != null && property.CanWrite)
                        {
                            property.SetValue(s, axisIndex);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Extracts the min and max dates from all series in the chart
        /// </summary>
        /// <param name="series"></param>
        /// <returns>Tuple containing min and max DateTime values</returns>
        private static (DateTime minDate, DateTime maxDate) GetDateRangeFromSeries(List<ISeries> series)
        {
            var allDates = new List<DateTime>();
            foreach (var s in series)
            {
                if (s?.Values == null) continue;
                if (s.Values is IEnumerable<DateTimePoint> dtPoints)
                {
                    allDates.AddRange(dtPoints.Select(p => p.DateTime));
                }
            }
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
        /// <param name="minDate">Minimum date in the range</param>
        /// <param name="maxDate">Maximum date in the range</param>
        /// <param name="chartType">Type of chart being created</param>
        /// <param name="series">Series data for automatic calculation</param>
        /// <param name="config">Chart configuration (may contain explicit DateUnit)</param>
        /// <returns>The date unit interval for the X-axis</returns>
        private static TimeSpan CalculateDateUnit(DateTime minDate, DateTime maxDate, ChartTypes chartType, List<ISeries> series, ChartConfiguration config)
        {
            // If DateUnit is explicitly specified, use it
            if (config.DateUnit.HasValue && config.DateUnit.Value > TimeSpan.Zero)
                return config.DateUnit.Value;

            // Otherwise, calculate automatically
            if (chartType == ChartTypes.StackedColumn || chartType == ChartTypes.Column)
            {
                return CalculateDateUnitForStackedColumn(series);
            }

            // Calculate based on target number of points
            const int points = 200;
            const int chartMinGrouping = 1; // Minimum grouping of 1 minute
            var durationMins = (int)(maxDate - minDate).TotalMinutes;
            return TimeSpan.FromMinutes(DateHelper.DateGrouping(durationMins, points, chartMinGrouping));
        }

        /// <summary>
        /// Automatically detect the date unit from the series data by finding the minimum
        /// interval between consecutive data points across all series. This represents the
        /// actual data collection interval and determines the width of the bars.
        /// </summary>
        /// <param name="series">The chart series containing DateTimePoint data</param>
        /// <returns>The detected interval as a TimeSpan, or a default of 1 minute if it cannot be determined</returns>
        private static TimeSpan CalculateDateUnitForStackedColumn(List<ISeries> series)
        {
            const int defaultIntervalMinutes = 1;
            const int maxSampleSize = 50;

            if (series == null || series.Count == 0)
                return TimeSpan.FromMinutes(defaultIntervalMinutes);

            // Find the global minimum interval across all series
            // Important: Check all series, not just the first one, because different series
            // may have different data densities (e.g., Series A every 5 min, Series B every 1 min)
            double minIntervalMinutes = double.MaxValue;
            int totalSamplesProcessed = 0;

            foreach (var s in series)
            {
                IEnumerable<DateTimePoint> points = s switch
                {
                    StackedColumnSeries<DateTimePoint> columnSeries => columnSeries.Values?.Cast<DateTimePoint>(),
                    StackedAreaSeries<DateTimePoint> areaSeries => areaSeries.Values?.Cast<DateTimePoint>(),
                    LineSeries<DateTimePoint> lineSeries => lineSeries.Values?.Cast<DateTimePoint>(),
                    _ => null
                };

                if (points == null)
                    continue;

                // Points are already sorted from ExtractDataPoints, so no need to OrderBy again
                DateTime? previousDate = null;

                foreach (var point in points)
                {
                    if (previousDate.HasValue)
                    {
                        var intervalMinutes = (point.DateTime - previousDate.Value).TotalMinutes;
                        if (intervalMinutes > 0 && intervalMinutes < minIntervalMinutes)
                        {
                            minIntervalMinutes = intervalMinutes;
                        }

                        if (++totalSamplesProcessed >= maxSampleSize)
                            break;
                    }
                    previousDate = point.DateTime;
                }

                // Stop if we've hit the sample budget
                if (totalSamplesProcessed >= maxSampleSize)
                    break;
            }

            if (minIntervalMinutes == double.MaxValue)
                return TimeSpan.FromMinutes(defaultIntervalMinutes);

            var detectedIntervalMinutes = (int)Math.Round(minIntervalMinutes);
            return detectedIntervalMinutes > 0
                ? TimeSpan.FromMinutes(detectedIntervalMinutes)
                : TimeSpan.FromMinutes(defaultIntervalMinutes);
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

                // Extract color based on series type (handle both DateTimePoint and ObservablePoint series)
                switch (series)
                {
                    case StackedAreaSeries<DateTimePoint> areaSeriesD:
                        if (areaSeriesD.Fill is SolidColorPaint fpD) color = fpD.Color;
                        break;

                    case StackedColumnSeries<DateTimePoint> columnSeriesD:
                        if (columnSeriesD.Fill is SolidColorPaint fpDc) color = fpDc.Color;
                        break;

                    case ColumnSeries<DateTimePoint> columnSeriesDt:
                        if (columnSeriesDt.Fill is SolidColorPaint fpDt) color = fpDt.Color;
                        break;

                    case LineSeries<DateTimePoint> lineSeriesD:
                        if (lineSeriesD.Stroke is SolidColorPaint spD) color = spD.Color;
                        break;

                    case StackedAreaSeries<ObservablePoint> areaSeriesO:
                        if (areaSeriesO.Fill is SolidColorPaint fpO) color = fpO.Color;
                        break;

                    case StackedColumnSeries<ObservablePoint> columnSeriesO:
                        if (columnSeriesO.Fill is SolidColorPaint fpOc) color = fpOc.Color;
                        break;

                    case ColumnSeries<ObservablePoint> columnSeriesO2:
                        if (columnSeriesO2.Fill is SolidColorPaint fpO2) color = fpO2.Color;
                        break;

                    case LineSeries<ObservablePoint> lineSeriesO:
                        if (lineSeriesO.Stroke is SolidColorPaint spO) color = spO.Color;
                        break;
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

        /// <summary>
        /// Manages applying an InnerRadius fraction to a PieChart by computing
        /// the pixel MaxRadialColumnWidth value for each PieSeries based on
        /// the chart control size. The manager attaches a SizeChanged handler
        /// and unregisters it when the chart is disposed.
        /// </summary>
        private sealed class PieDonutManager
        {
            private readonly Control chart;
            private readonly List<ISeries> series;
            private readonly double innerFraction;

            public PieDonutManager(Control chart, List<ISeries> series, double innerFraction)
            {
                this.chart = chart ?? throw new ArgumentNullException(nameof(chart));
                this.series = series ?? throw new ArgumentNullException(nameof(series));
                this.innerFraction = Math.Clamp(innerFraction, 0.0, 1.0);

                // attach handlers
                this.chart.SizeChanged += Chart_SizeChanged;
                this.chart.Disposed += Chart_Disposed;

                // initial apply
                ApplyMaxRadialColumnWidth();
            }

            private void Chart_SizeChanged(object sender, EventArgs e)
            {
                ApplyMaxRadialColumnWidth();
            }

            private void Chart_Disposed(object sender, EventArgs e)
            {
                // detach handlers to allow GC of captured objects
                try
                {
                    chart.SizeChanged -= Chart_SizeChanged;
                    chart.Disposed -= Chart_Disposed;
                }
                catch { }
            }

            private void ApplyMaxRadialColumnWidth()
            {
                try
                {
                    var minDim = Math.Min(chart.ClientSize.Width, chart.ClientSize.Height);
                    var radius = minDim / 2.0;
                    var thicknessPixels = radius * (1.0 - innerFraction);
                    if (thicknessPixels < 1.0) thicknessPixels = 1.0; // avoid zero which may hide slices

                    foreach (var s in series)
                    {
                        if (s is PieSeries<ObservableValue> ps)
                        {
                            ps.MaxRadialColumnWidth = thicknessPixels;
                        }
                    }
                }
                catch
                {
                    // swallow - do not let UI fail on layout hiccups
                }
            }
        }
    }
}