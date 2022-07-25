using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LiveCharts.Defaults;
using Microsoft.Data.SqlClient;
using LiveCharts.Wpf;
using LiveCharts;
using static DBADashGUI.Performance.Performance;

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
            get
            {
                return tsClose.Visible;
            }
            set
            {
                tsClose.Visible = value;
            }
        }

        public event EventHandler<EventArgs> Close;
        public event EventHandler<EventArgs> MoveUp;

        public Int32 InstanceID { get; set; }
        Int32 DateGrouping;
        bool smoothLines = true;
        private int durationMins;

        public Int32 PointSize;

        public bool SmoothLines
        {
            get
            {
                return smoothLines;
            }
            set
            {
                smoothLines = value;
                foreach (Series s in chartCPU.Series)
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

        public bool MoveUpVisible { 
            get { 
                return tsUp.Visible; 
            } 
            set { 
                tsUp.Visible = value; 
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
                DateGrouping = Common.DateGrouping(DateRange.DurationMins, 200);
                tsDateGrouping.Text = Common.DateGroupString(DateGrouping);
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
                cmd.Parameters.AddWithValue("@UTCOffset", Common.UtcOffset);
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
                    sqlProcessValues.Add(new DateTimePoint(eventTime.ToLocalTime(), Decimal.ToDouble((decimal)rdr["SQLProcessCPU"]) / 100.0));
                    otherValues.Add(new DateTimePoint(eventTime.ToLocalTime(), Decimal.ToDouble((decimal)rdr["OtherCPU"]) / 100.0));
                    maxValues.Add(new DateTimePoint(eventTime.ToLocalTime(), Decimal.ToDouble((decimal)rdr["MaxCPU"]) / 100.0));

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
            UpdateVisibility();
        }

        private void MAXToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AVGToolStripMenuItem.Checked = (!MAXToolStripMenuItem.Checked);
            UpdateVisibility();
        }

        private void CPU_Load(object sender, EventArgs e)
        {
            Common.AddDateGroups(tsDateGrouping, TsDateGrouping_Click);

        }

        private void TsDateGrouping_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            DateGrouping = Convert.ToInt32(ts.Tag);
            tsDateGrouping.Text = Common.DateGroupString(DateGrouping);
            RefreshData();

        }

        private void tsClose_Click(object sender, EventArgs e)
        {
            Close.Invoke(this, new EventArgs());
        }

        private void tsUp_Click(object sender, EventArgs e)
        {
            MoveUp.Invoke(this, new EventArgs());
        }


    }
}
