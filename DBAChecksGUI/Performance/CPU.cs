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
using System.Data.SqlClient;
using LiveCharts.Wpf;
using LiveCharts;

namespace DBAChecksGUI.Performance
{
    public partial class CPU : UserControl
    {
        public CPU()
        {
            InitializeComponent();
        }

        DateTime eventTime;
        Int32 mins;

        string connectionString;
        Int32 InstanceID;
        DateTime fromDate;
        DateTime toDate;
        string DateGrouping;

        public void RefreshData(DateTime fromDate, DateTime toDate, string connectionString, Int32 InstanceID,string dateGrouping="None")
        {
            mins = (Int32)toDate.Subtract(fromDate).TotalMinutes;
            this.InstanceID = InstanceID;
            this.fromDate = fromDate;
            this.toDate = toDate;
            this.connectionString = connectionString;
            this.DateGrouping = dateGrouping;
            refreshData(false);
        }

        public void RefreshData()
        {
            fromDate = eventTime.AddSeconds(1);
            toDate = DateTime.UtcNow.AddSeconds(60);
            refreshData(true);
        }

        private void refreshData(bool update = false)
        {
            SqlConnection cn = new SqlConnection(connectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.CPU_Get", cn);
                cmd.Parameters.AddWithValue("@InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("@FromDate", fromDate);
                cmd.Parameters.AddWithValue("@ToDate", toDate);
                cmd.Parameters.AddWithValue("@DateGrouping", DateGrouping);
                cmd.CommandType = CommandType.StoredProcedure;
                var rdr = cmd.ExecuteReader();

                var sqlProcessValues = new ChartValues<DateTimePoint>();
                var otherValues = new ChartValues<DateTimePoint>();

                while (rdr.Read())
                {
                    eventTime = (DateTime)rdr["EventTime"];
                    sqlProcessValues.Add(new DateTimePoint(eventTime.ToLocalTime(), (Int32)rdr["SQLProcessCPU"] / 100.0));
                    otherValues.Add(new DateTimePoint(eventTime.ToLocalTime(), (Int32)rdr["otherCPU"] / 100.0));

                }
                if (update)
                {
                    chartCPU.Series[0].Values.AddRange(sqlProcessValues);
                    chartCPU.Series[1].Values.AddRange(otherValues);
                    while (chartCPU.Series[0].Values.Count > mins)
                    {
                        if (((DateTimePoint)chartCPU.Series[0].Values[0]).DateTime > DateTime.Now.AddMinutes(-mins))
                        {
                            break;
                        }
                        else
                        {
                            chartCPU.Series[0].Values.RemoveAt(0);
                            chartCPU.Series[1].Values.RemoveAt(0);
                        }
                    }
                }
                else
                {
                    SeriesCollection s1 = new SeriesCollection
                    {
                        new StackedAreaSeries
                        {
                            Title="SQL Process",
                            Values = sqlProcessValues
                        },
                        new StackedAreaSeries
                        {
                        Title = "Other",
                        Values = otherValues
                        }
                    };
                    chartCPU.AxisX.Clear();
                    chartCPU.AxisY.Clear();
                    string format = mins < 1440 ? "HH:mm" : "yyyy-MM-dd HH:mm";
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
                    chartCPU.Series = s1;
                }


            }

        }


    }
}
