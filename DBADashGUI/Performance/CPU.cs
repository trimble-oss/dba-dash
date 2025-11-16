using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using Microsoft.Data.SqlClient;
using System;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using UserControl = System.Windows.Forms.UserControl;

namespace DBADashGUI.Performance
{
    public partial class CPU : UserControl, IMetricChart
    {
        public CPU()
        {
            InitializeComponent();
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool CloseVisible
        {
            get => tsClose.Visible; set => tsClose.Visible = value;
        }

        public event EventHandler<EventArgs> Close;

        public event EventHandler<EventArgs> MoveUp;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int InstanceID { get; set; }

        private int DateGrouping;
        private bool smoothLines = true;
        private int durationMins;

        public int PointSize;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool SmoothLines
        {
            get => smoothLines;
            set
            {
                smoothLines = value;
                foreach (Series s in chartCPU.Series.Cast<Series>())
                {
                    if (s.GetType() == typeof(LineSeries))
                    {
                        ((LineSeries)s).LineSmoothness = smoothLines ? 1 : 0;
                    }
                    else if (s.GetType() == typeof(StackedAreaSeries))
                    {
                        ((StackedAreaSeries)s).LineSmoothness = smoothLines ? 1 : 0;
                    }
                }
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool MoveUpVisible
        {
            get => tsUp.Visible; set => tsUp.Visible = value;
        }

        private CPUMetric _metric = new() { AggregateType = IMetric.AggregateTypes.Avg };

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public CPUMetric Metric
        { get => _metric; set { _metric = value; SelectAggregate(); } }

        IMetric IMetricChart.Metric => Metric;

        private void SelectAggregate()
        {
            tsAgg.Text = Enum.GetName(Metric.AggregateType);
            foreach (ToolStripMenuItem mnu in tsAgg.DropDownItems)
            {
                mnu.Checked = (string)mnu.Tag == Enum.GetName(Metric.AggregateType);
            }
        }

        public void RefreshData(int InstanceID)
        {
            this.InstanceID = InstanceID;
            RefreshData();
        }

        private async Task<DataTable> GetCpuDataTable()
        {
            await using var cn = new SqlConnection(Common.ConnectionString);
            await using var cmd = new SqlCommand("dbo.CPU_Get", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = Config.DefaultCommandTimeout };
            await cn.OpenAsync();
            cmd.Parameters.AddWithValue("@InstanceID", InstanceID);
            cmd.Parameters.AddWithValue("@FromDate", DateRange.FromUTC);
            cmd.Parameters.AddWithValue("@ToDate", DateRange.ToUTC);
            cmd.Parameters.AddWithValue("@DateGroupingMin", DateGrouping);
            cmd.Parameters.AddWithValue("@UTCOffset", DateHelper.UtcOffset);
            if (DateRange.HasTimeOfDayFilter)
            {
                cmd.Parameters.AddWithValue("Hours", DateRange.TimeOfDay.AsDataTable());
            }

            if (DateRange.HasDayOfWeekFilter)
            {
                cmd.Parameters.AddWithValue("DaysOfWeek", DateRange.DayOfWeek.AsDataTable());
            }

            await using var rdr = await cmd.ExecuteReaderAsync();

            var dt = new DataTable();
            dt.Load(rdr);
            return dt;
        }

        private DataTable CpuDataTable;

        public async void RefreshData()
        {
            if (durationMins != DateRange.DurationMins) // Update date grouping only if date range has changed
            {
                DateGrouping = DateHelper.DateGrouping(DateRange.DurationMins, 200);
                tsDateGrouping.Text = DateHelper.DateGroupString(DateGrouping);
                durationMins = DateRange.DurationMins;
            }

            CpuDataTable = await GetCpuDataTable();

            var sqlProcessValues = new ChartValues<DateTimePoint>();
            var otherValues = new ChartValues<DateTimePoint>();
            var maxValues = new ChartValues<DateTimePoint>();

            foreach (DataRow row in CpuDataTable.Rows)
            {
                var eventTime = (DateTime)row["EventTime"];
                sqlProcessValues.Add(new DateTimePoint(eventTime.ToAppTimeZone(), decimal.ToDouble((decimal)row["SQLProcessCPU"]) / 100.0));
                otherValues.Add(new DateTimePoint(eventTime.ToAppTimeZone(), decimal.ToDouble((decimal)row["OtherCPU"]) / 100.0));
                maxValues.Add(new DateTimePoint(eventTime.ToAppTimeZone(), decimal.ToDouble((decimal)row["MaxCPU"]) / 100.0));
            }

            SeriesCollection s1 = new()
                {
                    new StackedAreaSeries
                    {
                        Title="SQL Process",
                        Values = sqlProcessValues,
                        LineSmoothness = SmoothLines ? 1 : 0
                    },
                    new StackedAreaSeries
                    {
                    Title = "Other",
                    Values = otherValues,
                    LineSmoothness = SmoothLines ? 1 : 0
                    },
                    new LineSeries
                    {
                    Title = "Max CPU",
                    Values = maxValues,
                    LineSmoothness = SmoothLines ? 1 : 0,
                    PointGeometrySize = PointSize,
                    }
                };
            chartCPU.AxisX.Clear();
            chartCPU.AxisY.Clear();

            chartCPU.AxisX.Add(new Axis
            {
                LabelFormatter = val => new DateTime((long)val).ToString(DateRange.DateFormatString)
            });
            chartCPU.AxisY.Add(new Axis
            {
                LabelFormatter = val => val.ToString("P1"),
                MaxValue = 1,
                MinValue = 0,
            });
            if (maxValues.Count > 1)
            {
                chartCPU.Series = s1;
            }
            else
            {
                chartCPU.Series.Clear();
            }
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            if (chartCPU.Series.Count == 3)
            {
                ((StackedAreaSeries)chartCPU.Series[0]).Visibility = AVGToolStripMenuItem.Checked ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
                ((StackedAreaSeries)chartCPU.Series[1]).Visibility = AVGToolStripMenuItem.Checked ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
                ((LineSeries)chartCPU.Series[2]).Visibility = MAXToolStripMenuItem.Checked ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
            }
        }

        private void AVGToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MAXToolStripMenuItem.Checked = (!AVGToolStripMenuItem.Checked);
            Metric.AggregateType = AVGToolStripMenuItem.Checked ? IMetric.AggregateTypes.Avg : IMetric.AggregateTypes.Max;
            tsAgg.Text = Enum.GetName(Metric.AggregateType);
            UpdateVisibility();
        }

        private void MAXToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AVGToolStripMenuItem.Checked = (!MAXToolStripMenuItem.Checked);
            Metric.AggregateType = AVGToolStripMenuItem.Checked ? IMetric.AggregateTypes.Avg : IMetric.AggregateTypes.Max;
            tsAgg.Text = Enum.GetName(Metric.AggregateType);
            UpdateVisibility();
        }

        private void CPU_Load(object sender, EventArgs e)
        {
            DateHelper.AddDateGroups(tsDateGrouping, TsDateGrouping_Click);
        }

        private void TsDateGrouping_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            DateGrouping = Convert.ToInt32(ts.Tag);
            tsDateGrouping.Text = DateHelper.DateGroupString(DateGrouping);
            RefreshData();
        }

        private void TsClose_Click(object sender, EventArgs e)
        {
            Close.Invoke(this, EventArgs.Empty);
        }

        private void TsUp_Click(object sender, EventArgs e)
        {
            MoveUp.Invoke(this, EventArgs.Empty);
        }

        private void CopyData_Click(object sender, EventArgs e)
        {
            if (CpuDataTable is null)
            {
                MessageBox.Show("Nothing to copy", "Copy Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Common.CopyDataTableToClipboard(CpuDataTable);
        }

        private void ExportDataToExcel_Click(object sender, EventArgs e)
        {
            if (CpuDataTable is null)
            {
                MessageBox.Show("Nothing to export", "Export Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Common.PromptSaveDataTableToXLSX(CpuDataTable);
        }
    }
}