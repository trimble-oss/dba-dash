using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DBADash.DailyReport
{
    /// <summary>The hourly series a chart draws: the last 24 hours, and the baseline weeks' range and median for each hour</summary>
    public class PerformanceChartSeries
    {
        public string Metric { get; init; }
        public string Label { get; init; }
        public string Unit { get; init; }
        /// <summary>24 values, null where the hour has no data</summary>
        public double?[] Current { get; init; }
        public double?[] UsualMin { get; init; }
        public double?[] UsualMax { get; init; }
        public double?[] UsualMedian { get; init; }
        public double? Threshold { get; init; }
        /// <summary>Axis label for each hour (local time)</summary>
        public string[] HourLabels { get; init; }
        /// <summary>24 flags: hours the report doesn't count (outside its hours of the day), shaded.  Null: every hour counts</summary>
        public bool[] NotCounted { get; init; }

        public bool HasBaseline => UsualMedian.Any(v => v.HasValue);
        public bool HasData => Current.Count(v => v.HasValue) >= 2;

        public static PerformanceChartSeries From(InstancePerformanceResult result, PerformanceFinding finding, PerformanceThresholdValues thresholds,
            int baselineWeeks, Func<int, string> formatHour, HashSet<(int Period, int HourOffset)> includedHours = null)
        {
            var hours = result.Data?.Hours ?? new List<PerformanceHour>();
            var fullHour = PerformanceAnalyzer.FullHourCPUSamples(hours);
            double? At(int period, int hour)
            {
                var h = hours.FirstOrDefault(x => x.Period == period && x.HourOffset == hour);
                return h == null ? null : PerformanceAnalyzer.HourlyValue(finding.Metric, h, thresholds.MinIOPs, fullHour);
            }

            var current = new double?[24];
            var min = new double?[24];
            var max = new double?[24];
            var median = new double?[24];
            for (var hour = 0; hour < 24; hour++)
            {
                current[hour] = At(0, hour);
                var usual = Enumerable.Range(1, baselineWeeks).Select(p => At(p, hour)).Where(v => v.HasValue).Select(v => v!.Value).ToList();
                if (usual.Count == 0) continue;
                min[hour] = usual.Min();
                max[hour] = usual.Max();
                median[hour] = PerformanceAnalyzer.Median(usual.Select(v => (double?)v));
            }

            var info = DailyReportSettings.Metrics.FirstOrDefault(m => m.Key == finding.Metric);
            return new PerformanceChartSeries
            {
                Metric = finding.Metric,
                Label = finding.Label,
                Unit = info?.Unit ?? string.Empty,
                Current = current,
                UsualMin = min,
                UsualMax = max,
                UsualMedian = median,
                Threshold = PerformanceAnalyzer.WarningThreshold(finding.Metric, thresholds),
                HourLabels = Enumerable.Range(0, 24).Select(formatHour).ToArray(),
                NotCounted = includedHours == null ? null : Enumerable.Range(0, 24).Select(h => !includedHours.Contains((0, h))).ToArray()
            };
        }
    }

    /// <summary>
    /// Draws a daily report chart as a PNG to embed in the email: the last 24 hours as a line over the usual range (the baseline weeks' min to
    /// max as a band, their median dashed), with the warning threshold where the measure has one.  Drawn at twice the size it's shown at, so
    /// it stays sharp on high DPI screens.  Colours are the Trimble Modus palette the rest of the report uses.
    /// </summary>
    public static class PerformanceChart
    {
        public const int Width = 680;
        public const int Height = 150;
        private const float Scale = 2f;

        /// <summary>Created once and kept: FromFamilyName falls back to the default typeface when the font isn't there, which mustn't be disposed</summary>
        private static readonly Lazy<SKTypeface> Typeface = new(() => SKTypeface.FromFamilyName("Segoe UI"));

        private static readonly SKColor Blue = SKColor.Parse("#0063a3");
        private static readonly SKColor Band = SKColor.Parse("#dcedf9");
        private static readonly SKColor Median = SKColor.Parse("#6a6e79");
        private static readonly SKColor Grid = SKColor.Parse("#e0e1e9");
        private static readonly SKColor Text = SKColor.Parse("#6a6e79");
        private static readonly SKColor Red = SKColor.Parse("#da212c");
        private static readonly SKColor NotCountedShade = SKColor.Parse("#f1f1f6");

        public static byte[] RenderPng(PerformanceChartSeries s)
        {
            var w = (int)(Width * Scale);
            var h = (int)(Height * Scale);
            using var surface = SKSurface.Create(new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Premul));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);
            canvas.Scale(Scale);

            var typeface = Typeface.Value;
            using var font = new SKFont(typeface, 10.5f);
            using var textPaint = new SKPaint { Color = Text, IsAntialias = true };

            const float left = 44, right = 8, top = 24, bottom = 20;
            var plotW = Width - left - right;
            var plotH = Height - top - bottom;

            var values = s.Current.Concat(s.UsualMax).Where(v => v.HasValue).Select(v => v!.Value).ToList();
            var dataMax = values.Count == 0 ? 1 : values.Max();
            // The threshold is drawn when it's near the data - one far above it would flatten the chart
            var showThreshold = s.Threshold.HasValue && s.Threshold.Value <= dataMax * 1.5;
            var yMax = NiceMax(Math.Max(dataMax, showThreshold ? s.Threshold!.Value : 0) * 1.1);
            // A percentage can't go over 100, so the axis stops there rather than rounding up to 200
            if (s.Unit == "%" && dataMax <= 100) yMax = Math.Min(yMax, 100);

            float X(int hour) => left + (hour + 0.5f) * plotW / 24f;
            float Y(double v) => top + plotH - (float)(v / yMax) * plotH;

            // Grid and axis labels
            using (var gridPaint = new SKPaint { Color = Grid, StrokeWidth = 0.75f, IsAntialias = true })
            {
                foreach (var v in new[] { 0, yMax / 2, yMax })
                {
                    canvas.DrawLine(left, Y(v), left + plotW, Y(v), gridPaint);
                    canvas.DrawText(FormatValue(v, s.Unit), left - 4, Y(v) + 3.5f, SKTextAlign.Right, font, textPaint);
                }
            }
            for (var hour = 0; hour < 24; hour += 3)
            {
                canvas.DrawText(s.HourLabels[hour], X(hour), Height - 5, SKTextAlign.Center, font, textPaint);
            }

            // Hours the report doesn't count, shaded behind everything else
            if (s.NotCounted != null)
            {
                using var shadePaint = new SKPaint { Color = NotCountedShade, Style = SKPaintStyle.Fill };
                foreach (var run in Runs(hour => s.NotCounted[hour]))
                {
                    canvas.DrawRect(SKRect.Create(left + run[0] * plotW / 24f, top, run.Count * plotW / 24f, plotH), shadePaint);
                }
            }

            // Usual range band and median, drawn in runs of consecutive hours with data
            if (s.HasBaseline)
            {
                using var bandPaint = new SKPaint { Color = Band, Style = SKPaintStyle.Fill, IsAntialias = true };
                foreach (var run in Runs(hour => s.UsualMin[hour].HasValue && s.UsualMax[hour].HasValue))
                {
                    using var path = new SKPath();
                    path.MoveTo(X(run[0]), Y(s.UsualMax[run[0]]!.Value));
                    foreach (var hour in run.Skip(1)) path.LineTo(X(hour), Y(s.UsualMax[hour]!.Value));
                    foreach (var hour in run.AsEnumerable().Reverse()) path.LineTo(X(hour), Y(s.UsualMin[hour]!.Value));
                    path.Close();
                    canvas.DrawPath(path, bandPaint);
                }
                using var medianPaint = new SKPaint { Color = Median, StrokeWidth = 1, Style = SKPaintStyle.Stroke, IsAntialias = true, PathEffect = SKPathEffect.CreateDash(new[] { 4f, 3f }, 0) };
                DrawLine(canvas, s.UsualMedian, X, Y, medianPaint);
            }

            if (showThreshold)
            {
                using var thresholdPaint = new SKPaint { Color = Red, StrokeWidth = 1.25f, Style = SKPaintStyle.Stroke, IsAntialias = true, PathEffect = SKPathEffect.CreateDash(new[] { 5f, 3f }, 0) };
                canvas.DrawLine(left, Y(s.Threshold!.Value), left + plotW, Y(s.Threshold.Value), thresholdPaint);
            }

            using (var linePaint = new SKPaint { Color = Blue, StrokeWidth = 2, Style = SKPaintStyle.Stroke, IsAntialias = true, StrokeJoin = SKStrokeJoin.Round })
            using (var dotPaint = new SKPaint { Color = Blue, Style = SKPaintStyle.Fill, IsAntialias = true })
            {
                DrawLine(canvas, s.Current, X, Y, linePaint);
                // A lone hour has no line to show it, so every hour gets a dot
                for (var hour = 0; hour < 24; hour++)
                {
                    if (s.Current[hour].HasValue) canvas.DrawCircle(X(hour), Y(s.Current[hour]!.Value), 1.8f, dotPaint);
                }
            }

            // Title and legend along the top
            using (var titlePaint = new SKPaint { Color = SKColor.Parse("#252a2e"), IsAntialias = true })
            using (var titleFont = new SKFont(typeface, 11) { Embolden = true })
            {
                canvas.DrawText($"{s.Label} by hour{(string.IsNullOrEmpty(s.Unit) ? string.Empty : $" ({s.Unit})")}", left, 11, SKTextAlign.Left, titleFont, titlePaint);
            }
            var legendX = left + plotW;
            legendX = Legend(canvas, legendX, "Last 24 hours", Blue, false, font, textPaint);
            if (s.HasBaseline) legendX = Legend(canvas, legendX, "Usual (range, median)", Median, true, font, textPaint);
            if (showThreshold) legendX = Legend(canvas, legendX, "Warning threshold", Red, true, font, textPaint);
            if (s.NotCounted != null && s.NotCounted.Any(n => n)) ShadeLegend(canvas, legendX, "Not counted", font, textPaint);

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }

        /// <summary>Draws a legend entry ending at x, and returns where the next one (to its left) should end</summary>
        private static float Legend(SKCanvas canvas, float x, string text, SKColor color, bool dashed, SKFont font, SKPaint textPaint)
        {
            var textWidth = font.MeasureText(text);
            canvas.DrawText(text, x, 11, SKTextAlign.Right, font, textPaint);
            using var paint = new SKPaint { Color = color, StrokeWidth = dashed ? 1 : 2, IsAntialias = true, PathEffect = dashed ? SKPathEffect.CreateDash(new[] { 3f, 2f }, 0) : null };
            var lineEnd = x - textWidth - 3;
            canvas.DrawLine(lineEnd - 12, 7.5f, lineEnd, 7.5f, paint);
            return lineEnd - 22;
        }

        /// <summary>A legend entry with a shaded square rather than a line</summary>
        private static void ShadeLegend(SKCanvas canvas, float x, string text, SKFont font, SKPaint textPaint)
        {
            var textWidth = font.MeasureText(text);
            canvas.DrawText(text, x, 11, SKTextAlign.Right, font, textPaint);
            using var paint = new SKPaint { Color = SKColor.Parse("#d6d7e0"), Style = SKPaintStyle.Fill };
            canvas.DrawRect(SKRect.Create(x - textWidth - 13, 3, 9, 9), paint);
        }

        private static void DrawLine(SKCanvas canvas, double?[] values, Func<int, float> x, Func<double, float> y, SKPaint paint)
        {
            foreach (var run in Runs(hour => values[hour].HasValue))
            {
                if (run.Count < 2) continue;
                using var path = new SKPath();
                path.MoveTo(x(run[0]), y(values[run[0]]!.Value));
                foreach (var hour in run.Skip(1)) path.LineTo(x(hour), y(values[hour]!.Value));
                canvas.DrawPath(path, paint);
            }
        }

        /// <summary>Runs of consecutive hours where hasValue is true, so a gap in the data is a gap in the line</summary>
        private static List<List<int>> Runs(Func<int, bool> hasValue)
        {
            var runs = new List<List<int>>();
            List<int> run = null;
            for (var hour = 0; hour < 24; hour++)
            {
                if (hasValue(hour))
                {
                    run ??= new List<int>();
                    run.Add(hour);
                }
                else if (run != null)
                {
                    runs.Add(run);
                    run = null;
                }
            }
            if (run != null) runs.Add(run);
            return runs;
        }

        /// <summary>1, 2 or 5 times a power of ten, at or above the value, so the axis labels are round numbers</summary>
        public static double NiceMax(double value)
        {
            if (value <= 0 || double.IsNaN(value)) return 1;
            var magnitude = Math.Pow(10, Math.Floor(Math.Log10(value)));
            foreach (var step in new[] { 1, 2, 5, 10 })
            {
                if (step * magnitude >= value) return step * magnitude;
            }
            return 10 * magnitude;
        }

        private static string FormatValue(double v, string unit) =>
            unit == "%" ? v.ToString("0", CultureInfo.InvariantCulture) + "%"
            : v >= 10000 ? TopQueryAnalyzer.Compact(v)
            : v.ToString(v < 10 && v != 0 ? "0.#" : "#,0", CultureInfo.InvariantCulture);
    }
}
