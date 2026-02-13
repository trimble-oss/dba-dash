using DBADashGUI.Changes;
using DBADashGUI.Charts;
using DBADashGUI.CustomReports;
using DBADashGUI.Theme;
using Humanizer;
using LiveChartsCore.SkiaSharpView.WinForms;
using Microsoft.Data.SqlClient;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    public partial class ResourceGovernorWorkloadGroupsMetrics : UserControl, IRefreshData, ISetContext, IMetricChart
    {
        private DBADashContext CurrentContext;
        private int DateGrouping => DateHelper.DateGrouping(DateRange.DurationMins, 200);

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool CloseVisible
        {
            get => tsClose.Visible; set => tsClose.Visible = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool MoveUpVisible
        {
            get => tsUp.Visible; set => tsUp.Visible = value;
        }

        private ResourceGovernorWorkloadGroupsMetric _metric = new();

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ResourceGovernorWorkloadGroupsMetric Metric
        {
            get => _metric;
            set
            {
                _metric = value;
                UpdateUIFromMetric();
            }
        }

        IMetric IMetricChart.Metric => Metric;

        private void UpdateUIFromMetric()
        {
            // Update checked state of metrics menu items
            foreach (ToolStripMenuItem item in tsMetrics.DropDownItems.OfType<ToolStripMenuItem>())
            {
                if (item.Tag is string metric)
                {
                    item.Checked = MetricsToDisplay.Contains(metric);
                }
            }

            // Update chart type icon
            UpdateChartTypeIcon();
        }

        private bool ShowTable
        {
            get => _metric.ShowTable;
            set => _metric.ShowTable = value;
        }

        private ChartTypes ChartType
        {
            get => _metric.ChartType;
            set => _metric.ChartType = value;
        }

        private List<string> MetricsToDisplay
        {
            get => _metric.MetricsToDisplay;
            set => _metric.MetricsToDisplay = value;
        }

        private Dictionary<string, SKColor> groupColors = new Dictionary<string, SKColor>();
        private bool isRefreshing = false;
        private CancellationTokenSource colorExtractionCts;
        private CustomReportResult reportResult = ResourceGovernorWorkloadGroupsReport.GetReportResult();

        public ResourceGovernorWorkloadGroupsMetrics()
        {
            InitializeComponent();
            InitializeMetricsMenu();
            UpdateChartTypeIcon();
        }

        private List<string> MetricsToLoad = new List<string>()
        {
            "cpu_cores",
            "cpu_percent",
            "cpu_share_percent",
            "requests_per_min",
            "queued_request_count_per_min",
            "cpu_limit_violations_per_min",
            "lock_waits_per_min",
            "lock_wait_time_ms_per_sec",
            "query_optimizations_per_min",
            "suboptimal_plan_generation_count_per_min",
            "reduced_memgrant_count_per_min",
            "cpu_usage_preemptive_ms_per_min",
            "tempdb_data_limit_violations_per_min",
            "avg_active_request_count",
            "avg_queued_request_count",
            "avg_blocked_task_count",
            "avg_active_parallel_thread_count",
            "avg_tempdb_data_space_kb"
        };

        public event EventHandler<EventArgs> Close;

        public event EventHandler<EventArgs> MoveUp;

        public async void RefreshData()
        {
            // Prevent concurrent refreshes
            if (isRefreshing) return;
            isRefreshing = true;

            try
            {
                // Properly dispose existing controls before clearing
                DisposeExistingControls();

                var metricsDT = await GetResourceGovernorWorkloadGroupsMetrics();
                // Determine total number of rows (charts + optional table)
                var rowCount = MetricsToDisplay.Count + (ShowTable ? 1 : 0);
                if (rowCount == 0)
                {
                    // Nothing to display, return
                    return;
                }
                // Create a TableLayoutPanel for vertical layout
                var tableLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = rowCount
                };

                // Configure column and rows
                tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

                float rowPercent = 100F / rowCount;
                CartesianChart firstChart = null;
                for (int i = 0; i < MetricsToDisplay.Count; i++)
                {
                    tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, rowPercent));
                    var metric = MetricsToDisplay[i];
                    var y_format = metric.Contains("percent", StringComparison.OrdinalIgnoreCase) ? "P1" : null;
                    var y_max = metric.Contains("percent", StringComparison.OrdinalIgnoreCase) ? 1 : (double?)null;
                    // Create chart for each metric
                    var chart = ChartHelper.GetChartFromDataTable(metricsDT,
                        new ChartConfiguration()
                        {
                            DateColumn = "SnapshotDate",
                            YAxisFormat = y_format,
                            YAxisMax = y_max,
                            ChartType = ChartType,
                            YAxisLabel = FormatMetricName(metric),
                            SeriesColumn = "name",
                            MetricColumn = metric,
                            XAxisMin = DateRange.FromUTC,
                            XAxisMax = DateRange.ToUTC
                        }
                     );
                    chart.ApplyTheme();
                    tableLayout.Controls.Add(chart, 0, i);

                    // Keep reference to first chart for color extraction
                    if (i == 0)
                    {
                        firstChart = chart;
                    }
                }
                if (ShowTable)
                {
                    var dt = await GetResourceGovernorWorkloadGroups();
                    tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, rowPercent));
                    var dgv = new DBADashDataGridView()
                    {
                        Dock = DockStyle.Fill,
                        AllowUserToAddRows = false,
                        ReadOnly = true,
                        RowHeadersVisible = false,
                    };

                    dgv.Columns.Add(new DataGridViewTextBoxColumn()
                    {
                        Name = "Color",
                        HeaderText = "",
                        Width = 30,
                        ReadOnly = true,
                        SortMode = DataGridViewColumnSortMode.NotSortable,
                        DisplayIndex = 0,
                    });
                    dgv.AddColumns(dt, reportResult);

                    dgv.DataSource = dt;
                    dgv.RowsAdded += Dgv_RowsAdded;
                    dgv.CellFormatting += DataGridView_CellFormatting;
                    dgv.DataBindingComplete += DataGridView_DataBindingComplete;
                    dgv.ApplyTheme();

                    tableLayout.Controls.Add(dgv, 0, MetricsToDisplay.Count);
                }

                // Add the layout panel to the control
                pnlCharts.Controls.Add(tableLayout);

                // Extract colors after a short delay to allow chart rendering
                if (firstChart != null)
                {
                    colorExtractionCts?.Cancel();
                    colorExtractionCts?.Dispose();
                    colorExtractionCts = new CancellationTokenSource();
                    var token = colorExtractionCts.Token;

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await ChartHelper.WaitForColorsToBeReady(firstChart, token);
                            if (token.IsCancellationRequested) return;

                            var colors = ChartHelper.ExtractColorsFromChart(firstChart);

                            if (!token.IsCancellationRequested && !IsDisposed)
                            {
                                Invoke(new Action(() =>
                                {
                                    groupColors = colors;
                                    // Refresh the DataGridView to show the colors if we got any
                                    if (ShowTable && groupColors.Count > 0 && tableLayout.Controls.Count > MetricsToDisplay.Count &&
                                        tableLayout.Controls[MetricsToDisplay.Count] is DataGridView dgv)
                                    {
                                        dgv.Invalidate();
                                    }
                                }));
                            }
                        }
                        catch (OperationCanceledException) { }
                        catch (Exception)
                        {
                            // Silently ignore - color extraction is non-critical visual enhancement
                        }
                    }, token);
                }
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error refreshing chart");
            }
            finally
            {
                isRefreshing = false;
            }
        }

        private static string FormatMetricName(string metric)
        {
            // Convert snake_case to Title Case
            return metric.Replace("_", " ").Humanize(LetterCasing.Title);
        }

        private void InitializeMetricsMenu()
        {
            // Clear existing items
            tsMetrics.DropDownItems.Clear();

            // Add a menu item for each metric
            foreach (var metric in MetricsToLoad)
            {
                var menuItem = new ToolStripMenuItem
                {
                    Text = FormatMetricName(metric),
                    CheckOnClick = true,
                    Checked = MetricsToDisplay.Contains(metric),
                    Tag = metric // Store the metric name for reference
                };
                menuItem.Click += MetricMenuItem_Click;
                tsMetrics.DropDownItems.Add(menuItem);
            }
        }

        private void MetricMenuItem_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem menuItem && menuItem.Tag is string metric)
            {
                if (menuItem.Checked)
                {
                    // Add metric if not already in the list
                    if (!MetricsToDisplay.Contains(metric))
                    {
                        MetricsToDisplay.Add(metric);
                    }
                }
                else
                {
                    // Remove metric from the list
                    MetricsToDisplay.Remove(metric);
                }

                // Refresh the charts to reflect the changes
                RefreshData();
            }
        }

        private void Dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            if (sender is not DBADashDataGridView dgv) return;
            reportResult?.CellHighlightingRules.FormatRowsAdded(dgv, e);
        }

        private void DataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            // Only format the Color column (first column, index 0)
            if (e.ColumnIndex == 0 && e.RowIndex >= 0)
            {
                var dgv = sender as DataGridView;
                if (dgv != null && dgv.Rows[e.RowIndex].DataBoundItem is DataRowView rowView)
                {
                    var groupName = rowView["name"]?.ToString();

                    if (!string.IsNullOrEmpty(groupName) && groupColors.ContainsKey(groupName))
                    {
                        var skColor = groupColors[groupName];
                        var color = System.Drawing.Color.FromArgb(skColor.Alpha, skColor.Red, skColor.Green, skColor.Blue);

                        // Simply set the cell's background color
                        e.CellStyle.BackColor = color;
                        e.CellStyle.SelectionBackColor = color;
                    }
                }
            }
        }

        private void DataGridView_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            if (sender is DataGridView dgv)
            {
                // Delay the resize to allow the DataGridView to fully render
                dgv.BeginInvoke(new Action(() =>
                {
                    dgv.LoadColumnLayout(reportResult.ColumnLayout);
                    dgv.Columns["Color"].DisplayIndex = 0;
                    dgv.AutoResizeColumnsWithMaxColumnWidth();
                    dgv.AutoResizeColumnHeadersHeight();
                }));
            }
        }

        public async Task<DataTable> GetResourceGovernorWorkloadGroups()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.ResourceGovernorWorkloadGroups_Get", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = Config.DefaultCommandTimeout };
            cmd.Parameters.AddWithValue("@InstanceID", CurrentContext.InstanceID);
            cmd.Parameters.AddWithValue("@FromDate", DateRange.FromUTC);
            cmd.Parameters.AddWithValue("@ToDate", DateRange.ToUTC);
            await cn.OpenAsync();
            var dt = new DataTable();
            using var rdr = await cmd.ExecuteReaderAsync();
            dt.Load(rdr);
            DateHelper.ConvertUTCToAppTimeZone(ref dt);
            return dt;
        }

        public async Task<DataTable> GetResourceGovernorWorkloadGroupsMetrics()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.ResourceGovernorWorkloadGroupsMetrics_Get", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = Config.DefaultCommandTimeout };
            cmd.Parameters.AddWithValue("@InstanceID", CurrentContext.InstanceID);
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
            await cn.OpenAsync();
            using var rdr = await cmd.ExecuteReaderAsync();

            var dt = new DataTable();
            dt.Load(rdr);
            return dt;
        }

        public void SetContext(DBADashContext _context)
        {
            CurrentContext = _context;
            RefreshData();
        }

        private void Refresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void DisposeExistingControls()
        {
            // Create a snapshot to avoid modifying collection during iteration
            var controlsToDispose = pnlCharts.Controls.Cast<Control>().ToArray();

            // Dispose all controls properly before clearing
            foreach (Control control in controlsToDispose)
            {
                if (control is TableLayoutPanel tableLayout)
                {
                    // Create snapshot of child controls
                    var childControls = tableLayout.Controls.Cast<Control>().ToArray();

                    // Dispose controls within the table layout
                    foreach (Control childControl in childControls)
                    {
                        if (childControl is DataGridView dgv)
                        {
                            // Remove event handlers to prevent memory leaks
                            dgv.RowsAdded -= Dgv_RowsAdded;
                            dgv.CellFormatting -= DataGridView_CellFormatting;
                            dgv.DataBindingComplete -= DataGridView_DataBindingComplete;
                        }
                        childControl.Dispose();
                    }
                    tableLayout.Dispose();
                }
                else
                {
                    control.Dispose();
                }
            }
            pnlCharts.Controls.Clear();
        }

        private void SelectChartType_Click(object sender, EventArgs e)
        {
            foreach (ToolStripMenuItem item in tsChartType.DropDownItems)
            {
                item.Checked = item == sender;
            }
            var strChartType = (string)(sender as ToolStripMenuItem).Tag;
            ChartType = Enum.Parse<ChartTypes>(strChartType);
            UpdateChartTypeIcon();
            RefreshData();
        }

        private void UpdateChartTypeIcon()
        {
            switch (ChartType)
            {
                case ChartTypes.Line:
                    tsChartType.Image = Properties.Resources.LineChart_16x;
                    break;

                case ChartTypes.StackedArea:
                    tsChartType.Image = Properties.Resources.StackedAreaChart;
                    break;

                case ChartTypes.StackedColumn:
                    tsChartType.Image = Properties.Resources.StackedColumnChart_24x;
                    break;

                default:
                    tsChartType.Image = Properties.Resources.StackedAreaChart;
                    break;
            }
        }

        public void RefreshData(int InstanceID)
        {
            CurrentContext = CommonData.GetDBADashContext(InstanceID);
            RefreshData();
        }

        private void Close_Click(object sender, EventArgs e)
        {
            Close?.Invoke(this, EventArgs.Empty);
        }

        private void MoveUp_Click(object sender, EventArgs e)
        {
            MoveUp?.Invoke(this, EventArgs.Empty);
        }

        private void ShowHideTable_Click(object sender, EventArgs e)
        {
            ShowTable = !ShowTable;
            RefreshData();
        }

        private async void ViewConfig_Click(object sender, EventArgs e)
        {
            var frm = new Form()
            {
                Text = "Resource Governor Configuration",
                Width = 1300,
                Height = 600,
                StartPosition = FormStartPosition.CenterParent
            };
            var rg = new ResourceGovernor()
            {
                Dock = DockStyle.Fill,
            };
            rg.SetContext(CurrentContext);
            frm.Controls.Add(rg);
            await frm.ShowSingleInstanceAsync();
        }
    }
}