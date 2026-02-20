using DBADashGUI.Charts;
using DBADashGUI.Theme;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    public partial class ObjectExecutionLineChart : UserControl
    {
        public ObjectExecutionLineChart()
        {
            InitializeComponent();
        }

        public int InstanceID;
        public string Instance;
        public long ObjectID;
        private int dateGrouping;
        private int durationMins;
        public DateTime FromDate;
        public DateTime ToDate;
        private double lineSmoothness = ChartConfiguration.DefaultLineSmoothness;
        private double geometrySize = ChartConfiguration.DefaultGeometrySize;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Title
        {
            get => tsTitle.Text; set => tsTitle.Text = value;
        }

        private readonly Dictionary<string, ColumnMetaData> columns = new()
        {
                {"AvgCPU", new ColumnMetaData{Name="Avg CPU (sec)",IsVisible=true, AxisName="Primary" } },
                {"TotalCPU", new ColumnMetaData{Name="Total CPU (sec)",IsVisible=false, AxisName="Primary" } },
                {"cpu_ms_per_sec", new ColumnMetaData{Name="CPU (ms/sec)",IsVisible=false, AxisName="Primary" } },
                {"ExecutionsPerMin", new ColumnMetaData{Name="Executions/min",IsVisible=true, AxisName="Executions" } },
                {"MaxExecutionsPerMin", new ColumnMetaData{Name="Max Executions/min",IsVisible=false, AxisName="Executions" } },
                {"ExecutionCount", new ColumnMetaData{Name="Execution Count",IsVisible=false, AxisName="Executions" } },
                {"AvgDuration", new ColumnMetaData{Name="Avg Duration (sec)",IsVisible=true, AxisName="Primary" } },
                {"TotalDuration", new ColumnMetaData{Name="Total Duration (sec)",IsVisible=false, AxisName="Primary" } },
                {"duration_ms_per_sec", new ColumnMetaData{Name="Duration (ms/sec)",IsVisible=false, AxisName="Primary" } },
                {"AvgLogicalReads", new ColumnMetaData{Name="Avg Logical Reads",IsVisible=false, AxisName="Pages" } },
                {"TotalLogicalReads", new ColumnMetaData{Name="Total Logical Reads",IsVisible=false, AxisName="Pages" } },
                {"AvgPhysicalReads", new ColumnMetaData{Name="Avg Physical Reads",IsVisible=false, AxisName="Pages" } },
                {"TotalPhysicalReads", new ColumnMetaData{Name="Total Physical Reads",IsVisible=false, AxisName="Pages" } },
                {"AvgWrites", new ColumnMetaData{Name="Avg Writes",IsVisible=false, AxisName="Pages" } },
                {"TotalWrites", new ColumnMetaData{Name="Total Writes",IsVisible=false, AxisName="Pages" } },
            };

        public void RefreshData()
        {
            var newDurationMins = Convert.ToInt32(ToDate.Subtract(FromDate).TotalMinutes);
            if (newDurationMins != durationMins) // Update date grouping only if duration has changed
            {
                dateGrouping = DateHelper.DateGrouping(newDurationMins, 200);
                tsGroup.Text = DateHelper.DateGroupString(dateGrouping);
                durationMins = newDurationMins;
            }

            var dt = CommonData.ObjectExecutionStats(InstanceID, -1, ObjectID, dateGrouping, "AvgDuration", FromDate, ToDate, Instance);

            if (dt == null || dt.Rows.Count == 0)
            {
                chart1.Series = Array.Empty<ISeries>();
                return;
            }

            // Get visible columns
            var visibleColumns = columns.Where(c => c.Value.IsVisible).Select(c => c.Key).ToArray();

            if (visibleColumns.Length == 0)
            {
                chart1.Series = Array.Empty<ISeries>();
                return;
            }

            // Create series names dictionary
            var seriesNames = columns.ToDictionary(c => c.Key, c => c.Value.Name);

            // Create column-to-axis-name mapping
            var columnAxisNames = columns
                .Where(c => c.Value.IsVisible)
                .ToDictionary(c => c.Key, c => c.Value.AxisName);

            // Setup Y-axes configurations
            var yAxes = new List<YAxisConfiguration>();
            var axisNameToIndexMap = new Dictionary<string, int>();

            // Determine which axes to include based on visible columns
            var hasPrimaryMetrics = columnAxisNames.Values.Any(a => a == "Primary");
            var hasExecutionMetrics = columnAxisNames.Values.Any(a => a == "Executions");
            var hasPagesMetrics = columnAxisNames.Values.Any(a => a == "Pages");

            // Axis 0: Primary (CPU/Duration) - always include if has primary metrics
            if (hasPrimaryMetrics)
            {
                axisNameToIndexMap["Primary"] = yAxes.Count;
                yAxes.Add(new YAxisConfiguration
                {
                    Name = "Primary",
                    Label = string.Empty,
                    Format = "0.000",
                    MinLimit = 0,
                    Position = AxisPosition.Start
                });
            }

            // Axis 1: Executions - include if has execution metrics
            if (hasExecutionMetrics)
            {
                axisNameToIndexMap["Executions"] = yAxes.Count;
                yAxes.Add(new YAxisConfiguration
                {
                    Name = "Executions",
                    Label = "Executions",
                    Format = "0.0",
                    MinLimit = 0,
                    Position = AxisPosition.End
                });
            }

            // Axis 2: Pages - only include if has reads/writes metrics
            if (hasPagesMetrics)
            {
                axisNameToIndexMap["Pages"] = yAxes.Count;
                yAxes.Add(new YAxisConfiguration
                {
                    Name = "Pages",
                    Label = "Pages",
                    Format = "0.0",
                    MinLimit = 0,
                    Position = AxisPosition.End
                });
            }

            // Update chart using ChartHelper with name-based axis mapping
            var config = new ChartConfiguration
            {
                DateColumn = "SnapshotDate",
                MetricColumns = visibleColumns,
                ChartType = ChartTypes.Line,
                ShowLegend = true,
                LegendPosition = LegendPosition.Bottom,
                LineSmoothness = lineSmoothness,
                GeometrySize = geometrySize,
                XAxisMin = FromDate.ToAppTimeZone(),
                XAxisMax = ToDate.ToAppTimeZone(),
                SeriesNames = seriesNames,
                YAxes = yAxes.ToArray(),
                ColumnAxisNames = columnAxisNames,
                DateUnit = TimeSpan.FromMinutes(dateGrouping)
            };

            ChartHelper.UpdateChart(chart1, dt, config);
        }

        private void TsLineSmoothness_Click(object sender, EventArgs e)
        {
            lineSmoothness = Convert.ToDouble(((ToolStripMenuItem)sender).Tag);
            RefreshData();
        }

        private void ObjectExecutionLineChart_Load(object sender, EventArgs e)
        {
            DateHelper.AddDateGroups(tsGroup, TsGroup_Click);
            LoadMeasures();
        }

        private void TsGroup_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            dateGrouping = (int)ts.Tag;
            tsGroup.Text = DateHelper.DateGroupString(dateGrouping);
            RefreshData();
        }

        private void LoadMeasures()
        {
            foreach (var k in columns.Keys)
            {
                var m = columns[k];
                tsMeasures.DropDownItems.Add(new ToolStripMenuItem(m.Name, null, TsMeasure_Click) { Checked = m.IsVisible, Tag = k, CheckOnClick = true });
            }
        }

        private void TsMeasure_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            columns[((string)ts.Tag)!].IsVisible = ts.Checked;
            RefreshData();  // Refresh chart to apply visibility changes
        }

        private void TsPointSize_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            geometrySize = Convert.ToInt32(ts.Tag);
            RefreshData();
        }
    }
}