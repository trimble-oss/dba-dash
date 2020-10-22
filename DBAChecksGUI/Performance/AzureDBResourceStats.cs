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
using LiveCharts.Defaults;
using LiveCharts;
using LiveCharts.Wpf;
using static DBAChecksGUI.Performance.Performance;

namespace DBAChecksGUI.Performance
{
    public partial class AzureDBResourceStats : UserControl
    {

        DateTime FromDate;
        DateTime ToDate;
        public Int32 InstanceID;
        public string ElasticPoolName = String.Empty;
        Int32 Mins=60;

        public void SetDateRange(DateTime from,DateTime to)
        {
            this.FromDate = from;
            this.ToDate = to;
            this.Mins = -1;
            checkTime();
        }

        public void SetMins(Int32 mins)
        {
            this.Mins = mins;
            FromDate = DateTime.MinValue;
            ToDate = DateTime.MinValue;
            checkTime();
        }



        DateTime from
        {
            get
            {
                if (Mins > 0)
                {
                    return DateTime.Now.AddMinutes(-Mins);
                }
                else
                {
                    return FromDate;
                }
            }
        }
        DateTime to
        {
            get
            {
                if (Mins > 0)
                {
                    return DateTime.Now;
                }
                else
                {
                    return ToDate;
                }
            }
        }

        public AzureDBResourceStats()
        {
            InitializeComponent();
        }

        bool SmoothLines { 
            get {
                return smoothLinesToolStripMenuItem.Checked;
            }
            set { smoothLinesToolStripMenuItem.Checked = value; 
            }
        }
        Int32 pointSize
        {
            get
            {
                if (pointsToolStripMenuItem.Checked)
                {
                    return 10;
                }
                else
                {
                    return 0;
                }
            }
        }

        string DateFormat
        {
            get
            {
                DateGroup dg = DateGrouping;
                if(dg== DateGroup.None)
                {
                    return "yyyy-MM-dd HH:mm:ss";
                }
                else if(dg== DateGroup.DAY)
                {
                    return "yyyy-MM-dd";
                }
                else
                {
                    return "yyyy-MM-dd HH:mm";
                }
               
            }
        }

        public DateGroup DateGrouping
        {
            get
            {
                var Mins = to.Subtract(from).TotalMinutes;
                if (Mins < 61)
                {
                    return DateGroup.None;
                }
                else if (Mins < 181)
                {
                    return DateGroup._1MIN;
                }
                if (Mins < 2881)
                {
                    return DateGroup._10MIN;
                }
                if (Mins < 11520)
                {
                    return DateGroup._60MIN;
                }
                if (Mins < 28800)
                {
                    return DateGroup._120MIN;
                }
                return DateGroup.DAY;
            }
        }

        Dictionary<string, columnMetaData> columns;

         Dictionary<string,columnMetaData> DBColumns = new Dictionary<string, columnMetaData>
            {
                {"avg_cpu_percent", new columnMetaData{Alias="Avg CPU %",isVisible=false } },
                {"avg_data_io_percent", new columnMetaData{Alias="Avg Data %",isVisible=false } },
                {"avg_log_write_percent", new columnMetaData{Alias="Avg Log Write %",isVisible=false } },
                {"AvgDTUPercent", new columnMetaData{Alias="Avg DTU %",isVisible=false } },
                {"avg_memory_usage_percent", new columnMetaData{Alias="Avg Memory %",isVisible=false } },
                {"xtp_storage_percent", new columnMetaData{Alias="XTP Storage %",isVisible=false } },
                {"avg_instance_cpu_percent", new columnMetaData{Alias="Avg CPU % (Instance)",isVisible=false } },
                {"avg_instance_memory_percent", new columnMetaData{Alias="Avg Memory % (Instance)",isVisible=false } },
                {"max_worker_percent", new columnMetaData{Alias="Max Worker %",isVisible=false } },
                {"max_session_percent", new columnMetaData{Alias="Max Session %",isVisible=false } },
                {"max_cpu_percent", new columnMetaData{Alias="Max CPU %",isVisible=true } },
                {"max_data_io_percent", new columnMetaData{Alias="Max Data %",isVisible=true } },
                {"max_log_write_percent", new columnMetaData{Alias="Max Log Write %",isVisible=true } },
                {"max_instance_cpu_percent", new columnMetaData{Alias="Max CPU % (Instance)",isVisible=false } },
                {"max_instance_memory_percent", new columnMetaData{Alias="Max Memory % (Instance)",isVisible=false } },
                {"MaxDTUPercent", new columnMetaData{Alias="Max DTU %",isVisible=false} },
                {"AvgDTUsUsed", new columnMetaData{Alias="Avg DTU",isVisible=false,axis=1} },
                {"MaxDTUsUsed", new columnMetaData{Alias="Max DTU",isVisible=false,axis=1} },
                {"dtu_limit", new columnMetaData{Alias="DTU Limit",isVisible=false,axis=1} },
                {"cpu_limit", new columnMetaData{Alias="CPU Limit",isVisible=false,axis=1} },
            };

