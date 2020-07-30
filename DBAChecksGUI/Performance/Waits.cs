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
using LiveCharts.Configurations;
using LiveCharts.Wpf;
using LiveCharts.Defaults;
using LiveCharts;
using static DBAChecksGUI.Performance.Performance;

namespace DBAChecksGUI.Performance
{
    public partial class Waits : UserControl
    {
        public Waits()
        {
            InitializeComponent();
        }

        public class DateModel
        {
            public System.DateTime DateTime { get; set; }
            public double Value { get; set; }
        }

        public DateTimePoint x;
        Int32 instanceID;
        DateTime lastWait = DateTime.MinValue;
        string connectionString;
        DateGroup dateGrouping;
        DateTime from;
        DateTime to;
        Int32 mins;

        public void RefreshData()
        {
            if (lastWait != DateTime.MinValue && dateGrouping == DateGroup._1MIN)
            {
                this.to = DateTime.UtcNow.AddMinutes(1);
                this.from = lastWait.AddSeconds(1);
                refreshData(true);
            }
        }
        public void RefreshData(Int32 instanceID, DateTime from, DateTime to, string connectionString, DateGroup dateGrouping = DateGroup.None)
        {
            this.instanceID = instanceID;
            this.connectionString = connectionString;
            this.from = from;
            this.to = to;
            mins = (Int32)to.Subtract(from).TotalMinutes;
            this.dateGrouping = dateGrouping;
            refreshData(false);
        }


         private void refreshData(bool update)
        {

            if (!update)
            {
                waitChart.Series.Clear();
                waitChart.AxisX.Clear();
                waitChart.AxisY.Clear();
                lastWait = DateTime.MinValue;
            }
         

            SqlConnection cn = new SqlConnection(connectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.Waits_Get", cn);
                cmd.Parameters.AddWithValue("InstanceID", instanceID);
                cmd.Parameters.AddWithValue("FromDate", from);
                cmd.Parameters.AddWithValue("ToDate", to);
                cmd.Parameters.AddWithValue("DateGrouping", dateGrouping.ToString().Replace("_",""));
                cmd.CommandType = CommandType.StoredProcedure;
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                if(dt.Rows.Count == 0)
                {
                    return;
                }
                var dPoints = new Dictionary<string, ChartValues<DateTimePoint>>();
                string current = string.Empty;
                ChartValues<DateTimePoint> values = new ChartValues<DateTimePoint>();               
                foreach (DataRow r in dt.Rows){
                    var waitType = (string)r["WaitType"];
                    var time = (DateTime)r["Time"];
                    if (time > lastWait)
                    {
                        lastWait = time;
                    }
                    if (current!= waitType)
                    {
                        if (values.Count > 0) { dPoints.Add(current, values); }
                        values = new ChartValues<DateTimePoint>();
                        current = waitType;
                    }
                    values.Add(new DateTimePoint(((DateTime)r["Time"]).ToLocalTime(), (double)(decimal)r["WaitTimeMsPerSec"]));
                }
                if (values.Count > 0)
                {
                    dPoints.Add(current, values);
                    values = new ChartValues<DateTimePoint>();
                }


                Int32 fromMins = 1;
                if(dateGrouping == DateGroup.DAY)
                {
                    fromMins = 1440;
                }
                else if( dateGrouping == DateGroup.None || dateGrouping == DateGroup._1MIN)
                {
                    fromMins = 1;
                }
                else if (dateGrouping == DateGroup._10MIN)
                {
                    fromMins = 10;
                }
                else if(dateGrouping == DateGroup._60MIN)
                {
                    fromMins = 60;
                }
                else if (dateGrouping == DateGroup._120MIN)
                {
                    fromMins = 120;
                }
                else
                {
                    throw new NotImplementedException("dateGrouping");
                }

                if (update)
                {
                     List<string> existingTitles = new List<string>();
                    foreach(StackedColumnSeries s in waitChart.Series)
                    {
                        existingTitles.Add(s.Title);
                        if (dPoints.ContainsKey(s.Title))
                        {
                            values = dPoints[s.Title];
                            s.Values.AddRange(values);
                        }
                     
                        while(s.Values.Count>0 && DateTime.Now.Subtract(((DateTimePoint)s.Values[0]).DateTime).TotalMinutes > mins)
                        {
                            s.Values.RemoveAt(0);
                        }
                    }
                    foreach (var x in dPoints)
                    {
                        if (!existingTitles.Contains(x.Key))
                        {
                            waitChart.Series.Add(new StackedColumnSeries
                            {
                                Title = x.Key,
                                Values = x.Value
                            });
                        }
                    }
                }
                else
                {
                    CartesianMapper<DateTimePoint> dayConfig = Mappers.Xy<DateTimePoint>()
.X(dateModel => dateModel.DateTime.Ticks / TimeSpan.FromMinutes(fromMins).Ticks)
.Y(dateModel => dateModel.Value);


                    SeriesCollection s1 = new SeriesCollection(dayConfig);
                    foreach (var x in dPoints)
                    {
                        s1.Add(new StackedColumnSeries
                        {
                            Title = x.Key,
                            Values = x.Value
                        });
                    }
                    waitChart.Series = s1;

                    string format = "t";
                    if (fromMins >= 1440)
                    {
                        format = "yyyy-MM-dd";
                    }
                    else if (mins >= 1440)
                    {
                        format = "yyyy-MM-dd HH:mm";
                    }
                    waitChart.AxisX.Add(new Axis
                    {
                        LabelFormatter = value => new DateTime((long)(value * TimeSpan.FromMinutes(fromMins).Ticks)).ToString(format)
                    });
                    waitChart.AxisY.Add(new Axis
                    {
                        LabelFormatter = val => val.ToString("0ms/sec")

                    });

                }

            }
        }
    }
}
