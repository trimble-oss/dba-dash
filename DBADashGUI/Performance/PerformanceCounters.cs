using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using static DBADashGUI.Performance.Performance;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
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
        public bool MoveUpVisible
        {
            get
            {
                return tsUp.Visible;
            }
            set
            {
                tsUp.Visible = value;
            }
        }

        public Int32 InstanceID { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public int CounterID { get => Metric.CounterID; set => Metric.CounterID = value; }
        public string CounterName { get=>Metric.CounterName; set=>Metric.CounterName=value; }

        private PerformanceCounterMetric _metric = new();
        public PerformanceCounterMetric Metric { get=>_metric; set { _metric = value; SelectAggregate(); } } 
        IMetric IMetricChart.Metric { get => Metric; }

        public bool SmoothLines = false;
        public Int32 PointSize = 5;

        private int durationMins;
       

        Int32 DateGrouping;
        DataTable dt;

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

        public void RefreshData(int InstanceID)
        {
            this.InstanceID = InstanceID;
            RefreshData();
        }

        private void SetDateGroup(int mins)
        {
            if (durationMins != mins) // Change dategroup only if date range has changed.
            {
                DateGrouping = Common.DateGrouping(mins, 200);
                tsDateGrouping.Text = Common.DateGroupString(DateGrouping);
                durationMins = mins;
            }
        }

        private void RefreshChart()
        {
            chart1.AxisX.Clear();
            chart1.AxisY.Clear();
            chart1.Series = null;
            
            var values = new ChartValues<DateTimePoint>();
            double maxValue = 0;
            double minValue = 0;
            if (dt.Rows.Count < 2)
            {
                return;
            }
            foreach (DataRow r in dt.Rows)
            {
                string aggColName= "Value_" + Enum.GetName(Metric.AggregateType);
                
                var value = Convert.ToDouble(r[aggColName]);
                maxValue = value > maxValue ? value : maxValue;
                minValue = value < minValue ? value : minValue;
                values.Add(new DateTimePoint((DateTime)r["SnapshotDate"],value ));
            }
            Int32 pointSize = PointSize;
            if (dt.Rows.Count > 500)
            {
                pointSize= 0;
            }
            if(maxValue==0 && minValue == 0)
            {
                maxValue = 1;
            }
            if (maxValue < 1 && minValue==0)
            {
                minValue = -maxValue/2;
            }
            maxValue *= 1.1;
            
            SeriesCollection s1 = new()
            {
                        new LineSeries
                        {
                        Title = Metric.CounterName,
                        Values =values,
                        LineSmoothness = SmoothLines ? 1 : 0,
                        PointGeometrySize = pointSize,
                        }
                    };
            
     
            string format = "yyyy-MM-dd HH:mm";
            chart1.AxisX.Add(new Axis
            {
                LabelFormatter = val => new System.DateTime((long)val).ToString(format)
            });
            chart1.AxisY.Add(new Axis
            {
                LabelFormatter = val => val.ToString("#,##0.######"),
                MaxValue=maxValue,
                MinValue=minValue

            });
            chart1.Series = s1;
            chart1.LegendLocation = LegendLocation.Top;
            chart1.Text = Metric.CounterName;

        }

        private DataTable GetPerformanceCounter()
        {           
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.PerformanceCounter_Get", cn) {  CommandType = CommandType.StoredProcedure })
            using(var da = new SqlDataAdapter(cmd))
            {
                cn.Open();

                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("FromDate", FromDate == DateTime.MinValue ? DateRange.FromUTC : FromDate);
                cmd.Parameters.AddWithValue("ToDate", ToDate == DateTime.MinValue ? DateRange.ToUTC : ToDate);
                cmd.Parameters.AddWithValue("CounterID", Metric.CounterID);
                cmd.Parameters.AddWithValue("DateGroupingMin", DateGrouping);
                cmd.Parameters.AddWithValue("UTCOffset", Common.UtcOffset);
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
                Common.ConvertUTCToLocal(ref dt);
                return dt;
            }           
        }

        private void PerformanceCounters_Load(object sender, EventArgs e)
        {
            Common.AddDateGroups(tsDateGrouping, TsDateGrouping_Click);
        }

        private void TsDateGrouping_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            DateGrouping = Convert.ToInt32(ts.Tag);
            tsDateGrouping.Text = Common.DateGroupString(DateGrouping);
            dt = GetPerformanceCounter();
            RefreshChart();           
        }

        private void TsAgg_Click(object sender, EventArgs e)
        {
            foreach(ToolStripMenuItem itm in tsAgg.DropDownItems)
            {
                itm.Checked = itm == sender;
                if (itm.Checked)
                {
                    Metric.AggregateType = Enum.Parse<AggregateTypes>((string)itm.Tag);
                    tsAgg.Text = itm.Text;
                }
            }
            RefreshChart();
        }

        private void TsClose_Click(object sender, EventArgs e)
        {
            Close.Invoke(this,new EventArgs());
        }

        private void TsUp_Click(object sender, EventArgs e)
        {
            MoveUp.Invoke(this, new EventArgs());
        }
    }
}
