using DBADashGUI.Charts;
using DBADashGUI.Theme;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    public partial class AzureDBResourceStats : UserControl, ISetContext, IRefreshData
    {
        private int InstanceID;
        private int MasterInstanceID;
        private string ElasticPoolName = string.Empty;
        private double lineSmoothness = ChartConfiguration.DefaultLineSmoothness;
        private double geometrySize = ChartConfiguration.DefaultGeometrySize;

        public AzureDBResourceStats()
        {
            InitializeComponent();
        }

        private int _dateGrouping;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int DateGrouping
        {
            get => _dateGrouping;
            set
            {
                _dateGrouping = value;
                tsDateGrouping.Text = DateHelper.DateGroupString(_dateGrouping);
            }
        }

        private readonly Dictionary<string, ColumnMetaData> DBColumns = new()
        {
                {"avg_cpu_percent", new ColumnMetaData{Name="Avg CPU %",IsVisible=false, AxisName="Percent" } },
                {"avg_data_io_percent", new ColumnMetaData{Name="Avg Data %",IsVisible=false, AxisName="Percent" } },
                {"avg_log_write_percent", new ColumnMetaData{Name="Avg Log Write %",IsVisible=false, AxisName="Percent" } },
                {"AvgDTUPercent", new ColumnMetaData{Name="Avg DTU %",IsVisible=false, AxisName="Percent" } },
                {"avg_memory_usage_percent", new ColumnMetaData{Name="Avg Memory %",IsVisible=false, AxisName="Percent" } },
                {"xtp_storage_percent", new ColumnMetaData{Name="XTP Storage %",IsVisible=false, AxisName="Percent" } },
                {"avg_instance_cpu_percent", new ColumnMetaData{Name="Avg CPU % (Instance)",IsVisible=false, AxisName="Percent" } },
                {"avg_instance_memory_percent", new ColumnMetaData{Name="Avg Memory % (Instance)",IsVisible=false, AxisName="Percent" } },
                {"max_worker_percent", new ColumnMetaData{Name="Max Worker %",IsVisible=false, AxisName="Percent" } },
                {"max_session_percent", new ColumnMetaData{Name="Max Session %",IsVisible=false, AxisName="Percent" } },
                {"max_cpu_percent", new ColumnMetaData{Name="Max CPU %",IsVisible=true, AxisName="Percent" } },
                {"max_data_io_percent", new ColumnMetaData{Name="Max Data %",IsVisible=true, AxisName="Percent" } },
                {"max_log_write_percent", new ColumnMetaData{Name="Max Log Write %",IsVisible=true, AxisName="Percent" } },
                {"max_instance_cpu_percent", new ColumnMetaData{Name="Max CPU % (Instance)",IsVisible=false, AxisName="Percent" } },
                {"max_instance_memory_percent", new ColumnMetaData{Name="Max Memory % (Instance)",IsVisible=false, AxisName="Percent" } },
                {"MaxDTUPercent", new ColumnMetaData{Name="Max DTU %",IsVisible=false, AxisName="Percent"} },
                {"AvgDTUsUsed", new ColumnMetaData{Name="Avg DTU",IsVisible=false, AxisName="DTU"} },
                {"MaxDTUsUsed", new ColumnMetaData{Name="Max DTU",IsVisible=false, AxisName="DTU"} },
                {"dtu_limit", new ColumnMetaData{Name="DTU Limit",IsVisible=false, AxisName="DTU"} },
                {"cpu_limit", new ColumnMetaData{Name="CPU Limit",IsVisible=false, AxisName="DTU"} },
            };

        private readonly Dictionary<string, ColumnMetaData> PoolColumns = new()
        {
                {"avg_cpu_percent", new ColumnMetaData{Name="Avg CPU %",IsVisible=false, AxisName="Percent" } },
                {"avg_data_io_percent", new ColumnMetaData{Name="Avg Data %",IsVisible=false, AxisName="Percent" } },
                {"avg_log_write_percent", new ColumnMetaData{Name="Avg Log Write %",IsVisible=false, AxisName="Percent" } },
                {"AvgDTUPercent", new ColumnMetaData{Name="Avg DTU %",IsVisible=false, AxisName="Percent" } },
                {"max_worker_percent", new ColumnMetaData{Name="Max Worker %",IsVisible=false, AxisName="Percent" } },
                {"max_session_percent", new ColumnMetaData{Name="Max Session %",IsVisible=false, AxisName="Percent" } },
                {"max_cpu_percent", new ColumnMetaData{Name="Max CPU %",IsVisible=true, AxisName="Percent" } },
                {"max_data_io_percent", new ColumnMetaData{Name="Max Data %",IsVisible=true, AxisName="Percent" } },
                {"max_log_write_percent", new ColumnMetaData{Name="Max Log Write %",IsVisible=true, AxisName="Percent" } },
                {"MaxDTUPercent", new ColumnMetaData{Name="Max DTU %",IsVisible=false, AxisName="Percent"} },
                {"AvgDTUsUsed", new ColumnMetaData{Name="Avg DTU",IsVisible=false, AxisName="DTU"} },
                {"MaxDTUsUsed", new ColumnMetaData{Name="Max DTU",IsVisible=false, AxisName="DTU"} },
                {"dtu_limit", new ColumnMetaData{Name="DTU Limit",IsVisible=false, AxisName="DTU"} }
            };

        private DataTable dt;

        public void SetContext(DBADashContext _context)
        {
            InstanceID = _context.InstanceID;
            MasterInstanceID = _context.MasterInstanceID;
            ElasticPoolName = _context.ElasticPoolName;
            tsPool.Text = $"Elastic Pool: {ElasticPoolName}";
            tsDB.Text = $"DB: {_context.DatabaseName}";
            splitContainer1.Panel1Collapsed = _context.Type == SQLTreeItem.TreeType.ElasticPool;
            splitContainer1.Panel2Collapsed = string.IsNullOrEmpty(ElasticPoolName);
            RefreshData();
        }

        public void RefreshData()
        {
            DateGrouping = DateHelper.DateGrouping(DateRange.DurationMins, 400);
            RefreshDataLocal();
        }

        private void RefreshDataLocal()
        {
            try
            {
                UpdateDBChart();
                UpdatePoolChart();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error refreshing Azure DB Resource Stats");
            }
        }

        private void UpdateDBChart()
        {
            if (splitContainer1.Panel1Collapsed)
            {
                return;
            }
            UpdateChart(GetAzureDBResourceStats(), chartDB, DBColumns);
        }

        private void UpdatePoolChart()
        {
            if (splitContainer1.Panel2Collapsed)
            {
                return;
            }
            UpdateChart(GetAzurePoolResourceStats(), chartPool, PoolColumns);

            // Align X axes between the two charts
            if (!splitContainer1.Panel1Collapsed
                 && !splitContainer1.Panel2Collapsed
                 && chartDB.XAxes != null
                 && chartPool.XAxes != null)
            {
                var minTicks = DateRange.FromUTC.ToAppTimeZone().Ticks;
                var maxTicks = DateRange.ToUTC.ToAppTimeZone().Ticks;

                var dbXAxes = chartDB.XAxes.ToArray();
                var poolXAxes = chartPool.XAxes.ToArray();

                if (dbXAxes.Length > 0)
                {
                    dbXAxes[0].MinLimit = minTicks;
                    dbXAxes[0].MaxLimit = maxTicks;
                    chartDB.XAxes = dbXAxes;
                }

                if (poolXAxes.Length > 0)
                {
                    poolXAxes[0].MinLimit = minTicks;
                    poolXAxes[0].MaxLimit = maxTicks;
                    chartPool.XAxes = poolXAxes;
                }
            }
        }

        private void UpdateChart(DataTable dt, LiveChartsCore.SkiaSharpView.WinForms.CartesianChart chart, Dictionary<string, ColumnMetaData> columns)
        {
            var cnt = dt.Rows.Count;
            if (cnt < 2)
            {
                chart.Series = Array.Empty<ISeries>();
                chart.Visible = false;
                return;
            }

            chart.Visible = true;

            // Get visible columns
            var visibleColumns = columns.Where(c => c.Value.IsVisible).Select(c => c.Key).ToArray();

            if (visibleColumns.Length == 0)
            {
                chart.Series = Array.Empty<ISeries>();
                return;
            }

            // Create series names dictionary
            var seriesNames = columns.ToDictionary(c => c.Key, c => c.Value.Name);

            // Create column-to-axis-name mapping
            var columnAxisNames = columns
                .Where(c => c.Value.IsVisible)
                .ToDictionary(c => c.Key, c => c.Value.AxisName);

            // Calculate DTU max value for Y-axis scaling
            var dtuMax = 1.0;
            foreach (DataRow r in dt.Rows)
            {
                if (r["dtu_limit"] != DBNull.Value)
                {
                    var dtuLimit = Convert.ToDouble(r["dtu_limit"]);
                    dtuMax = Math.Max(dtuMax, dtuLimit);
                }
                if (dt.Columns.Contains("cpu_limit") && r["cpu_limit"] != DBNull.Value)
                {
                    var cpuLimit = Convert.ToDouble(r["cpu_limit"]);
                    dtuMax = Math.Max(dtuMax, cpuLimit);
                }
            }

            // Setup Y-axes configurations
            var yAxes = new List<YAxisConfiguration>();
            var axisNameToIndexMap = new Dictionary<string, int>();

            // Determine which axes to include based on visible columns
            var hasPercentMetrics = columnAxisNames.Values.Any(a => a == "Percent");
            var hasDTUMetrics = columnAxisNames.Values.Any(a => a == "DTU");

            // Axis 0: Percent - include if has percent metrics
            if (hasPercentMetrics)
            {
                axisNameToIndexMap["Percent"] = yAxes.Count;
                yAxes.Add(new YAxisConfiguration
                {
                    Name = "Percent",
                    Label = "%",
                    Format = "0.0",
                    MinLimit = 0,
                    MaxLimit = 100,
                    Position = AxisPosition.Start
                });
            }

            // Axis 1: DTU - include if has DTU metrics
            if (hasDTUMetrics)
            {
                axisNameToIndexMap["DTU"] = yAxes.Count;
                yAxes.Add(new YAxisConfiguration
                {
                    Name = "DTU",
                    Label = "DTU",
                    Format = "0",
                    MinLimit = 0,
                    MaxLimit = dtuMax,
                    Position = AxisPosition.End
                });
            }

            // Update chart using ChartHelper with name-based axis mapping
            var config = new ChartConfiguration
            {
                DateColumn = "end_time",
                MetricColumns = visibleColumns,
                ChartType = ChartTypes.Line,
                ShowLegend = true,
                LegendPosition = LegendPosition.Bottom,
                LineSmoothness = lineSmoothness,
                GeometrySize = geometrySize,
                XAxisMin = DateRange.FromUTC.ToAppTimeZone(),
                XAxisMax = DateRange.ToUTC.ToAppTimeZone(),
                SeriesNames = seriesNames,
                YAxes = yAxes.ToArray(),
                ColumnAxisNames = columnAxisNames
            };

            ChartHelper.UpdateChart(chart, dt, config);
        }

        private void AddMeasures()
        {
            PopulateMeasureMenu(DBColumns, tsMeasures);
            PopulateMeasureMenu(PoolColumns, tsPoolMeasures);
        }

        private void PopulateMeasureMenu(Dictionary<string, ColumnMetaData> columns, ToolStripDropDownButton menu)
        {
            foreach (var k in columns)
            {
                var dd = new ToolStripMenuItem(k.Value.Name)
                {
                    Name = k.Key,
                    CheckOnClick = true,
                    Checked = k.Value.IsVisible
                };
                dd.Click += (sender, e) =>
                {
                    var mnu = (ToolStripMenuItem)sender;
                    if (!string.IsNullOrEmpty(mnu.Name))
                    {
                        columns[mnu.Name].IsVisible = mnu.Checked;
                        RefreshDataLocal();
                    }
                };
                menu.DropDownItems.Add(dd);
            }
        }

        private DataTable GetAzurePoolResourceStats()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using SqlCommand cmd = new("dbo.AzureDBElasticPoolResourceStats_Get", cn) { CommandType = CommandType.StoredProcedure };

            cn.Open();
            cmd.Parameters.AddWithValue("FromDate", DateRange.FromUTC);
            cmd.Parameters.AddWithValue("ToDate", DateRange.ToUTC);
            cmd.Parameters.AddWithValue("InstanceID", MasterInstanceID);
            cmd.Parameters.AddWithValue("elastic_pool_name", ElasticPoolName);
            cmd.Parameters.AddWithValue("DateGroupingMin", DateGrouping);
            cmd.Parameters.AddWithValue("UTCOffset", DateHelper.UtcOffset);
            SqlDataAdapter da = new(cmd);
            DataTable dt = new();
            da.Fill(dt);
            return dt;
        }

        private DataTable GetAzureDBResourceStats()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using SqlCommand cmd = new("dbo.AzureDBResourceStats_Get", cn) { CommandType = CommandType.StoredProcedure };

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

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void AzureDBResourceStats_Load(object sender, EventArgs e)
        {
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
            lineSmoothness = smoothLinesToolStripMenuItem.Checked ? ChartConfiguration.DefaultLineSmoothness : 0;
            RefreshDataLocal();
        }

        private void PointsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            geometrySize = pointsToolStripMenuItem.Checked ? ChartConfiguration.DefaultGeometrySize : 0;
            RefreshDataLocal();
        }
    }
}