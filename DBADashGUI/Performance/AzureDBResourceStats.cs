using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    public partial class AzureDBResourceStats : UserControl
    {

        public Int32 InstanceID;
        public string ElasticPoolName = String.Empty;

        public AzureDBResourceStats()
        {
            InitializeComponent();
        }

        bool SmoothLines
        {
            get
            {
                return smoothLinesToolStripMenuItem.Checked;
            }
            set
            {
                smoothLinesToolStripMenuItem.Checked = value;
            }
        }
        Int32 PointSize
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
                if (DateGrouping == 0)
                {
                    return "yyyy-MM-dd HH:mm:ss";
                }
                else if (DateGrouping >= 1440)
                {
                    return "yyyy-MM-dd";
                }
                else
                {
                    return "yyyy-MM-dd HH:mm";
                }

            }
        }



        Int32 _dateGrouping = 0;
        public Int32 DateGrouping
        {
            get
            {
                return _dateGrouping;
            }
            set
            {
                _dateGrouping = value;
                tsDateGrouping.Text = Common.DateGroupString(_dateGrouping);
            }

        }



        Dictionary<string, ColumnMetaData> columns;
        readonly Dictionary<string, ColumnMetaData> DBColumns = new()
        {
                {"avg_cpu_percent", new ColumnMetaData{Alias="Avg CPU %",isVisible=false } },
                {"avg_data_io_percent", new ColumnMetaData{Alias="Avg Data %",isVisible=false } },
                {"avg_log_write_percent", new ColumnMetaData{Alias="Avg Log Write %",isVisible=false } },
                {"AvgDTUPercent", new ColumnMetaData{Alias="Avg DTU %",isVisible=false } },
                {"avg_memory_usage_percent", new ColumnMetaData{Alias="Avg Memory %",isVisible=false } },
                {"xtp_storage_percent", new ColumnMetaData{Alias="XTP Storage %",isVisible=false } },
                {"avg_instance_cpu_percent", new ColumnMetaData{Alias="Avg CPU % (Instance)",isVisible=false } },
                {"avg_instance_memory_percent", new ColumnMetaData{Alias="Avg Memory % (Instance)",isVisible=false } },
                {"max_worker_percent", new ColumnMetaData{Alias="Max Worker %",isVisible=false } },
                {"max_session_percent", new ColumnMetaData{Alias="Max Session %",isVisible=false } },
                {"max_cpu_percent", new ColumnMetaData{Alias="Max CPU %",isVisible=true } },
                {"max_data_io_percent", new ColumnMetaData{Alias="Max Data %",isVisible=true } },
                {"max_log_write_percent", new ColumnMetaData{Alias="Max Log Write %",isVisible=true } },
                {"max_instance_cpu_percent", new ColumnMetaData{Alias="Max CPU % (Instance)",isVisible=false } },
                {"max_instance_memory_percent", new ColumnMetaData{Alias="Max Memory % (Instance)",isVisible=false } },
                {"MaxDTUPercent", new ColumnMetaData{Alias="Max DTU %",isVisible=false} },
                {"AvgDTUsUsed", new ColumnMetaData{Alias="Avg DTU",isVisible=false,axis=1} },
                {"MaxDTUsUsed", new ColumnMetaData{Alias="Max DTU",isVisible=false,axis=1} },
                {"dtu_limit", new ColumnMetaData{Alias="DTU Limit",isVisible=false,axis=1} },
                {"cpu_limit", new ColumnMetaData{Alias="CPU Limit",isVisible=false,axis=1} },
            };
        readonly Dictionary<string, ColumnMetaData> PoolColumns = new()
        {
                {"avg_cpu_percent", new ColumnMetaData{Alias="Avg CPU %",isVisible=false } },
                {"avg_data_io_percent", new ColumnMetaData{Alias="Avg Data %",isVisible=false } },
                {"avg_log_write_percent", new ColumnMetaData{Alias="Avg Log Write %",isVisible=false } },
                {"AvgDTUPercent", new ColumnMetaData{Alias="Avg DTU %",isVisible=false } },
                {"max_worker_percent", new ColumnMetaData{Alias="Max Worker %",isVisible=false } },
                {"max_session_percent", new ColumnMetaData{Alias="Max Session %",isVisible=false } },
                {"max_cpu_percent", new ColumnMetaData{Alias="Max CPU %",isVisible=true } },
                {"max_data_io_percent", new ColumnMetaData{Alias="Max Data %",isVisible=true } },
                {"max_log_write_percent", new ColumnMetaData{Alias="Max Log Write %",isVisible=true } },
                {"MaxDTUPercent", new ColumnMetaData{Alias="Max DTU %",isVisible=false} },
                {"AvgDTUsUsed", new ColumnMetaData{Alias="Avg DTU",isVisible=false,axis=1} },
                {"MaxDTUsUsed", new ColumnMetaData{Alias="Max DTU",isVisible=false,axis=1} },
                {"dtu_limit", new ColumnMetaData{Alias="DTU Limit",isVisible=false,axis=1} }
            };


        DataTable dt;

        public void RefreshData()
        {
            DateGrouping = Common.DateGrouping(DateRange.DurationMins, 400);
            RefreshDataLocal();
        }

        private void RefreshDataLocal()
        {
            if (ElasticPoolName == string.Empty)
            {
                dt = GetAzureDBResourceStats();
            }
            else
            {
                dt = GetAzurePoolResourceStats();
            }

            UpdateChart();
        }

        private void UpdateChart()
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
            Int32 y1Max = 1;
            foreach (DataRow r in dt.Rows)
            {
                var dtuLimit = r["dtu_limit"] == DBNull.Value ? 0 : (Int32)r["dtu_limit"];
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
                        PointGeometrySize = PointSize,
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
            var y0visible = columns.Values.Where(c => c.isVisible && c.axis == 0).Any();
            chart1.AxisY.Add(new Axis
            {
                Title = "%",
                LabelFormatter = val => val.ToString("0.0"),
                MinValue = 0,
                MaxValue = 100,
                Visibility = y0visible ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden

            });
            var y1visible = columns.Values.Where(c => c.isVisible && c.axis == 1).Any();
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

        private void AddMeasures()
        {
            foreach (var k in columns)
            {
                var dd = new ToolStripMenuItem(k.Value.Alias)
                {
                    Name = (string)k.Key,
                    CheckOnClick = true
                };
                dd.Checked = dd.Enabled && k.Value.isVisible;
                dd.Click += MeasureDropDown_Click;
                tsMeasures.DropDownItems.Add(dd);
            }
        }

        private void MeasureDropDown_Click(object sender, EventArgs e)
        {
            var mnu = (ToolStripMenuItem)sender;
            columns[mnu.Name].isVisible = mnu.Checked;
            UpdateChart();
        }

        private DataTable GetAzurePoolResourceStats()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            {
                using (SqlCommand cmd = new("dbo.AzureDBElasticPoolResourceStats_Get", cn) { CommandType = CommandType.StoredProcedure })
                {
                    cn.Open();
                    cmd.Parameters.AddWithValue("FromDate", DateRange.FromUTC);
                    cmd.Parameters.AddWithValue("ToDate", DateRange.ToUTC);
                    cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                    cmd.Parameters.AddWithValue("elastic_pool_name", ElasticPoolName);
                    cmd.Parameters.AddWithValue("DateGroupingMin", DateGrouping);
                    cmd.Parameters.AddWithValue("UTCOffset", Common.UtcOffset);
                    SqlDataAdapter da = new(cmd);
                    DataTable dt = new();
                    da.Fill(dt);
                    return dt;
                }
            }
        }


        private DataTable GetAzureDBResourceStats()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            {
                using (SqlCommand cmd = new("dbo.AzureDBResourceStats_Get", cn) { CommandType = CommandType.StoredProcedure })
                {
                    cn.Open();
                    cmd.Parameters.AddWithValue("FromDate", DateRange.FromUTC);
                    cmd.Parameters.AddWithValue("ToDate", DateRange.ToUTC);
                    cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                    cmd.Parameters.AddWithValue("DateGroupingMin", DateGrouping);
                    cmd.Parameters.AddWithValue("UTCOffset", Common.UtcOffset);
                    SqlDataAdapter da = new(cmd);
                    DataTable dt = new();
                    da.Fill(dt);
                    return dt;
                }
            }
        }

        private void TsRefresh_Click(object sender, EventArgs e)
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
            AddMeasures();
            Common.AddDateGroups(tsDateGrouping, TsDateGrouping_Click);

        }

        private void TsDateGrouping_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            DateGrouping = Convert.ToInt32(ts.Tag);
            RefreshDataLocal();
        }

        private void SmoothLinesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateChart();
        }

        private void PointsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (LineSeries s in chart1.Series.Cast<LineSeries>())
            {
                s.PointGeometrySize = PointSize;
            }
        }

    }
}
