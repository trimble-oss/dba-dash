using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using static DBAChecksGUI.Performance.Performance;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;

namespace DBAChecksGUI.Performance
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

        public void RefreshData()
        {
            chart1.AxisX.Clear();
            chart1.AxisY.Clear();
            chart1.Series = null;
            var dt = GetPerformanceCounter();
            var values = new ChartValues<DateTimePoint>();
            double maxValue = 0;
            double minValue = 0;
            if (dt.Rows.Count < 2)
            {
                return;
            }
            foreach (DataRow r in dt.Rows)
            {
                var value = Convert.ToDouble(r["Value"]);
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
            var cn = new SqlConnection(Common.ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.PerformanceCounter_Get", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("FromDate", FromDate);
                cmd.Parameters.AddWithValue("ToDate", ToDate);
                cmd.Parameters.AddWithValue("CounterID", CounterID);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }
    }
}