        Dictionary<string, columnMetaData> PoolColumns = new Dictionary<string, columnMetaData>
            {
                {"avg_cpu_percent", new columnMetaData{Alias="Avg CPU %",isVisible=false } },
                {"avg_data_io_percent", new columnMetaData{Alias="Avg Data %",isVisible=false } },
                {"avg_log_write_percent", new columnMetaData{Alias="Avg Log Write %",isVisible=false } },
                {"AvgDTUPercent", new columnMetaData{Alias="Avg DTU %",isVisible=false } },              
                {"max_worker_percent", new columnMetaData{Alias="Max Worker %",isVisible=false } },
                {"max_session_percent", new columnMetaData{Alias="Max Session %",isVisible=false } },
                {"max_cpu_percent", new columnMetaData{Alias="Max CPU %",isVisible=true } },
                {"max_data_io_percent", new columnMetaData{Alias="Max Data %",isVisible=true } },
                {"max_log_write_percent", new columnMetaData{Alias="Max Log Write %",isVisible=true } },
                {"MaxDTUPercent", new columnMetaData{Alias="Max DTU %",isVisible=false} },
                {"AvgDTUsUsed", new columnMetaData{Alias="Avg DTU",isVisible=false,axis=1} },
                {"MaxDTUsUsed", new columnMetaData{Alias="Max DTU",isVisible=false,axis=1} },
                {"dtu_limit", new columnMetaData{Alias="DTU Limit",isVisible=false,axis=1} }
            };


        DataTable dt;

        public void RefreshData()
        {
            if (ElasticPoolName == string.Empty)
            {
                dt = GetAzureDBResourceStats();
            }
            else
            {
                dt = GetAzurePoolResourceStats();
            }

            updateChart();
        }

        private void updateChart()
        {
            var cnt = dt.Rows.Count;
            if (cnt < 2)
            {
                chart1.Visible = false;
                return;
            }
            else
            {
                chart1.Visible = true;
            }
            foreach (var s in columns.Keys)
            {
                columns[s].Points = new DateTimePoint[cnt];
            }

            Int32 i = 0;
            Int32 y1Max=1;
            foreach (DataRow r in dt.Rows)
            {
                var dtuLimit = r["dtu_limit"] ==DBNull.Value ? 0 : (Int32)r["dtu_limit"];
                if (ElasticPoolName == string.Empty)
                {
                    var cpuLimit = r["cpu_Limit"] == DBNull.Value ? 0 : Convert.ToInt32(r["cpu_limit"]);
                    y1Max = cpuLimit > y1Max ? cpuLimit : y1Max;
                }

                y1Max = dtuLimit > y1Max ? dtuLimit : y1Max;
                foreach (var kv in columns)
                {
                    if (kv.Value.isVisible)
                    {
                        var v = r[kv.Key] == DBNull.Value ? 0 : Convert.ToDouble(r[kv.Key]);
                        var endtime = (DateTime)r["end_time"];
                        columns[kv.Key].Points[i] = new DateTimePoint(endtime, v);
                    }
                }
                i++;
            }

            var sc = new SeriesCollection();
            chart1.Series = sc;
            foreach (var kv in columns)
            {
                if (kv.Value.isVisible)
                {
                    var v = new ChartValues<DateTimePoint>();
                    v.AddRange(columns[kv.Key].Points);
                    sc.Add(new LineSeries
                    {
                        Title = columns[kv.Key].Alias,
                        Tag = kv.Key,
                        ScalesYAt = columns[kv.Key].axis,
                        LineSmoothness = SmoothLines ? 1 : 0,
                        PointGeometrySize = pointSize,
                        Values = v
                    }
                    ); 
                }
            }
            chart1.AxisX.Clear();
            chart1.AxisY.Clear();
            chart1.AxisX.Add(new Axis
            {
                Title = "Time",
                LabelFormatter = val => new System.DateTime((long)val).ToString(DateFormat)

            });
            var y0visible = columns.Values.Where(c => c.isVisible && c.axis == 0).Count() > 0;
            chart1.AxisY.Add(new Axis
            {
                Title = "%",
                LabelFormatter = val => val.ToString("0.0"),
                MinValue = 0,
                MaxValue = 100,
                Visibility = y0visible ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden

            });
            var y1visible = columns.Values.Where(c => c.isVisible && c.axis == 1).Count() > 0;
            if (y1visible)
            {
                chart1.AxisY.Add(new Axis
                {
                    Title = "DTU",
                    LabelFormatter = val => val.ToString("0"),
                    Position = AxisPosition.RightTop,
                    MinValue = 0,
                    MaxValue = y1Max

                });
            }
            chart1.LegendLocation = LegendLocation.Bottom;
        }

