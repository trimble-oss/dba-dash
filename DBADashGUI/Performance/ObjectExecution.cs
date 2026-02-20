using DBADashGUI.Charts;
using DBADashGUI.Theme;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    public partial class ObjectExecution : UserControl, IMetricChart
    {
        public ObjectExecution()
        {
            InitializeComponent();
        }

        public class DateModel
        {
            public DateTime DateTime { get; set; }
            public double Value { get; set; }
        }

        private int TopRows => tsTop.Tag == null ? 10 : Convert.ToInt32(tsTop.Tag);
        private bool IncludeOther => includeOtherToolStripMenuItem.Checked;

        private int instanceID;
        private DateTime chartMaxDate = DateTime.MinValue;

        private int mins;
        private long objectID;
        private int databaseid;
        private int dateGrouping;
        private LegendPosition legendPosition = LegendPosition.Hidden;

        public event EventHandler<EventArgs> Close;

        public event EventHandler<EventArgs> MoveUp;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool CloseVisible
        {
            get => tsClose.Visible;
            set => tsClose.Visible = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool MoveUpVisible
        {
            get => tsUp.Visible;
            set => tsUp.Visible = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ObjectExecutionMetric Metric { get; set; } = new();

        IMetric IMetricChart.Metric => Metric;

        public void RefreshData(int instanceID, long objectID, int databaseID)
        {
            this.instanceID = instanceID;
            if (mins != DateRange.DurationMins)
            {
                dateGrouping = DateHelper.DateGrouping(DateRange.DurationMins, 65);
                if (dateGrouping < 1)
                {
                    dateGrouping = 1;
                }
                tsDateGroup.Text = DateHelper.DateGroupString(dateGrouping);
                mins = DateRange.DurationMins;
            }
            this.objectID = objectID;

            databaseid = databaseID;
            RefreshData();
        }

        public void RefreshData(int InstanceID)
        {
            RefreshData(InstanceID, -1, -1);
        }

        public void RefreshData()
        {
            lblExecution.Text = databaseid > 0 ? "Execution Stats: Database" : "Execution Stats: Instance";
            toolStrip1.Tag = databaseid > 0 ? "ALT" : null; // set tag to ALT to use the alternate menu renderer
            toolStrip1.ApplyTheme(DBADashUser.SelectedTheme);

            var dt = CommonData.ObjectExecutionStats(instanceID, databaseid, objectID, dateGrouping, Metric.Measure, DateRange.FromUTC, DateRange.ToUTC, default, TopRows, IncludeOther);

            if (dt == null || dt.Rows.Count == 0)
            {
                objectExecChart.Series = Array.Empty<ISeries>();
                objectExecChart.XAxes = Array.Empty<Axis>();
                objectExecChart.YAxes = Array.Empty<Axis>();
                return;
            }

            // Add a computed column for ObjectName (combination of DatabaseName and object_name)
            if (!dt.Columns.Contains("ObjectName"))
            {
                dt.Columns.Add("ObjectName", typeof(string));
                foreach (DataRow row in dt.Rows)
                {
                    row["ObjectName"] = row["DatabaseName"] + " | " + row["object_name"];
                }
            }

            // Use ChartHelper with SeriesColumn to group by ObjectName
            var config = new ChartConfiguration
            {
                DateColumn = "SnapshotDate",
                MetricColumn = "Measure",
                SeriesColumn = "ObjectName",  // Group data by ObjectName - each becomes a series
                ChartType = ChartTypes.StackedColumn,
                ShowLegend = true,
                LegendPosition = legendPosition,
                XAxisMin = DateRange.FromUTC.ToAppTimeZone(),
                XAxisMax = DateRange.ToUTC.ToAppTimeZone(),
                YAxisLabel = measures[Metric.Measure].DisplayName,
                YAxisFormat = "0.0",
                YAxisMin = 0,
                DateUnit = TimeSpan.FromMinutes(dateGrouping)
            };

            ChartHelper.UpdateChart(objectExecChart, dt, config);
        }

        private class Measure
        {
            public string Name { get; set; }
            public string DisplayName { get; init; }
            public string LabelFormat { get; init; }
        }

        private class Measures : Dictionary<string, Measure>
        {
            public void Add(string Name, string displayName, string labelFormat)
            {
                Add(Name, new Measure() { Name = Name, DisplayName = displayName, LabelFormat = labelFormat });
            }
        }

        private readonly Measures measures = new()
            {
                {"TotalDuration", "Total Duration","#,##0.000 sec"},
                {"duration_ms_per_sec","Duration (ms/sec)","#,##0.000 ms/sec"},
                {"AvgDuration", "Avg Duration","#,##0.000 sec"},
                {"TotalCPU", "Total CPU","#,##0.000 sec"},
                {"cpu_ms_per_sec", "CPU (ms/sec)","#,##0.000 ms/sec"},
                {"AvgCPU","Avg CPU", "#,##0.000  sec"},
                {"ExecutionCount", "Execution Count","N0"},
                {"ExecutionsPerMin", "Executions Per Min","#,##0.000"},
                {"MaxExecutionsPerMin", "Max Executions Per Min","#,##0.000"},
                {"TotalLogicalReads","Total Logical Reads","N0" },
                {"AvgLogicalReads","Avg Logical Reads","N0" },
                {"TotalPhysicalReads","Total Physical Reads" ,"N0"},
                {"AvgPhysicalReads","Avg Physical Reads" ,"N0"},
                {"TotalWrites","Total Writes" ,"N0"},
                {"AvgWrites","Avg Writes","N0" }
            };

        private void ObjectExecution_Load(object sender, EventArgs e)
        {
            DateHelper.AddDateGroups(tsDateGroup, TsDateGrouping_Click);
            foreach (var m in measures)
            {
                ToolStripMenuItem itm = new(m.Value.DisplayName)
                {
                    Name = m.Key
                };
                if (m.Key == Metric.Measure)
                {
                    itm.Checked = true;
                    tsMeasures.Text = m.Value.DisplayName;
                }

                itm.Click += Itm_Click;
                tsMeasures.DropDownItems.Add(itm);
            }
        }

        private void TsDateGrouping_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            dateGrouping = Convert.ToInt32(ts.Tag);
            tsDateGroup.Text = DateHelper.DateGroupString(dateGrouping);
            RefreshData();
        }

        private void Itm_Click(object sender, EventArgs e)
        {
            var tsItm = ((ToolStripMenuItem)sender);
            if (Metric.Measure != tsItm.Name)
            {
                Metric.Measure = tsItm.Name;
                foreach (ToolStripMenuItem itm in tsMeasures.DropDownItems)
                {
                    itm.Checked = itm.Name == Metric.Measure;
                }
                tsMeasures.Text = tsItm.Text;
            }
            RefreshData(instanceID, objectID, databaseid);
        }

        private void TsClose_Click(object sender, EventArgs e)
        {
            Close?.Invoke(this, EventArgs.Empty);
        }

        private void TsUp_Click(object sender, EventArgs e)
        {
            MoveUp?.Invoke(this, EventArgs.Empty);
        }

        private void Top_Select(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            tsTop.Tag = ts.Tag;
            tsTop.Text = "Top " + ts.Tag;
            foreach (var itm in tsTop.DropDownItems.OfType<ToolStripMenuItem>().Where(item => item != includeOtherToolStripMenuItem))
            {
                itm.Checked = itm == ts;
            }
            RefreshData();
        }

        private void includeOtherToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void SetLegendPosition(object sender, EventArgs e)
        {
            var item = (ToolStripMenuItem)sender;
            Enum.TryParse(item.Tag.ToString(), out legendPosition);
            foreach (ToolStripMenuItem menuItem in tsLegend.DropDownItems.OfType<ToolStripMenuItem>())
            {
                menuItem.Checked = menuItem == item;
            }
            objectExecChart.LegendPosition = legendPosition;
        }
    }
}