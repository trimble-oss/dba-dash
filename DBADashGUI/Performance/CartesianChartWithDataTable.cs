using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;

namespace DBADashGUI.Performance
{
    internal class CartesianChartWithDataTable : LiveChartsCore.SkiaSharpView.WinForms.CartesianChart
    {
        public int DefaultPointSize = 8;
        private double _defaultLineSmoothness = 0.2;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public double DefaultLineSmoothness
        {
            get => _defaultLineSmoothness;
            set
            {
                _defaultLineSmoothness = value;
                // LiveCharts2 LineSeries<T> uses GeometryFill/Stroke; smoothness handled via LineSmoothness
                foreach (var s in Series.OfType<LineSeries<DateTimePoint>>())
                {
                    s.LineSmoothness = _defaultLineSmoothness;
                }
            }
        }

        public void SetPointSize(int pointSize)
        {
            DefaultPointSize = pointSize;
            foreach (var s in Series.OfType<LineSeries<DateTimePoint>>())
            {
                s.GeometrySize = pointSize;
            }
        }

        public SKColor? DefaultFill;

        public void UpdateColumnVisibility(Dictionary<string, ColumnMetaData> columns)
        {
            foreach (var s in Series.OfType<LineSeries<DateTimePoint>>())
            {
                var key = (string)s.Tag;
                s.IsVisible = columns.TryGetValue(key, out var meta) && meta.IsVisible;
            }
        }

        public void AddDataTable(DataTable dt, Dictionary<string, ColumnMetaData> columns, string dateCol, bool convertToLocalTime = true)
        {
            var cnt = dt.Rows.Count;
            if (cnt < 1)
            {
                return;
            }

            // Build a local map of column name -> DateTimePoint[] for LiveCharts2
            var columnPoints = new Dictionary<string, DateTimePoint[]>(columns.Count);

            foreach (var key in columns.Keys)
            {
                columnPoints[key] = new DateTimePoint[cnt];
            }

            var rows = dt.Rows.Cast<DataRow>().ToList();

            for (var i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                foreach (var key in columns.Keys)
                {
                    var v = r[key] == DBNull.Value ? 0 : Convert.ToDouble(r[key]);
                    var t = (DateTime)r[dateCol];
                    if (convertToLocalTime)
                    {
                        t = t.ToAppTimeZone();
                    }
                    columnPoints[key][i] = new DateTimePoint(t, v);
                }
            }
            var keys = columns.Keys.ToList();
            var sc = new List<ISeries>();
            for (var i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                var meta = columns[key];
                var v = columnPoints[key];

                var lineSeries = new LineSeries<DateTimePoint>
                {
                    Name = meta.Name,
                    Tag = key,
                    ScalesYAt = meta.axis,
                    LineSmoothness = DefaultLineSmoothness,
                    Values = v,
                    GeometrySize = 10,
                };

                sc.Add(lineSeries);
                i += 1;
            }

            Series = sc;
            UpdateColumnVisibility(columns);

            XAxes = new List<Axis>
            {
                new Axis
                {
                    Name = "Time",
                    Labeler = val => new DateTime((long)val).ToString(DateRange.DateFormatString)
                }
            };

            if (Series.FirstOrDefault() is LineSeries<DateTimePoint> first &&
                first.Values is IReadOnlyCollection<DateTimePoint> vals &&
                vals.Count == 1)
            {
                Series = new List<ISeries>(); // fix tends to zero error
            }
        }

        // Very small HSL → SKColor helper
        private static SKColor FromHsl(double h, double s, double l)
        {
            double r, g, b;

            if (Math.Abs(s) <= double.Epsilon)
            {
                r = g = b = l;
            }
            else
            {
                double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
                double p = 2 * l - q;

                double HueToRgb(double p2, double q2, double t)
                {
                    if (t < 0) t += 1;
                    if (t > 1) t -= 1;
                    if (t < 1.0 / 6) return p2 + (q2 - p2) * 6 * t;
                    if (t < 1.0 / 2) return q2;
                    if (t < 2.0 / 3) return p2 + (q2 - p2) * (2.0 / 3 - t) * 6;
                    return p2;
                }

                r = HueToRgb(p, q, h + 1.0 / 3);
                g = HueToRgb(p, q, h);
                b = HueToRgb(p, q, h - 1.0 / 3);
            }

            return new SKColor(
                (byte)(r * 255),
                (byte)(g * 255),
                (byte)(b * 255));
        }
    }
}