        private void addMeasures()
        {
            foreach(var k in columns)
            {
                var dd = new ToolStripMenuItem(k.Value.Alias);
                dd.Name = (string)k.Key;
                dd.CheckOnClick = true;
                dd.Checked = dd.Enabled ? k.Value.isVisible : false;
                dd.Click += measureDropDown_Click;
                tsMeasures.DropDownItems.Add(dd);
            }
     }

        private void measureDropDown_Click(object sender, EventArgs e)
        {
            var mnu = (ToolStripMenuItem)sender;
            columns[mnu.Name].isVisible = mnu.Checked;
            updateChart();
        }

        private DataTable GetAzurePoolResourceStats()
        {
            SqlConnection cn = new SqlConnection(Common.ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.AzureDBElasticPoolResourceStats_Get", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("FromDate", from);
                cmd.Parameters.AddWithValue("ToDate", to);
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("elastic_pool_name", ElasticPoolName);
                cmd.Parameters.AddWithValue("DateGrouping", DateGrouping.ToString().Replace("_", ""));
                cmd.Parameters.AddWithValue("UTCOffset", Common.UtcOffset);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }


        private DataTable GetAzureDBResourceStats()
        {
            SqlConnection cn = new SqlConnection(Common.ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.AzureDBResourceStats_Get", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("FromDate", from);
                cmd.Parameters.AddWithValue("ToDate",to);
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("DateGrouping", DateGrouping.ToString().Replace("_", ""));
                cmd.Parameters.AddWithValue("UTCOffset",Common.UtcOffset);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        private void tsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void AzureDBResourceStats_Load(object sender, EventArgs e)
        {
            if (ElasticPoolName == string.Empty)
            {
                columns = DBColumns;
            }
            else
            {
                columns = PoolColumns;
            }
            addMeasures();
           
        }

        private void smoothLinesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            updateChart();
        }

        private void tsTime_Click(object sender, EventArgs e)
        {
            var itm = (ToolStripMenuItem)sender;
            SetMins(Convert.ToInt32(itm.Tag));

            RefreshData();
        }

        private void checkTime()
        {
            foreach (var ts in tsTime.DropDownItems)
            {
                if (ts.GetType() == typeof(ToolStripMenuItem))
                {
                    var mnu = (ToolStripMenuItem)ts;
                    mnu.Checked = Convert.ToInt32(mnu.Tag) == Mins;
                }
            }
            if (FromDate > DateTime.MinValue)
            {
                tsCustom.Checked = true;
            }
        }

        private void tsCustom_Click(object sender, EventArgs e)
        {
            var frm = new CustomTimePicker();
            frm.FromDate = from;
            frm.ToDate = to;
            frm.ShowDialog();
            if(frm.DialogResult == DialogResult.OK)
            {
                SetDateRange(frm.FromDate, frm.ToDate);   
                RefreshData();
            }
        }

        private void pointsToolStripMenuItem_Click(object sender, EventArgs e)
        {
                foreach (LineSeries s in chart1.Series)
                {
                    s.PointGeometrySize = pointSize;
                }
         }
        
    }
}
