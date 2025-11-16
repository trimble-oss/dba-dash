using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Title
        {
            get => tsTitle.Text; set => tsTitle.Text = value;
        }

        private readonly Dictionary<string, ColumnMetaData> columns = new()
        {
                {"AvgCPU", new ColumnMetaData{Name="Avg CPU (sec)",IsVisible=true } },
                {"TotalCPU", new ColumnMetaData{Name="Total CPU (sec)",IsVisible=false } },
                {"cpu_ms_per_sec", new ColumnMetaData{Name="CPU (ms/sec)",IsVisible=false } },
                {"ExecutionsPerMin", new ColumnMetaData{Name="Executions/min",IsVisible=true, axis=1 } },
                {"MaxExecutionsPerMin", new ColumnMetaData{Name="Max Executions/min",IsVisible=false, axis=1 } },
                {"ExecutionCount", new ColumnMetaData{Name="Execution Count",IsVisible=false, axis=1 } },
                {"AvgDuration", new ColumnMetaData{Name="Avg Duration (sec)",IsVisible=true} },
                {"TotalDuration", new ColumnMetaData{Name="Total Duration (sec)",IsVisible=false } },
                {"duration_ms_per_sec", new ColumnMetaData{Name="Duration (ms/sec)",IsVisible=false } },
                {"AvgLogicalReads", new ColumnMetaData{Name="Avg Logical Reads",IsVisible=false,axis=2} },
                {"TotalLogicalReads", new ColumnMetaData{Name="Total Logical Reads",IsVisible=false,axis=2} },
                {"AvgPhysicalReads", new ColumnMetaData{Name="Avg Physical Reads",IsVisible=false,axis=2} },
                {"TotalPhysicalReads", new ColumnMetaData{Name="Total Physical Reads",IsVisible=false,axis=2} },
                {"AvgWrites", new ColumnMetaData{Name="Avg Writes",IsVisible=false,axis=2} },
                {"TotalWrites", new ColumnMetaData{Name="Total Writes",IsVisible=false,axis=2} },
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
            chart1.Series.Clear();
            chart1.DefaultFill = System.Windows.Media.Brushes.Transparent;

            chart1.AddDataTable(dt, columns, "SnapshotDate", false);
            chart1.AxisY.Clear();

            chart1.AxisY.Add(new Axis
            {
                Title = "",
                LabelFormatter = val => val.ToString("0.000"),
                MinValue = 0
            });
            chart1.AxisY.Add(new Axis
            {
                Title = "Executions",
                LabelFormatter = val => val.ToString("0.0"),
                Position = AxisPosition.RightTop,
                MinValue = 0
            });
            chart1.AxisY.Add(new Axis
            {
                Title = "Pages",
                LabelFormatter = val => val.ToString("0.0"),
                Position = AxisPosition.RightTop,
                MinValue = 0
            });
            chart1.LegendLocation = LegendLocation.Bottom;
        }

        private void TsLineSmoothness_Click(object sender, EventArgs e)
        {
            chart1.DefaultLineSmoothness = Convert.ToDouble(((ToolStripMenuItem)sender).Tag);
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
            chart1.UpdateColumnVisibility(columns);
        }

        private void TsPointSize_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            chart1.SetPointSize(Convert.ToInt32(ts.Tag));
        }
    }
}