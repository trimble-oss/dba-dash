using DBADashGUI.Theme;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Data.SqlClient;
using SkiaSharp.Views.Desktop;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    public partial class MemoryUsage : UserControl, ISetContext, IRefreshData
    {
        public MemoryUsage()
        {
            InitializeComponent();
            ChartView = ChartViews.Pie; // Update control visibility
        }

        private int InstanceID;

        private bool isClerksRefreshed;
        private bool isConfigRefreshed;
        private bool isCountersRefreshed;
        private List<int> MemoryCounters;
        private int dateGrouping;
        private static readonly int MaxChartPoints = 1000;
        private string selectedClerk;
        private string selectedCounter;
        private string selectedCounterAlias;
        private readonly ToolTip dgvToolTip = new() { AutomaticDelay = 100, AutoPopDelay = 60000, ReshowDelay = 100 };
        private int previousDurationMins;

        private enum ChartViews
        {
            Pie,
            PerformanceCounter,
            MemoryClerk
        }

        private ChartViews ChartView
        {
            get
            {
                if (pieChart1.Visible)
                {
                    return ChartViews.Pie;
                }
                else if (performanceCounters1.Visible)
                {
                    return ChartViews.PerformanceCounter;
                }
                else
                {
                    return ChartViews.MemoryClerk;
                }
            }
            set
            {
                pieChart1.Visible = value == ChartViews.Pie;
                performanceCounters1.Visible = value == ChartViews.PerformanceCounter;
                chartClerk.Visible = value == ChartViews.MemoryClerk;
                tsDateGroup.Visible = value == ChartViews.MemoryClerk;
                tsAgg.Visible = value == ChartViews.MemoryClerk;
                tsPieChart.Enabled = value != ChartViews.Pie;
            }
        }

        public void SetContext(DBADashContext _context)
        {
            InstanceID = _context.InstanceID;
            RefreshData();
        }

        public void RefreshData()
        {
            isClerksRefreshed = false;
            isCountersRefreshed = false;
            isConfigRefreshed = false;
            ResetDateGroupingIfDurationChanged();
            RefreshCurrentTab();
            RefreshClerkLineChart();
            RefreshPerformanceChart();
        }

        private void ResetDateGroupingIfDurationChanged()
        {
            if (Math.Abs(DateRange.DurationMins - previousDurationMins) > 5)
            {
                dateGrouping = DateHelper.DateGrouping(DateRange.DurationMins, MaxChartPoints);
                tsDateGroup.Text = DateHelper.DateGroupString(dateGrouping);
                previousDurationMins = DateRange.DurationMins;
            }
        }

        private void RefreshCurrentTab()
        {
            if ((tab1.SelectedTab == tabClerks || ChartView == ChartViews.Pie) && !isClerksRefreshed)
            {
                RefreshClerks();
            }
            if (tab1.SelectedTab == tabConfig && !isConfigRefreshed)
            {
                RefreshConfig();
            }
            else if (tab1.SelectedTab == tabCounters && !isCountersRefreshed)
            {
                RefreshCounters();
            }
        }

        private void RefreshPerformanceChart()
        {
            if (ChartView == ChartViews.PerformanceCounter)
            {
                performanceCounters1.FromDate = DateRange.FromUTC;
                performanceCounters1.ToDate = DateRange.ToUTC;
                performanceCounters1.InstanceID = InstanceID;
                performanceCounters1.RefreshData();
            }
        }

        private void RefreshClerkLineChart()
        {
            if (ChartView == ChartViews.MemoryClerk)
            {
                ShowMemoryUsageForClerk();
            }
        }

        private void RefreshClerks()
        {
            var dt = GetMemoryUsage();
            dgv.AutoGenerateColumns = false;
            if (dgv.Columns.Count == 0)
            {
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "colMemoryClerkType", HeaderText = "Memory Clerk Type", DataPropertyName = "MemoryClerkType", ToolTipText = "Memory clerk type. Usage is captured from sys.dm_os_memory_clerks" });
                dgv.Columns.Add(new DataGridViewLinkColumn() { Name = "colPages", HeaderText = "Pages KB (Current)", DataPropertyName = "pages_kb", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, LinkColor = DashColors.LinkColor, SortMode = DataGridViewColumnSortMode.Automatic, ToolTipText = "Memory usage in KB for each memory clerk from the latest snapshot within the selected date range" });
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "colPagesPct", HeaderText = "Pages % (Current)", DataPropertyName = "Pct", DefaultCellStyle = new DataGridViewCellStyle() { Format = "P1" } });
                dgv.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "Pages KB (Max)", DataPropertyName = "max_pages_kb", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, LinkColor = DashColors.LinkColor, SortMode = DataGridViewColumnSortMode.Automatic, ToolTipText = "Max memory usage in KB for each memory clerk within the selected date range" });
                dgv.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "Pages KB (Avg)", DataPropertyName = "avg_pages_kb", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, LinkColor = DashColors.LinkColor, SortMode = DataGridViewColumnSortMode.Automatic, ToolTipText = "Average memory usage in KB for each memory clerk within the selected date range" });
                dgv.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "Pages KB (Min)", DataPropertyName = "min_pages_kb", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, LinkColor = DashColors.LinkColor, SortMode = DataGridViewColumnSortMode.Automatic, ToolTipText = "Minimum memory usage in KB for each memory clerk within the selected date range" });
                dgv.Columns.Add(new DataGridViewLinkColumn() { Name = "colVirtualMemoryCommitted", HeaderText = "Virtual Memory Committed KB (Current)", DataPropertyName = "virtual_memory_committed_kb", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, LinkColor = DashColors.LinkColor, SortMode = DataGridViewColumnSortMode.Automatic, ToolTipText = "Virtual memory committed in KB for each memory clerk from the latest snapshot within the selected date range" });
                dgv.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "Virtual Memory Committed KB (Max)", DataPropertyName = "max_virtual_memory_committed_kb", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, LinkColor = DashColors.LinkColor, Visible = false, SortMode = DataGridViewColumnSortMode.Automatic, ToolTipText = "Max virtual memory committed in KB for each memory clerk within the selected date range" });
                dgv.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "Virtual Memory Committed KB (Avg)", DataPropertyName = "avg_virtual_memory_committed_kb", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, LinkColor = DashColors.LinkColor, Visible = false, SortMode = DataGridViewColumnSortMode.Automatic, ToolTipText = "Avg virtual memory committed in KB for each memory clerk within the selected date range" });
                dgv.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "Virtual Memory Committed KB (Min)", DataPropertyName = "min_virtual_memory_committed_kb", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, LinkColor = DashColors.LinkColor, Visible = false, SortMode = DataGridViewColumnSortMode.Automatic, ToolTipText = "Min virtual memory committed in KB for each memory clerk within the selected date range" });
                dgv.Columns.Add(new DataGridViewLinkColumn() { Name = "colAWEAllocated", HeaderText = "AWE Allocated KB (Current)", DataPropertyName = "awe_allocated_kb", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, LinkColor = DashColors.LinkColor, SortMode = DataGridViewColumnSortMode.Automatic, ToolTipText = "AWE memory allocated in KB for each memory clerk from the latest snapshot within the selected date range" });
                dgv.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "AWE Allocated KB (Max)", DataPropertyName = "max_awe_allocated_kb", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, LinkColor = DashColors.LinkColor, Visible = false, SortMode = DataGridViewColumnSortMode.Automatic, ToolTipText = "Max AWE memory allocated in KB for each memory clerk within the selected date range" });
                dgv.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "AWE Allocated KB (Avg)", DataPropertyName = "avg_awe_allocated_kb", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, LinkColor = DashColors.LinkColor, Visible = false, SortMode = DataGridViewColumnSortMode.Automatic, ToolTipText = "Average AWE memory allocated in KB for each memory clerk within the selected date range" });
                dgv.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "AWE Allocated KB (Min)", DataPropertyName = "min_awe_allocated_kb", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, LinkColor = DashColors.LinkColor, Visible = false, SortMode = DataGridViewColumnSortMode.Automatic, ToolTipText = "Minimum AWE memory allocated in KB for each memory clerk within the selected date range" });
                dgv.Columns.Add(new DataGridViewLinkColumn() { Name = "colSharedMemoryReserved", HeaderText = "Shared Memory Reserved KB (Current)", DataPropertyName = "shared_memory_reserved_kb", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, LinkColor = DashColors.LinkColor, SortMode = DataGridViewColumnSortMode.Automatic, ToolTipText = "Shared memory reserved in KB for each memory clerk from the latest snapshot within the selected date range" });
                dgv.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "Shared Memory Reserved KB (Max)", DataPropertyName = "max_shared_memory_reserved_kb", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, LinkColor = DashColors.LinkColor, Visible = false, SortMode = DataGridViewColumnSortMode.Automatic, ToolTipText = "Max shared memory reserved in KB for each memory clerk within the selected date range" });
                dgv.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "Shared Memory Reserved KB (Avg)", DataPropertyName = "avg_shared_memory_reserved_kb", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, LinkColor = DashColors.LinkColor, Visible = false, SortMode = DataGridViewColumnSortMode.Automatic, ToolTipText = "Average shared memory reserved in KB for each memory clerk within the selected date range" });
                dgv.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "Shared Memory Reserved KB (Min)", DataPropertyName = "min_shared_memory_reserved_kb", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, LinkColor = DashColors.LinkColor, Visible = false, SortMode = DataGridViewColumnSortMode.Automatic, ToolTipText = "Minimum shared memory reserved in KB for each memory clerk within the selected date range" });
                dgv.Columns.Add(new DataGridViewLinkColumn() { Name = "colSharedMemoryCommitted", HeaderText = "Shared Memory Committed KB (Current)", DataPropertyName = "shared_memory_committed_kb", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, LinkColor = DashColors.LinkColor, SortMode = DataGridViewColumnSortMode.Automatic, ToolTipText = "Shared memory committed in KB for each memory clerk from the latest snapshot within the selected date range" });
                dgv.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "Shared Memory Committed KB (Max)", DataPropertyName = "max_shared_memory_committed_kb", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, LinkColor = DashColors.LinkColor, Visible = false, SortMode = DataGridViewColumnSortMode.Automatic, ToolTipText = "Max shared memory committed in KB for each memory clerk within the selected date range" });
                dgv.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "Shared Memory Committed KB (Avg)", DataPropertyName = "avg_shared_memory_committed_kb", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, LinkColor = DashColors.LinkColor, Visible = false, SortMode = DataGridViewColumnSortMode.Automatic, ToolTipText = "Average shared memory committed in KB for each memory clerk within the selected date range" });
                dgv.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "Shared Memory Committed KB (Min)", DataPropertyName = "min_shared_memory_committed_kb", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, LinkColor = DashColors.LinkColor, Visible = false, SortMode = DataGridViewColumnSortMode.Automatic, ToolTipText = "Minimum shared memory committed in KB for each memory clerk within the selected date range" });
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Snapshot Date", DataPropertyName = "SnapshotDate", ToolTipText = "Date/Time of the last memory usage collection (within the selected date range)" });
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "colDescription", HeaderText = "Description", DataPropertyName = "MemoryClerkDescription", ToolTipText = "Description of the memory clerk" });
                dgv.ApplyTheme();
            }
            dgv.DataSource = new DataView(dt);

            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            if (ChartView == ChartViews.Pie)
            {
                ShowPie(ref dt);
            }
            isClerksRefreshed = true;
        }

        private void ShowPie(ref DataTable dt)
        {
            var series = new List<ISeries>();
            double other = 0d;

            foreach (DataRow r in dt.Rows)
            {
                var pages = Convert.ToDouble(r["pages_kb"]);
                var pct = Convert.ToDouble(r["Pct"]);
                var name = (string)r["MemoryClerkType"];
                var showDataLabels = pct > 0.05;

                if (pct > 0.02)
                {
                    var s = new PieSeries<double>
                    {
                        Name = name,
                        Values = new[] { pages },
                        DataLabelsPaint = showDataLabels
                            ? new SolidColorPaint(DashColors.White.ToSKColor()) { StrokeThickness = 2 }
                            : null,
                        DataLabelsFormatter = point =>
                            showDataLabels
                                ? $"{point.Context.Series.Name} ({point.StackedValue?.Share:P2})"
                                : string.Empty,
                        DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                        ToolTipLabelFormatter = point => $"{point.StackedValue?.Share:P2}"
                    };
                    series.Add(s);
                }
                else
                {
                    other += pages;
                }
            }

            if (other > 0)
            {
                var s = new PieSeries<double>
                {
                    Name = "{Other}",
                    Values = new[] { other },
                    DataLabelsPaint = new SolidColorPaint(DashColors.White.ToSKColor()) { StrokeThickness = 2 },
                    DataLabelsFormatter = point => $"{point.Context.Series.Name} ({point.StackedValue?.Share:P2})",
                    DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                    ToolTipLabelFormatter = point => $"{point.StackedValue?.Share:P2}"
                };
                series.Add(s);
            }

            pieChart1.Series = series;
            pieChart1.LegendTextPaint = new SolidColorPaint(DBADashUser.SelectedTheme.ForegroundColor.ToSKColor());
            pieChart1.LegendPosition = LiveChartsCore.Measure.LegendPosition.Bottom;
        }

        public DataTable GetMemoryUsage()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.MemoryUsage_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cmd.Parameters.AddWithValue("InstanceID", InstanceID);
            cmd.Parameters.AddWithValue("FromDate", DateRange.FromUTC);
            cmd.Parameters.AddWithValue("ToDate", DateRange.ToUTC);
            var dt = new DataTable();
            da.Fill(dt);
            DateHelper.ConvertUTCToAppTimeZone(ref dt);
            return dt;
        }

        private void Dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
            selectedClerk = (string)row["MemoryClerkType"];
            var dataProperty = dgv.Columns[e.ColumnIndex].DataPropertyName.ToLower();
            switch (dataProperty)
            {
                case "pages_kb":
                case "max_pages_kb":
                case "avg_pages_kb":
                case "min_pages_kb":
                    selectedCounter = "pages_kb";
                    selectedCounterAlias = selectedClerk + " - Pages KB";
                    break;

                case "virtual_memory_committed_kb":
                case "max_virtual_memory_committed_kb":
                case "avg_virtual_memory_committed_kb":
                case "min_virtual_memory_committed_kb":
                    selectedCounter = "virtual_memory_committed_kb";
                    selectedCounterAlias = selectedClerk + " - Virtual Memory Committed KB";
                    break;

                case "awe_allocated_kb":
                case "max_awe_allocated_kb":
                case "avg_awe_allocated_kb":
                case "min_awe_allocated_kb":
                    selectedCounter = "awe_allocated_kb";
                    selectedCounterAlias = selectedClerk + " - AWE Allocated KB";
                    break;

                case "shared_memory_reserved_kb":
                case "max_shared_memory_reserved_kb":
                case "avg_shared_memory_reserved_kb":
                case "min_shared_memory_reserved_kb":
                    selectedCounter = "shared_memory_reserved_kb";
                    selectedCounterAlias = selectedClerk + " - Shared Memory Reserved KB";
                    break;

                case "shared_memory_committed_kb":
                case "max_shared_memory_committed_kb":
                case "avg_shared_memory_committed_kb":
                case "min_shared_memory_committed_kb":
                    selectedCounter = "shared_memory_committed_kb";
                    selectedCounterAlias = selectedClerk + " - Shared Memory Committed KB";
                    break;

                default:
                    return;
            }
            switch (dataProperty)
            {
                case var _ when dataProperty.StartsWith("avg_"):
                    SetAggregation(avgToolStripMenuItem);
                    break;

                case var _ when dataProperty.StartsWith("min_"):
                    SetAggregation(minToolStripMenuItem);
                    break;

                default:
                    SetAggregation(maxToolStripMenuItem);
                    break;
            }
            ShowMemoryUsageForClerk();
        }

        private void ShowMemoryUsageForClerk(string format = "N0")
        {
            if (string.IsNullOrEmpty(selectedClerk))
            {
                MessageBox.Show("Please select a memory clerk to view.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            ChartView = ChartViews.MemoryClerk;

            var dt = GetMemoryClerkUsage(selectedClerk, dateGrouping, tsAgg.Text, selectedCounter);
            if (dt.Rows.Count > MaxChartPoints)
            {
                MessageBox.Show("Max Chart points exceeded.  Please select a narrower date range or increase the date grouping.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var columns = new Dictionary<string, ColumnMetaData>
            {
                {selectedCounter, new ColumnMetaData{Name=selectedCounterAlias,IsVisible=true } }
            };
            chartClerk.Series = Array.Empty<ISeries>();
            chartClerk.XAxes = Array.Empty<Axis>();

            var labelPaint = DBADashUser.SelectedTheme.ThemeIdentifier == ThemeType.Dark
                ? new SolidColorPaint(DashColors.White.ToSKColor())
                : new SolidColorPaint(DashColors.TrimbleBlueDark.ToSKColor());
            var labelFontSize = DBADashUser.ChartAxisLabelFontSize;
            var nameFontSize = DBADashUser.ChartAxisNameFontSize;

            chartClerk.YAxes = new[]
            {
                new Axis
                {
                    MinLimit = 0,
                    Labeler = val => val.ToString(format),
                    LabelsPaint = labelPaint,
                    TextSize = labelFontSize,
                    NamePaint = labelPaint,
                    NameTextSize = nameFontSize,
                    TicksPaint = new SolidColorPaint(new SKColor(0xCC, 0xCC, 0xCC)),
                    SubticksPaint = new SolidColorPaint(new SKColor(0xE0, 0xE0, 0xE0))
                }
            };

            chartClerk.AddDataTable(dt, columns, "SnapshotDate", false);
            tsAgg.Enabled = dateGrouping > 0;
        }

        private DataTable GetMemoryClerkUsage(string clerk, int? dateGrouping, string agg, string measure)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.MemoryClerkUsage_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cmd.Parameters.AddWithValue("InstanceID", InstanceID);
            cmd.Parameters.AddWithValue("FromDate", DateRange.FromUTC);
            cmd.Parameters.AddWithValue("ToDate", DateRange.ToUTC);
            cmd.Parameters.AddWithValue("MemoryClerkType", clerk);
            cmd.Parameters.AddWithValue("Mins", dateGrouping);
            cmd.Parameters.AddWithValue("Agg", agg);
            cmd.Parameters.AddWithValue("Measure", measure);
            var dt = new DataTable();
            da.Fill(dt);
            DateHelper.ConvertUTCToAppTimeZone(ref dt);
            return dt;
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            dgv.ExportToExcel();
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            dgv.CopyGrid();
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void RefreshConfig()
        {
            var dt = GetMemoryConfig();
            dgvConfig.DataSource = new DataView(dt);
            dgvConfig.Columns[0].Width = 400;
            dgvConfig.Columns[1].Width = 150;
            isConfigRefreshed = true;
        }

        private void RefreshCounters()
        {
            if (MemoryCounters == null || MemoryCounters.Count == 0)
            {
                MemoryCounters = GetMemoryCounters();
            }
            performanceCounterSummaryGrid1.Counters = MemoryCounters;
            performanceCounterSummaryGrid1.InstanceID = InstanceID;
            performanceCounterSummaryGrid1.RefreshData();
        }

        private DataTable GetMemoryConfig()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.MemoryConfig_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cmd.Parameters.AddWithValue("InstanceID", InstanceID);
            var dt = new DataTable();
            da.Fill(dt);
            return dt;
        }

        private List<int> GetMemoryCounters()
        {
            var Counters = new List<int>();
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.MemoryCounters_Get", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
            cmd.Parameters.AddWithValue("InstanceID", InstanceID);
            using var rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                Counters.Add(rdr.GetInt32(0));
            }

            return Counters;
        }

        private void Tab1_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshCurrentTab();
        }

        private void Dgv_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var toolTip = (string)((DataRowView)dgv.Rows[e.RowIndex].DataBoundItem)["MemoryClerkDescription"];
                dgvToolTip.SetToolTip(dgv, toolTip);
            }
        }

        private void MemoryUsage_Load(object sender, EventArgs e)
        {
            performanceCounterSummaryGrid1.ObjectLink = false;
            performanceCounterSummaryGrid1.InstanceLink = false;
            performanceCounterSummaryGrid1.CounterLink = false;
            performanceCounterSummaryGrid1.CounterSelected += PerformanceCounterSummaryGrid1_CounterSelected;
            DateHelper.AddDateGroups(tsDateGroup, TsDateGroup_Click);
        }

        private void TsDateGroup_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            dateGrouping = Convert.ToInt32(ts.Tag);
            tsDateGroup.Text = DateHelper.DateGroupString(dateGrouping);
            previousDurationMins = DateRange.DurationMins;
            ShowMemoryUsageForClerk();
        }

        private void PerformanceCounterSummaryGrid1_CounterSelected(object sender, PerformanceCounterSummaryGrid.CounterSelectedEventArgs e)
        {
            performanceCounters1.CounterID = e.CounterID;
            performanceCounters1.CounterName = e.CounterName;
            ChartView = ChartViews.PerformanceCounter;
            RefreshPerformanceChart();
        }

        private void TsAGG_Click(object sender, EventArgs e)
        {
            SetAggregation(sender as ToolStripMenuItem);
            ShowMemoryUsageForClerk();
        }

        private void SetAggregation(ToolStripMenuItem menuItem)
        {
            avgToolStripMenuItem.Checked = avgToolStripMenuItem == menuItem;
            maxToolStripMenuItem.Checked = maxToolStripMenuItem == menuItem;
            minToolStripMenuItem.Checked = minToolStripMenuItem == menuItem;
            tsAgg.Text = menuItem.Text;
        }

        private void TsPieChart_Click(object sender, EventArgs e)
        {
            ChartView = ChartViews.Pie;
            RefreshCurrentTab();
        }
    }
}