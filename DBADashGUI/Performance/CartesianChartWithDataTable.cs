using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;

namespace DBADashGUI.Performance
{
    internal class CartesianChartWithDataTable : LiveCharts.WinForms.CartesianChart
    {
        public int DefaultPointSize = 10;
        private double _defaultLineSmoothness = 0.5;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public double DefaultLineSmoothness
        {
            get => _defaultLineSmoothness;
            set
            {
                _defaultLineSmoothness = value;
                foreach (LineSeries s in Series.Cast<LineSeries>())
                {
                    s.LineSmoothness = _defaultLineSmoothness;
                }
            }
        }

        public void SetPointSize(int pointSize)
        {
            DefaultPointSize = pointSize;
            foreach (LineSeries s in Series.Cast<LineSeries>())
            {
                s.PointGeometrySize = pointSize;
            }
        }

        public System.Windows.Media.Brush DefaultFill;

        public void UpdateColumnVisibility(Dictionary<string, ColumnMetaData> columns)
        {
            foreach (LineSeries s in Series.Cast<LineSeries>())
            {
                s.Visibility = columns[(string)s.Tag].IsVisible ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
            }
        }

        public void AddDataTable(DataTable dt, Dictionary<string, ColumnMetaData> columns, string dateCol, bool convertToLocalTime = true)
        {
            var cnt = dt.Rows.Count;
            if (cnt < 1)
            {
                return;
            }
            foreach (var s in columns.Keys)
            {
                columns[s].Points = new DateTimePoint[cnt];
            }

            int i = 0;
            foreach (DataRow r in dt.Rows)
            {
                foreach (string s in columns.Keys)
                {
                    var v = r[s] == DBNull.Value ? 0 : Convert.ToDouble(r[s]);
                    var t = (DateTime)r[dateCol];
                    if (convertToLocalTime) { t = t.ToAppTimeZone(); }
                    columns[s].Points[i] = new DateTimePoint(t, v);
                }
                i++;
            }

            var sc = new SeriesCollection();
            foreach (string s in columns.Keys)
            {
                var v = new ChartValues<DateTimePoint>();
                v.AddRange(columns[s].Points);
                sc.Add(new LineSeries
                {
                    Title = columns[s].Name,
                    Tag = s,
                    ScalesYAt = columns[s].axis,
                    PointGeometrySize = cnt <= 100 ? DefaultPointSize : 0,
                    LineSmoothness = DefaultLineSmoothness,
                    Values = v,
                    Fill = DefaultFill
                }
                );
            }
            Series.AddRange(sc);
            UpdateColumnVisibility(columns);
            AxisX.Clear();
            if (AxisX.Count == 0)
            {
                AxisX.Add(new Axis
                {
                    Title = "Time",
                    LabelFormatter = val => new DateTime((long)val).ToString(DateRange.DateFormatString)
                });
            }
            if (Series[0].Values.Count == 1)
            {
                Series.Clear(); // fix tends to zero error
            }
        }
    }
}