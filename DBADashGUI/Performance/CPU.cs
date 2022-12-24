using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    public partial class CPU : UserControl, IMetricChart
    {
        public CPU()
        {
            InitializeComponent();
        }

        public bool CloseVisible
        {
            get => tsClose.Visible; set => tsClose.Visible = value;
        }

        public event EventHandler<EventArgs> Close;

        public event EventHandler<EventArgs> MoveUp;

        public Int32 InstanceID { get; set; }
        private Int32 DateGrouping;
        private bool smoothLines = true;
        private int durationMins;

        public Int32 PointSize;

        public bool SmoothLines
        {
            get => smoothLines;
            set
            {
                smoothLines = value;
                foreach (Series s in chartCPU.Series.Cast<Series>())
                {
                    if (s.GetType() == typeof(LineSeries))
                    {
                        ((LineSeries)s).LineSmoothness = smoothLines ? 1 : 0;
                    }
                    else if (s.GetType() == typeof(StackedAreaSeries))
                    {
                        ((StackedAreaSeries)s).LineSmoothness = smoothLines ? 1 : 0;
                    }
                }
            }
        }

        public bool MoveUpVisible
        {
            get => tsUp.Visible; set => tsUp.Visible = value;
        }

        private CPUMetric _metric = new() { AggregateType = IMetric.AggregateTypes.Avg };

        public CPUMetric Metric
        { get => _metric; set { _metric = value; SelectAggregate(); } }

        IMetric IMetricChart.Metric { get => Metric; }

        private void SelectAggregate()
        {
            tsAgg.Text = Enum.GetName(Metric.AggregateType);
            foreach (ToolStripMenuItem mnu in tsAgg.DropDownItems)
            {
                mnu.Checked = (string)mnu.Tag == Enum.GetName(Metric.AggregateType);
            }
        }

        public void RefreshData(int InstanceID)
        {
            this.InstanceID = InstanceID;
            RefreshData();
        }

        public void RefreshData()
        {
            if (durationMins != DateRange.DurationMins) // Update date grouping only if date range has changed
            {
                DateGrouping = DateHelper.DateGrouping(DateRange.DurationMins, 200);
                tsDateGrouping.Text = DateHelper.DateGroupString(DateGrouping);
                durationMins = DateRange.DurationMins;
            }

            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.CPU_Get", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
                cmd.Parameters.AddWithValue("@InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("@FromDate", DateRange.FromUTC);
                cmd.Parameters.AddWithValue("@ToDate", DateRange.ToUTC);
                cmd.Parameters.AddWithValue("@DateGroupingMin", DateGrouping);
                cmd.Parameters.AddWithValue("@UTCOffset", DateHelper.UtcOffset);
                if (DateRange.HasTimeOfDayFilter)
                {
                    cmd.Parameters.AddWithValue("Hours", DateRange.TimeOfDay.AsDataTable());
                }
                if (DateRange.HasDayOfWeekFilter)
                {
                    cmd.Parameters.AddWithValue("DaysOfWeek", DateRange.DayOfWeek.AsDataTable());
                }
                cmd.CommandTimeout = Properties.Settings.Default.CommandTimeout;
                using var rdr = cmd.ExecuteReader();

                var sqlProcessValues = new ChartValues<DateTimePoint>();
                var otherValues = new ChartValues<DateTimePoint>();
                var maxValues = new ChartValues<DateTimePoint>();

                while (rdr.Read())
                {
                    var eventTime = (DateTime)rdr["EventTime"];
                    sqlProcessValues.Add(new DateTimePoint(eventTime.ToAppTimeZone(), Decimal.ToDouble((decimal)rdr["SQLProcessCPU"]) / 100.0));
                    otherValues.Add(new DateTimePoint(eventTime.ToAppTimeZone(), Decimal.ToDouble((decimal)rdr["OtherCPU"]) / 100.0));
                    maxValues.Add(new DateTimePoint(eventTime.ToAppTimeZone(), Decimal.ToDouble((decimal)rdr["MaxCPU"]) / 100.0));
                }

                SeriesCollection s1 = new()
                {
                    new StackedAreaSeries
                    {
                        Title="SQL Process",
                        Values = sqlProcessValues,
                        LineSmoothness = SmoothLines ? 1 : 0
                    },
                    new StackedAreaSeries
                    {
                    Title = "Other",
                    Values = otherValues,
                    LineSmoothness = SmoothLines ? 1 : 0
                    },
                    new LineSeries
                    {
                    Title = "Max CPU",
                    Values = maxValues,
                    LineSmoothness = SmoothLines ? 1 : 0,
                    PointGeometrySize = PointSize,
                    }
                };
                chartCPU.AxisX.Clear();
                chartCPU.AxisY.Clear();
                string format = durationMins < 1440 ? "HH:mm" : "yyyy-MM-dd HH:mm";
                chartCPU.AxisX.Add(new Axis
                {
                    LabelFormatter = val => new System.DateTime((long)val).ToString(format)
                });
                chartCPU.AxisY.Add(new Axis
                {
                    LabelFormatter = val => val.ToString("P1"),
                    MaxValue = 1,
                    MinValue = 0,
                });
                if (maxValues.Count > 1)
                {
                    chartCPU.Series = s1;
                }
                else
                {
                    chartCPU.Series.Clear();
                }
                UpdateVisibility();
            }
        }

        private void UpdateVisibility()
        {
            if (chartCPU.Series.Count == 3)
            {
                ((StackedAreaSeries)chartCPU.Series[0]).Visibility = AVGToolStripMenuItem.Checked ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
                ((StackedAreaSeries)chartCPU.Series[1]).Visibility = AVGToolStripMenuItem.Checked ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
                ((LineSeries)chartCPU.Series[2]).Visibility = MAXToolStripMenuItem.Checked ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
            }
        }

        private void AVGToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MAXToolStripMenuItem.Checked = (!AVGToolStripMenuItem.Checked);
            Metric.AggregateType = AVGToolStripMenuItem.Checked ? IMetric.AggregateTypes.Avg : IMetric.AggregateTypes.Max;
            tsAgg.Text = Enum.GetName(Metric.AggregateType);
            UpdateVisibility();
        }

        private void MAXToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AVGToolStripMenuItem.Checked = (!MAXToolStripMenuItem.Checked);
            Metric.AggregateType = AVGToolStripMenuItem.Checked ? IMetric.AggregateTypes.Avg : IMetric.AggregateTypes.Max;
            tsAgg.Text = Enum.GetName(Metric.AggregateType);
            UpdateVisibility();
        }

        private void CPU_Load(object sender, EventArgs e)
        {
            DateHelper.AddDateGroups(tsDateGrouping, TsDateGrouping_Click);
        }

        private void TsDateGrouping_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            DateGrouping = Convert.ToInt32(ts.Tag);
            tsDateGrouping.Text = DateHelper.DateGroupString(DateGrouping);
            RefreshData();
        }

        private void TsClose_Click(object sender, EventArgs e)
        {
            Close.Invoke(this, new EventArgs());
        }

        private void TsUp_Click(object sender, EventArgs e)
        {
            MoveUp.Invoke(this, new EventArgs());
        }
    }
}