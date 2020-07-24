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
using LiveCharts.Wpf;
using LiveCharts;
using LiveCharts.Defaults;
using static DBAChecksGUI.Performance.Performance;

namespace DBAChecksGUI.Performance
{
    public partial class IOPerformance : UserControl
    {
        public IOPerformance()
        {
            InitializeComponent();
        }

        Int32 mins;
        DateTime ioTime = DateTime.MinValue;
        DateTime from;
        DateTime to;
        string connectionString;
        Int32 instanceID;
        DateGroup dateGrouping;

        List<LineSeries> series;



        private static DataTable IOStats(Int32 instanceid, DateTime from, DateTime to, string connectionString,DateGroup dateGrouping= DateGroup.None)
        {
            var dt = new DataTable();
            SqlConnection cn = new SqlConnection(connectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand(@"IOStats_Get", cn);
                cmd.Parameters.AddWithValue("@InstanceID", instanceid);
                cmd.Parameters.AddWithValue("@FromDate", from);
                cmd.Parameters.AddWithValue("@ToDate", to);
                cmd.Parameters.AddWithValue("DateGrouping", dateGrouping.ToString().Replace("_",""));
                cmd.CommandType = CommandType.StoredProcedure;
                var da = new SqlDataAdapter(cmd);
                da.Fill(dt);

            }
            return dt;
        }

        class columnMetaData
        {
            public string Alias;
            public bool isVisible;
            public DateTimePoint[] Points;
            public Int32 axis = 0;

        }

        public void RefreshData(Int32 InstanceID, DateTime fromDate, DateTime toDate, string connectionString, DateGroup dateGrouping)
        {
            if (this.dateGrouping != dateGrouping) {
                this.dateGrouping = dateGrouping;
                disableEnableDropdowns();
            }
            this.instanceID = InstanceID;
            this.from = fromDate;
            this.to = toDate;
            this.connectionString = connectionString;
            
            mins = (Int32)toDate.Subtract(fromDate).TotalMinutes;
            refreshData();
            
        }

        private void disableEnableDropdowns()
        {
            foreach(ToolStripMenuItem ts in tsMeasures.DropDownItems)
            {
                ts.Visible= (!(dateGrouping== DateGroup.None && ts.Name.StartsWith("Max")));
                ts.Checked = ts.Enabled ? ts.Checked : false;
            }
        }

        public void RefreshData()
        {
           
            if (DateTime.UtcNow.Subtract(ioTime).TotalMinutes > 30 || dateGrouping !=  DateGroup.None)
            {
                from = DateTime.UtcNow.AddMinutes(-mins);
                to = DateTime.UtcNow.AddMinutes(1);
                refreshData(false);
            }
            else
            {
                from = ioTime.AddSeconds(1);
                to = DateTime.UtcNow.AddMinutes(1);
                refreshData(true);
            }
        }

