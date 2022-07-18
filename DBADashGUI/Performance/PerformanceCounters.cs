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

namespace DBADashGUI.Performance
{
    public partial class PerformanceCounters : UserControl
    {
        public PerformanceCounters()
        {
            InitializeComponent();
        }

        public Int32 InstanceID { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public Int32 CounterID { get; set; }
        public string CounterName { get; set; }

        public bool SmoothLines = false;
        public Int32 PointSize = 5;

        private int durationMins;
        string agg = "Value_Avg";

        Int32 DateGrouping;
        DataTable dt;

        public void RefreshData()
        {
            this.InstanceID = InstanceID;
            setDateGroup(DateRange.DurationMins);
            dt = GetPerformanceCounter();
            refreshChart();
        }

        private void setDateGroup(int mins)
        {
            if (durationMins != mins) // Change dategroup only if date range has changed.
            {
                DateGrouping = Common.DateGrouping(mins, 200);
                tsDateGrouping.Text = Common.DateGroupString(DateGrouping);
                durationMins = mins;
            }
        }

        private void refreshChart()
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
                var value = Convert.ToDouble(r[agg]);
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
            
            SeriesCollection s1 = new SeriesCollection
                    {
                        new LineSeries
                        {
                        Title = CounterName,
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
            chart1.Text = CounterName;

        }

        private DataTable GetPerformanceCounter()
        {           
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.PerformanceCounter_Get", cn) {  CommandType = CommandType.StoredProcedure })
            using(var da = new SqlDataAdapter(cmd))
            {
                cn.Open();

                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("FromDate", FromDate);
                cmd.Parameters.AddWithValue("ToDate", ToDate);
                cmd.Parameters.AddWithValue("CounterID", CounterID);
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
                DataTable dt = new DataTable();
                da.Fill(dt);
                Common.ConvertUTCToLocal(ref dt);
                return dt;
            }           
        }

        private void PerformanceCounters_Load(object sender, EventArgs e)
        {
            Common.AddDateGroups(tsDateGrouping, tsDateGrouping_Click);
        }

        private void tsDateGrouping_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            DateGrouping = Convert.ToInt32(ts.Tag);
            tsDateGrouping.Text = Common.DateGroupString(DateGrouping);
            dt = GetPerformanceCounter();
            refreshChart();
        }

        private void tsAgg_Click(object sender, EventArgs e)
        {
            foreach(ToolStripMenuItem itm in tsAgg.DropDownItems)
            {
                itm.Checked = itm == sender;
                if (itm.Checked)
                {
                    agg = (string)itm.Tag;
                    tsAgg.Text = itm.Text;
                }
            }
            refreshChart();
        }
    }
}
