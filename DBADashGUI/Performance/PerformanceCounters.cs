using DBADashGUI.Charts;
using DBADashGUI.Theme;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using Microsoft.Data.SqlClient;
using System;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using static DBADashGUI.Performance.IMetric;

namespace DBADashGUI.Performance
{
    public partial class PerformanceCounters : UserControl, IMetricChart
    {
        public PerformanceCounters()
        {
            InitializeComponent();
        }

        public event EventHandler<EventArgs> Close;

        public event EventHandler<EventArgs> MoveUp;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool CloseVisible
        {
            get => tsClose.Visible;
            set => tsClose.Visible = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool MoveUpVisible
        {
            get => tsUp.Visible;
            set => tsUp.Visible = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int InstanceID { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DateTime FromDate { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DateTime ToDate { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int CounterID { get => Metric.CounterID; set => Metric.CounterID = value; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string CounterName { get => Metric.CounterName; set => Metric.CounterName = value; }

        private PerformanceCounterMetric _metric = new();

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public PerformanceCounterMetric Metric
        { get => _metric; set { _metric = value; SelectAggregate(); } }

        IMetric IMetricChart.Metric => Metric;

        private bool smoothLines = false;
        private double geometrySize = ChartConfiguration.DefaultGeometrySize;
        private double lineSmoothness = 0;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool SmoothLines
        {
            get => smoothLines;
            set
            {
                smoothLines = value;
                lineSmoothness = value ? ChartConfiguration.DefaultLineSmoothness : 0;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public double PointSize
        {
            get => geometrySize;
            set => geometrySize = value;
        }

        private int durationMins;
        private int DateGrouping;
        private DataTable dt;

        private void SelectAggregate()
        {
            tsAgg.Text = Enum.GetName(Metric.AggregateType);
            foreach (ToolStripMenuItem mnu in tsAgg.DropDownItems)
            {
                mnu.Checked = (string)mnu.Tag == Enum.GetName(Metric.AggregateType);
            }
        }

        public void RefreshData()
        {
            SetDateGroup(DateRange.DurationMins);
            dt = GetPerformanceCounter();
            RefreshChart();
        }

        public void RefreshData(int instanceID)
        {
            InstanceID = instanceID;
            RefreshData();
        }

        private void SetDateGroup(int mins)
        {
            if (durationMins == mins) return; // Change date group only if date range has changed.
            DateGrouping = DateHelper.DateGrouping(mins, 200);
            tsDateGrouping.Text = DateHelper.DateGroupString(DateGrouping);
            durationMins = mins;
        }

        private void RefreshChart()
        {
            tsTitle.Text = Metric.CounterName;
            if (dt == null || dt.Rows.Count < 2)
            {
                chart1.Series = Array.Empty<ISeries>();
                chart1.XAxes = Array.Empty<Axis>();
                chart1.YAxes = Array.Empty<Axis>();
                return;
            }

            // Add computed column for the selected aggregate
            string aggColName = "Value_" + Enum.GetName(Metric.AggregateType);
            if (!dt.Columns.Contains("SelectedValue"))
            {
                dt.Columns.Add("SelectedValue", typeof(double));
            }

            // Calculate min/max for Y-axis scaling
            double maxValue = 0;
            double minValue = 0;
            foreach (DataRow r in dt.Rows)
            {
                if (r[aggColName] != DBNull.Value)
                {
                    var value = Convert.ToDouble(r[aggColName]);
                    r["SelectedValue"] = value;
                    maxValue = value > maxValue ? value : maxValue;
                    minValue = value < minValue ? value : minValue;
                }
                else
                {
                    r["SelectedValue"] = DBNull.Value;
                }
            }

            // Adjust Y-axis limits
            if (maxValue == 0 && minValue == 0)
            {
                maxValue = 1;
            }
            if (maxValue < 1 && minValue == 0)
            {
                minValue = -maxValue / 2;
            }
            maxValue *= 1.1;

            // Auto-adjust point size based on data count
            var effectiveGeometrySize = dt.Rows.Count > 500 ? 0 : geometrySize;

            // Update chart using ChartHelper
            var config = new ChartConfiguration
            {
                DateColumn = "SnapshotDate",
                MetricColumn = "SelectedValue",
                ChartType = ChartTypes.Line,
                ShowLegend = true,
                LegendPosition = LegendPosition.Hidden,
                LineSmoothness = lineSmoothness,
                LineFill = true,
                GeometrySize = effectiveGeometrySize,
                XAxisMin = (FromDate == DateTime.MinValue ? DateRange.FromUTC : FromDate).ToAppTimeZone(),
                XAxisMax = (ToDate == DateTime.MinValue ? DateRange.ToUTC : ToDate).ToAppTimeZone(),
                YAxisLabel = string.Empty,
                YAxisFormat = "#,##0.######",
                YAxisMin = minValue,
                YAxisMax = maxValue,
                SeriesNames = new System.Collections.Generic.Dictionary<string, string>
                {
                    { "SelectedValue", Metric.CounterName }
                }
            };

            ChartHelper.UpdateChart(chart1, dt, config);
        }

        private DataTable GetPerformanceCounter()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.PerformanceCounter_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cn.Open();

            cmd.Parameters.AddWithValue("InstanceID", InstanceID);
            cmd.Parameters.AddWithValue("FromDate", FromDate == DateTime.MinValue ? DateRange.FromUTC : FromDate);
            cmd.Parameters.AddWithValue("ToDate", ToDate == DateTime.MinValue ? DateRange.ToUTC : ToDate);
            cmd.Parameters.AddWithValue("CounterID", Metric.CounterID);
            cmd.Parameters.AddWithValue("DateGroupingMin", DateGrouping);
            cmd.Parameters.AddWithValue("UTCOffset", DateHelper.UtcOffset);
            if (DateRange.HasDayOfWeekFilter)
            {
                cmd.Parameters.AddWithValue("DaysOfWeek", DateRange.DayOfWeek.AsDataTable());
            }
            if (DateRange.HasTimeOfDayFilter)
            {
                cmd.Parameters.AddWithValue("Hours", DateRange.TimeOfDay.AsDataTable());
            }
            DataTable dt = new();
            da.Fill(dt);
            DateHelper.ConvertUTCToAppTimeZone(ref dt);
            return dt;
        }

        private void PerformanceCounters_Load(object sender, EventArgs e)
        {
            DateHelper.AddDateGroups(tsDateGrouping, TsDateGrouping_Click);
        }

        private void TsDateGrouping_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            DateGrouping = Convert.ToInt32(ts.Tag);
            tsDateGrouping.Text = DateHelper.DateGroupString(DateGrouping);
            dt = GetPerformanceCounter();
            RefreshChart();
        }

        private void TsAgg_Click(object sender, EventArgs e)
        {
            foreach (ToolStripMenuItem itm in tsAgg.DropDownItems)
            {
                itm.Checked = itm == sender;
                if (itm.Checked)
                {
                    Metric.AggregateType = Enum.Parse<AggregateTypes>(((string)itm.Tag)!);
                    tsAgg.Text = itm.Text;
                }
            }
            RefreshChart();
        }

        private void TsClose_Click(object sender, EventArgs e)
        {
            Close?.Invoke(this, EventArgs.Empty);
        }

        private void TsUp_Click(object sender, EventArgs e)
        {
            MoveUp?.Invoke(this, EventArgs.Empty);
        }
    }
}