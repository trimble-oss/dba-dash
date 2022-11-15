using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Data;
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
            public System.DateTime DateTime { get; set; }
            public double Value { get; set; }
        }

        //public string measure = "TotalDuration";
        public DateTimePoint x;
        Int32 instanceID;
        DateTime chartMaxDate = DateTime.MinValue;


        Int32 mins;
        private Int64 objectID;
        Int32 databaseid = 0;
        private Int32 dateGrouping;
        public event EventHandler<EventArgs> Close;
        public event EventHandler<EventArgs> MoveUp;

        public bool CloseVisible
        {
            get
            {
                return tsClose.Visible;
            }
            set
            {
                tsClose.Visible = value;
            }
        }

        public bool MoveUpVisible
        {
            get
            {
                return tsUp.Visible;
            }
            set
            {
                tsUp.Visible = value;
            }
        }

        public ObjectExecutionMetric Metric { get; set; } = new();


        IMetric IMetricChart.Metric { get => Metric; }

        public void RefreshData(Int32 instanceID, Int64 objectID, Int32 databaseID)
        {
            this.instanceID = instanceID;
            if (mins != DateRange.DurationMins)
            {
                dateGrouping = Common.DateGrouping(DateRange.DurationMins, 35);
                if (dateGrouping < 1)
                {
                    dateGrouping = 1;
                }
                tsDateGroup.Text = Common.DateGroupString(dateGrouping);
                mins = DateRange.DurationMins;
            }
            this.objectID = objectID;

            this.databaseid = databaseID;
            RefreshData();
        }

        public void RefreshData(int InstanceID)
        {
            RefreshData(InstanceID, -1, -1);
        }

        public void RefreshData()
        {

            objectExecChart.Series.Clear();
            objectExecChart.AxisX.Clear();
            objectExecChart.AxisY.Clear();
            chartMaxDate = DateTime.MinValue;


            var dt = CommonData.ObjectExecutionStats(instanceID, databaseid, objectID, dateGrouping, Metric.Measure, DateRange.FromUTC, DateRange.ToUTC, "");

            if (dt.Rows.Count == 0)
            {
                return;
            }
            var dPoints = new Dictionary<string, ChartValues<DateTimePoint>>();
            string current = string.Empty;
            ChartValues<DateTimePoint> values = new();
            foreach (DataRow r in dt.Rows)
            {
                var objectName = (string)r["DatabaseName"] + " | " + (string)r["object_name"];
                var time = (DateTime)r["SnapshotDate"];
                if (time > chartMaxDate)
                {
                    chartMaxDate = time;
                }
                if (current != objectName)
                {
                    if (values.Count > 0) { dPoints.Add(current, values); }
                    values = new ChartValues<DateTimePoint>();
                    current = objectName;
                }
                values.Add(new DateTimePoint(((DateTime)r["SnapshotDate"]), Convert.ToDouble(r["Measure"])));
            }
            if (values.Count > 0)
            {
                dPoints.Add(current, values);
                values = new ChartValues<DateTimePoint>();
            }

            CartesianMapper<DateTimePoint> dayConfig = Mappers.Xy<DateTimePoint>()
.X(dateModel => dateModel.DateTime.Ticks / TimeSpan.FromMinutes(dateGrouping == 0 ? 1 : dateGrouping).Ticks)
.Y(dateModel => dateModel.Value);


            SeriesCollection s1 = new(dayConfig);
            foreach (var x in dPoints)
            {
                s1.Add(new StackedColumnSeries
                {
                    Title = x.Key,
                    Values = x.Value
                });
            }
            objectExecChart.Series = s1;

            string format = "t";
            if (dateGrouping >= 1440)
            {
                format = "yyyy-MM-dd";
            }
            else if (mins >= 1440)
            {
                format = "yyyy-MM-dd HH:mm";
            }
            objectExecChart.AxisX.Add(new Axis
            {
                LabelFormatter = value => new DateTime((long)(value * TimeSpan.FromMinutes(dateGrouping == 0 ? 1 : dateGrouping).Ticks)).ToString(format)
            });

            objectExecChart.AxisY.Add(new Axis
            {
                LabelFormatter = val => val.ToString(measures[Metric.Measure].LabelFormat)

            });


            lblExecution.Text = databaseid > 0 ? "Excution Stats: Database" : "Execution Stats: Instance";
        }

        private class Measure
        {

            public string Name { get; set; }
            public string DisplayName { get; set; }
            public string LabelFormat { get; set; }


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
                {"AvgDuration", "Avg Duration","#,##0.000 sec"},
                {"TotalCPU", "Total CPU","#,##0.000 sec"},
                {"AvgCPU","Avg CPU", "#,##0.000  sec"},
                {"ExecutionCount", "Execution Count","N0"},
                {"ExecutionsPerMin", "Executions Per Min","#,##0.000"},
                {"MaxExecutionsPerMin", "Max Executions Per Min","#,##0.000"},
                { "TotalLogicalReads","Total Logical Reads","N0" },
                {"AvgLogicalReads","Avg Logical Reads","N0" },
                {"TotalPhysicalReads","TotalPhysicalReads" ,"N0"},
                {"AvgPhysicalReads","Avg Physical Reads" ,"N0"},
                {"TotalWrites","Total Writes" ,"N0"},
                {"AvgWrites","Avg Writes","N0" }

            };

        private void ObjectExecution_Load(object sender, EventArgs e)
        {
            Common.AddDateGroups(tsDateGroup, TsDateGrouping_Click);
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
            tsDateGroup.Text = Common.DateGroupString(dateGrouping);
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
            Close.Invoke(this, new EventArgs());
        }

        private void TsUp_Click(object sender, EventArgs e)
        {
            MoveUp.Invoke(this, new EventArgs());
        }

    }
}