        private void refreshData(bool update = false)
        {

            string DateFormat = "HH:mm";
            if (mins > 1440)
            {
                DateFormat = "yyyy-MM-dd HH:mm";
            }
            var dt = IOStats(instanceID, from, to, connectionString, dateGrouping);
            var cnt = dt.Rows.Count;

            var columns = new Dictionary<string, columnMetaData>
            {
                {"MBsec", new columnMetaData{Alias="MB/sec",isVisible=true } },
                {"ReadMBsec", new columnMetaData{Alias="Read MB/sec",isVisible=false } },          
                {"WriteMBsec", new columnMetaData{Alias="Write MB/sec",isVisible=false } },          
                {"IOPs", new columnMetaData{Alias="IOPs",isVisible=true,axis=1 } },          
                {"ReadIOPs", new columnMetaData{Alias="Read IOPs",isVisible=false,axis=1 } },
                {"WriteIOPs", new columnMetaData{Alias="Write IOPs",isVisible=false ,axis=1} },
                {"Latency", new columnMetaData{Alias="Latency",isVisible=true,axis=2 } },
                {"ReadLatency", new columnMetaData{Alias="Read Latency",isVisible=false,axis=2} },
                {"WriteLatency", new columnMetaData{Alias="Write Latency",isVisible=false,axis=2 } },
                {"MaxMBsec", new columnMetaData{Alias="Max MB/sec",isVisible=false } },
                {"MaxReadMBsec", new columnMetaData{Alias="Max Read MB/sec",isVisible=false } },
                {"MaxWriteMBsec", new columnMetaData{Alias="Max Write MB/sec",isVisible=false } },
                {"MaxIOPs", new columnMetaData{Alias="Max IOPs",isVisible=false,axis=1 } },
                {"MaxReadIOPs", new columnMetaData{Alias="Max Read IOPs",isVisible=false,axis=1 } },
                {"MaxWriteIOPs", new columnMetaData{Alias="Max Write IOPs",isVisible=false ,axis=1} },
                {"MaxLatency", new columnMetaData{Alias="Max Latency",isVisible=false,axis=1 } },
                {"MaxReadLatency", new columnMetaData{Alias="Max Read Latency",isVisible=false ,axis=1} },
                {"MaxWriteLatency", new columnMetaData{Alias="Max Write Latency",isVisible=false ,axis=1} }
            };

            foreach(ToolStripMenuItem ts in tsMeasures.DropDownItems)
            {
                columns[ts.Name].isVisible = ts.Checked;
            }
            foreach (var s in columns.Keys)
            {
                columns[s].Points = new DateTimePoint[cnt];
            }

            Int32 i = 0;
            foreach (DataRow r in dt.Rows)
            {
                foreach (string s in columns.Keys)
                {
                    var v = r[s] == DBNull.Value ? 0 : (double)(decimal)r[s];
                    ioTime = (DateTime)r["SnapshotDate"];
                    columns[s].Points[i] = new DateTimePoint(ioTime.ToLocalTime(), v );
                }
                i++;
            }

            SeriesCollection sc;
            if (!update)
            {
                sc = new SeriesCollection();
                chartIO.Series = sc;
                foreach (string s in columns.Keys)
                {
                    sc.Add(new LineSeries
                    {
                        Title = columns[s].Alias,
                        Tag = s,
                        ScalesYAt = columns[s].axis,
                        PointGeometrySize = cnt <= 100 ? 10 : 0
                    }
                    );
                }
                chartIO.AxisX.Clear();
                chartIO.AxisY.Clear();
                chartIO.AxisX.Add(new Axis
                {
                    Title = "Time",
                    LabelFormatter = val => new System.DateTime((long)val).ToString(DateFormat)
                });
                chartIO.AxisY.Add(new Axis
                {
                    Title = "MB/sec",
                    LabelFormatter = val => val.ToString("0.0 MB"),
                    MinValue=0
                });
                chartIO.AxisY.Add(new Axis
                {
                    Title = "IOPs",
                    LabelFormatter = val => val.ToString("0.0 IOPs"),
                    Position = AxisPosition.RightTop,
                    MinValue=0
                });
                chartIO.AxisY.Add(new Axis
                {
                    Title = "Latency",
                    LabelFormatter = val => val.ToString("0.0ms"),
                    Position = AxisPosition.RightTop,
                    MinValue=0,
                    MaxValue=200
                });
            }
            else
            {
                sc = chartIO.Series;
            };

            bool addDropdowns = tsMeasures.DropDownItems.Count == 0;
        

            foreach (LineSeries s in chartIO.Series)
            {
                var c = columns[(string)s.Tag];
                if (s.Values == null)
                {
                    var v = new ChartValues<DateTimePoint>();
                    v.AddRange(c.Points);
                    s.Values = v;
                    
                }
                else
                {
                    s.Values.AddRange(c.Points);
                }
                s.Visibility = c.isVisible ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
                if (addDropdowns)
                {                  
                    var dd = new ToolStripMenuItem(c.Alias);
                    dd.Name = (string)s.Tag;
                    dd.CheckOnClick = true;
                    dd.Visible = (!(dd.Name.StartsWith("Max") && dateGrouping ==  DateGroup.None));
                    dd.Checked = dd.Enabled ? c.isVisible : false;
                    dd.Click += measureDropDown_Click;
                    tsMeasures.DropDownItems.Add(dd);
                }
            }
         
        }

        private void measureDropDown_Click(object sender, EventArgs e)
        {
            var dd = (ToolStripMenuItem)sender;
            foreach(LineSeries s in chartIO.Series)
            {
                if((string)s.Tag == dd.Name)
                {
                    s.Visibility = dd.Checked ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
                }
            }
        }
    }
}
