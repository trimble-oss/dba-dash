using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Data.SqlClient;
using SkiaSharp;
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
        private int durationMins;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int PointSize { get; set; } = 0;

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

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool SmoothLines { get; set; } = true;

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

            RenderCpuChart();
        }

        private void RenderCpuChart()
        {
            if (CpuDataTable == null || CpuDataTable.Rows.Count == 0)
            {
                chartCPU.Series = Array.Empty<ISeries>();
                chartCPU.XAxes = Array.Empty<Axis>();
                chartCPU.YAxes = Array.Empty<Axis>();
                return;
            }

            var rows = CpuDataTable.Rows.Cast<DataRow>().ToList();

            // Build DateTimePoint collections so X uses a DateTime scale
            DateTimePoint[] CreatePoints(string columnName) =>
                [.. rows.Select(r => new DateTimePoint(
                        ((DateTime)r["EventTime"]).ToLocalTime(),
                        Convert.ToDouble(r[columnName])))];

            var sqlPoints = CreatePoints("SQLProcessCPU");
            var otherPoints = CreatePoints("OtherCPU");
            var maxPoints = CreatePoints("MaxCPU");

            ISeries[] series;

            if (Metric.AggregateType == IMetric.AggregateTypes.Avg)
            {
                // AVG -> stacked area for SQLProcessCPU and OtherCPU
                var areaSeriesSql = new StackedAreaSeries<DateTimePoint>
                {
                    Values = sqlPoints,
                    Name = "SQL Server",
                    GeometrySize = PointSize,
                    LineSmoothness = SmoothLines ? 1 : 0,
                    ScalesYAt = 0
                };

                var areaSeriesOther = new StackedAreaSeries<DateTimePoint>
                {
                    Values = otherPoints,
                    Name = "Other",
                    GeometrySize = PointSize,
                    LineSmoothness = SmoothLines ? 1 : 0,
                    ScalesYAt = 0
                };

                series = [areaSeriesSql, areaSeriesOther];
            }
            else
            {
                // MAX -> line series for MaxCPU
                var maxLine = new LineSeries<DateTimePoint>
                {
                    Values = maxPoints,
                    Name = "Max CPU",
                    GeometrySize = PointSize,
                    LineSmoothness = SmoothLines ? 1 : 0,
                    ScalesYAt = 0
                };

                series = [maxLine];
            }

            chartCPU.Series = series;

            chartCPU.XAxes = new[]
            {
                new DateTimeAxis(
                    TimeSpan.FromMinutes(DateGrouping == 0 ? 1 : DateGrouping),
                    date => date.ToString(DateRange.DateFormatString))
                {
                    LabelsRotation = 15,
                    LabelsPaint = new SolidColorPaint(new SKColor(0x99, 0x99, 0x99)),
                    NamePaint = new SolidColorPaint(new SKColor(0x99, 0x99, 0x99)),
                    TextSize = 14,
                    TicksPaint = new SolidColorPaint(new SKColor(0xCC, 0xCC, 0xCC)),
                    SubticksPaint = new SolidColorPaint(new SKColor(0xE0, 0xE0, 0xE0)),
                    Padding = new LiveChartsCore.Drawing.Padding(0, 0, 0, 0),
                }
            };

            chartCPU.YAxes = new[]
            {
                new Axis
                {
                    Labeler = value => $"{value:0}%",
                    MinLimit = 0,
                    MaxLimit = 100,
                    LabelsPaint = new SolidColorPaint(new SKColor(0x99, 0x99, 0x99)),
                    NamePaint = new SolidColorPaint(new SKColor(0x99, 0x99, 0x99)),
                    TicksPaint = new SolidColorPaint(new SKColor(0xCC, 0xCC, 0xCC)),
                    SubticksPaint = new SolidColorPaint(new SKColor(0xE0, 0xE0, 0xE0)),
                    TextSize = 14,
                }
            };
        }

        private void AVGToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Use AVG as the "Area" mode: stacked area for SQL/Other + line for Max
            AVGToolStripMenuItem.Checked = true;
            MAXToolStripMenuItem.Checked = false;
            Metric.AggregateType = IMetric.AggregateTypes.Avg;
            tsAgg.Text = "Area";
            RenderCpuChart();
        }

        private void MAXToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Use MAX as the "Line" mode: 3 line series (no stacking)
            MAXToolStripMenuItem.Checked = true;
            AVGToolStripMenuItem.Checked = false;
            Metric.AggregateType = IMetric.AggregateTypes.Max;
            tsAgg.Text = "Line";
            RenderCpuChart();
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
            Close?.Invoke(this, EventArgs.Empty);
        }

        private void TsUp_Click(object sender, EventArgs e)
        {
            MoveUp?.Invoke(this, EventArgs.Empty);
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