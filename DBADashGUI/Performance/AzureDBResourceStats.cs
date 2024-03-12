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
    public partial class AzureDBResourceStats : UserControl, ISetContext, IRefreshData
    {
        public int InstanceID;
        public string ElasticPoolName = string.Empty;

        public AzureDBResourceStats()
        {
            InitializeComponent();
        }

        private bool SmoothLines
        {
            get => smoothLinesToolStripMenuItem.Checked;
            set => smoothLinesToolStripMenuItem.Checked = value;
        }

        private int PointSize => pointsToolStripMenuItem.Checked ? 10 : 0;

        private string DateFormat => DateGrouping switch
        {
            0 => "yyyy-MM-dd HH:mm:ss",
            >= 1440 => "yyyy-MM-dd",
            _ => "yyyy-MM-dd HH:mm"
        };

        private int _dateGrouping = 0;

        public int DateGrouping
        {
            get => _dateGrouping;
            set
            {
                _dateGrouping = value;
                tsDateGrouping.Text = DateHelper.DateGroupString(_dateGrouping);
            }
        }

        private Dictionary<string, ColumnMetaData> columns;

        private readonly Dictionary<string, ColumnMetaData> DBColumns = new()
        {
                {"avg_cpu_percent", new ColumnMetaData{Name="Avg CPU %",IsVisible=false } },
                {"avg_data_io_percent", new ColumnMetaData{Name="Avg Data %",IsVisible=false } },
                {"avg_log_write_percent", new ColumnMetaData{Name="Avg Log Write %",IsVisible=false } },
                {"AvgDTUPercent", new ColumnMetaData{Name="Avg DTU %",IsVisible=false } },
                {"avg_memory_usage_percent", new ColumnMetaData{Name="Avg Memory %",IsVisible=false } },
                {"xtp_storage_percent", new ColumnMetaData{Name="XTP Storage %",IsVisible=false } },
                {"avg_instance_cpu_percent", new ColumnMetaData{Name="Avg CPU % (Instance)",IsVisible=false } },
                {"avg_instance_memory_percent", new ColumnMetaData{Name="Avg Memory % (Instance)",IsVisible=false } },
                {"max_worker_percent", new ColumnMetaData{Name="Max Worker %",IsVisible=false } },
                {"max_session_percent", new ColumnMetaData{Name="Max Session %",IsVisible=false } },
                {"max_cpu_percent", new ColumnMetaData{Name="Max CPU %",IsVisible=true } },
                {"max_data_io_percent", new ColumnMetaData{Name="Max Data %",IsVisible=true } },
                {"max_log_write_percent", new ColumnMetaData{Name="Max Log Write %",IsVisible=true } },
                {"max_instance_cpu_percent", new ColumnMetaData{Name="Max CPU % (Instance)",IsVisible=false } },
                {"max_instance_memory_percent", new ColumnMetaData{Name="Max Memory % (Instance)",IsVisible=false } },
                {"MaxDTUPercent", new ColumnMetaData{Name="Max DTU %",IsVisible=false} },
                {"AvgDTUsUsed", new ColumnMetaData{Name="Avg DTU",IsVisible=false,axis=1} },
                {"MaxDTUsUsed", new ColumnMetaData{Name="Max DTU",IsVisible=false,axis=1} },
                {"dtu_limit", new ColumnMetaData{Name="DTU Limit",IsVisible=false,axis=1} },
                {"cpu_limit", new ColumnMetaData{Name="CPU Limit",IsVisible=false,axis=1} },
            };

        private readonly Dictionary<string, ColumnMetaData> PoolColumns = new()
        {
                {"avg_cpu_percent", new ColumnMetaData{Name="Avg CPU %",IsVisible=false } },
                {"avg_data_io_percent", new ColumnMetaData{Name="Avg Data %",IsVisible=false } },
                {"avg_log_write_percent", new ColumnMetaData{Name="Avg Log Write %",IsVisible=false } },
                {"AvgDTUPercent", new ColumnMetaData{Name="Avg DTU %",IsVisible=false } },
                {"max_worker_percent", new ColumnMetaData{Name="Max Worker %",IsVisible=false } },
                {"max_session_percent", new ColumnMetaData{Name="Max Session %",IsVisible=false } },
                {"max_cpu_percent", new ColumnMetaData{Name="Max CPU %",IsVisible=true } },
                {"max_data_io_percent", new ColumnMetaData{Name="Max Data %",IsVisible=true } },
                {"max_log_write_percent", new ColumnMetaData{Name="Max Log Write %",IsVisible=true } },
                {"MaxDTUPercent", new ColumnMetaData{Name="Max DTU %",IsVisible=false} },
                {"AvgDTUsUsed", new ColumnMetaData{Name="Avg DTU",IsVisible=false,axis=1} },
                {"MaxDTUsUsed", new ColumnMetaData{Name="Max DTU",IsVisible=false,axis=1} },
                {"dtu_limit", new ColumnMetaData{Name="DTU Limit",IsVisible=false,axis=1} }
            };

        private DataTable dt;

        public void SetContext(DBADashContext context)
        {
            InstanceID =context.MasterInstanceID;
            ElasticPoolName=context.ElasticPoolName;
            RefreshData();
        }

        public void RefreshData()
        {
            DateGrouping = DateHelper.DateGrouping(DateRange.DurationMins, 400);
            RefreshDataLocal();
        }

        private void RefreshDataLocal()
        {
            dt = ElasticPoolName == string.Empty ? GetAzureDBResourceStats() : GetAzurePoolResourceStats();

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

            int i = 0;
            int y1Max = 1;
            foreach (DataRow r in dt.Rows)
            {
                var dtuLimit = r["dtu_limit"] == DBNull.Value ? 0 : (int)r["dtu_limit"];
                if (ElasticPoolName == string.Empty)
                {
                    var cpuLimit = r["cpu_Limit"] == DBNull.Value ? 0 : Convert.ToInt32(r["cpu_limit"]);
                    y1Max = cpuLimit > y1Max ? cpuLimit : y1Max;
                }

                y1Max = dtuLimit > y1Max ? dtuLimit : y1Max;
                foreach (var kv in columns)
                {
                    if (kv.Value.IsVisible)
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
                if (kv.Value.IsVisible)
                {
                    var v = new ChartValues<DateTimePoint>();
                    v.AddRange(columns[kv.Key].Points);
                    sc.Add(new LineSeries
                    {
                        Title = columns[kv.Key].Name,
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
            var y0visible = columns.Values.Where(c => c.IsVisible && c.axis == 0).Any();
            chart1.AxisY.Add(new Axis
            {
                Title = "%",
                LabelFormatter = val => val.ToString("0.0"),
                MinValue = 0,
                MaxValue = 100,
                Visibility = y0visible ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden
            });
            var y1visible = columns.Values.Where(c => c.IsVisible && c.axis == 1).Any();
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
                var dd = new ToolStripMenuItem(k.Value.Name)
                {
                    Name = (string)k.Key,
                    CheckOnClick = true
                };
                dd.Checked = dd.Enabled && k.Value.IsVisible;
                dd.Click += MeasureDropDown_Click;
                tsMeasures.DropDownItems.Add(dd);
            }
        }

        private void MeasureDropDown_Click(object sender, EventArgs e)
        {
            var mnu = (ToolStripMenuItem)sender;
            columns[mnu.Name].IsVisible = mnu.Checked;
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
                    cmd.Parameters.AddWithValue("UTCOffset", DateHelper.UtcOffset);
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
                    cmd.Parameters.AddWithValue("UTCOffset", DateHelper.UtcOffset);
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
            columns = ElasticPoolName == string.Empty ? DBColumns : PoolColumns;
            AddMeasures();
            DateHelper.AddDateGroups(tsDateGrouping, TsDateGrouping_Click);
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