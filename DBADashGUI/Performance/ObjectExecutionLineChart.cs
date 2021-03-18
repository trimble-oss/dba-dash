using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LiveCharts.Wpf;
using LiveCharts;

namespace DBADashGUI.Performance
{
    public partial class ObjectExecutionLineChart : UserControl
    {
        public ObjectExecutionLineChart()
        {
            InitializeComponent();
        }

        public Int32 InstanceID;
        public string Instance;
        public Int64 ObjectID;
        public DateTime FromDate;
        public DateTime ToDate;
        int dateGrouping;
        public string Title
        {
            get
            {
                return tsTitle.Text;
            }
            set
            {
                tsTitle.Text = value;
            }
        }

        readonly Dictionary<string, columnMetaData>  columns = new Dictionary<string, columnMetaData>
            {
                {"AvgCPU", new columnMetaData{Alias="Avg CPU (sec)",isVisible=true } },
                {"TotalCPU", new columnMetaData{Alias="Total CPU (sec)",isVisible=false } },
                {"cpu_ms_per_sec", new columnMetaData{Alias="CPU (ms/sec)",isVisible=false } },
                {"ExecutionsPerMin", new columnMetaData{Alias="Executions/min",isVisible=true, axis=1 } },
                {"MaxExecutionsPerMin", new columnMetaData{Alias="Max Executions/min",isVisible=false, axis=1 } },
                {"ExecutionCount", new columnMetaData{Alias="Execution Count",isVisible=false, axis=1 } },
                {"AvgDuration", new columnMetaData{Alias="Avg Duration (sec)",isVisible=true} },
                {"TotalDuration", new columnMetaData{Alias="Total Duration (sec)",isVisible=false } },
                {"duration_ms_per_sec", new columnMetaData{Alias="Duration (ms/sec)",isVisible=false } },
                {"AvgLogicalReads", new columnMetaData{Alias="Avg Logical Reads",isVisible=false,axis=2} },
                {"TotalLogicalReads", new columnMetaData{Alias="Total Logical Reads",isVisible=false,axis=2} },
                {"AvgPhysicalReads", new columnMetaData{Alias="Avg Physical Reads",isVisible=false,axis=2} },
                {"TotalPhysicalReads", new columnMetaData{Alias="Total Physical Reads",isVisible=false,axis=2} },
                {"AvgWrites", new columnMetaData{Alias="Avg Writes",isVisible=false,axis=2} },
                {"TotalWrites", new columnMetaData{Alias="Total Writes",isVisible=false,axis=2} },
            };

        public void RefreshData()
        {
            var mins = (Int32)ToDate.Subtract(FromDate).TotalMinutes;
            dateGrouping = Common.DateGrouping(mins, 200);
            tsGroup.Text = Common.DateGroupString(dateGrouping);
            refreshData();
        }

        private void refreshData()
        {
            var dt = CommonData.ObjectExecutionStats(InstanceID, -1, FromDate, ToDate, ObjectID, dateGrouping, "AvgDuration", Instance);
            chart1.Series.Clear();
            chart1.DefaultFill = System.Windows.Media.Brushes.Transparent;


            chart1.AddDataTable(dt, columns, "SnapshotDate");
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

        private void tsLineSmoothness_Click(object sender, EventArgs e)
        {
            chart1.DefaultLineSmoothness = Convert.ToDouble(((ToolStripMenuItem)sender).Tag);
        }

        private void ObjectExecutionLineChart_Load(object sender, EventArgs e)
        {
            Common.AddDateGroups(tsGroup, tsGroup_Click);
            loadMeasures();
        }

        private void tsGroup_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            dateGrouping = (Int32)ts.Tag;
            tsGroup.Text = Common.DateGroupString(dateGrouping);
            refreshData();

        }

        private void loadMeasures()
        {
            foreach(var k in columns.Keys)
            {
                var m = columns[k];
                tsMeasures.DropDownItems.Add(new ToolStripMenuItem(m.Alias,null, tsMeasure_Click) { Checked = m.isVisible, Tag = k, CheckOnClick=true });
            }
        }



        private void tsMeasure_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            columns[(string)ts.Tag].isVisible = ts.Checked;
            chart1.UpdateColumnVisibility(columns);
        }

        private void tsPointSize_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            chart1.SetPointSize(Convert.ToInt32(ts.Tag));
        }
    }
}
