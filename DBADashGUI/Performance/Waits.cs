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
using LiveCharts.Configurations;
using LiveCharts.Wpf;
using LiveCharts.Defaults;
using LiveCharts;
using static DBADashGUI.Performance.Performance;
using System.Xml.Schema;

namespace DBADashGUI.Performance
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
        private Int32 dateGrouping;

        DateTime from;
        DateTime to;
        Int32 mins;
        string _waitType;

        public string WaitType
        {
            get
            {
                return _waitType;
            }
            set
            {
                tsFilter.Text = value == "" ? (criticalWaitsOnlyToolStripMenuItem.Checked ? "*Critical Waits*":"") : value;
                _waitType = value;
            }
        }

        public void RefreshData()
        {
            if (lastWait != DateTime.MinValue && dateGrouping == 1)
            {
                this.to = DateTime.UtcNow.AddMinutes(1);
                this.from = lastWait.AddSeconds(1);
                refreshData(true);
            }
        }
        public void RefreshData(Int32 instanceID, DateTime from, DateTime to, string connectionString)
        {
            this.instanceID = instanceID;
            this.connectionString = connectionString;
            mins = (Int32)to.Subtract(from).TotalMinutes;
            if(this.from!=from || this.to!=to){
                dateGrouping = Common.DateGrouping(mins, 35);
                if (dateGrouping < 1)
                {
                    dateGrouping = 1;
                }
                tsDateGrouping.Text = Common.DateGroupString(dateGrouping);
            }
            this.from = from;
            this.to = to;
        
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
                cmd.Parameters.AddWithValue("DateGroupingMin", dateGrouping);
                cmd.Parameters.AddWithValue("CriticalWaitsOnly", criticalWaitsOnlyToolStripMenuItem.Checked);
                if (_waitType != null && _waitType.Length > 0)
                {
                    cmd.Parameters.AddWithValue("WaitType", _waitType);
                }
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = Properties.Settings.Default.CommandTimeout;
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
.X(dateModel => dateModel.DateTime.Ticks / TimeSpan.FromMinutes(dateGrouping ==0 ? 1 : dateGrouping).Ticks)
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
                    if (dateGrouping >= 1440)
                    {
                        format = "yyyy-MM-dd";
                    }
                    else if (mins >= 1440)
                    {
                        format = "yyyy-MM-dd HH:mm";
                    }
                    waitChart.AxisX.Add(new Axis
                    {
                        LabelFormatter = value => new DateTime((long)(value * TimeSpan.FromMinutes(dateGrouping == 0 ? 1 : dateGrouping).Ticks)).ToString(format)
                    });
                    waitChart.AxisY.Add(new Axis
                    {
                        LabelFormatter = val => val.ToString("0ms/sec")

                    });

                }

            }
        }

        private void Waits_Load(object sender, EventArgs e)
        {
            Common.AddDateGroups(tsDateGrouping, TsDateGrouping_Click);
        }

        private void TsDateGrouping_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            dateGrouping = Convert.ToInt32(ts.Tag);
            tsDateGrouping.Text = Common.DateGroupString(dateGrouping);
            refreshData(false);
        }

        private void tsFilterWaitType_Click(object sender, EventArgs e)
        {
            string wt = _waitType;
            if (Common.ShowInputDialog(ref wt,"Wait Type (LIKE):") == DialogResult.OK)
            {
                WaitType = wt.EndsWith("%") || wt.Length==0 ? wt : wt+"%";
                refreshData(false);
            }
        }

        private void stringFilterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string wt = _waitType;
            if (Common.ShowInputDialog(ref wt, "Wait Type (LIKE):") == DialogResult.OK)
            {
                WaitType = wt.EndsWith("%") || wt.Length == 0 ? wt : wt + "%";
                criticalWaitsOnlyToolStripMenuItem.Checked = false;
                refreshData(false);
            }
        }

        private void criticalWaitsOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            WaitType = "";
            refreshData(false);
        }
    }
}
