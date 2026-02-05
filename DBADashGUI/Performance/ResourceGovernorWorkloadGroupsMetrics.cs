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
                        AutoGenerateColumns = false,
                        AllowUserToAddRows = false,
                        ReadOnly = true,
                        RowHeadersVisible = false,
                    };

                    // Add columns explicitly
                    dgv.Columns.AddRange(
                        new DataGridViewTextBoxColumn() { Name = "ColorColumn", HeaderText = "", Width = 30, ReadOnly = true },
                        new DataGridViewTextBoxColumn() { HeaderText = "Instance ID", DataPropertyName = "InstanceID", Visible = false },
                        new DataGridViewTextBoxColumn() { HeaderText = "Name", DataPropertyName = "name", ToolTipText = "Resource governor workload group name" },
                        new DataGridViewTextBoxColumn() { HeaderText = "Pool", DataPropertyName = "pool_name", ToolTipText = "Associated resource governor resource pool" },
                        new DataGridViewTextBoxColumn() { HeaderText = "Group ID", DataPropertyName = "group_id", Visible = false, ToolTipText = "ID of the workload group" },
                        new DataGridViewTextBoxColumn() { HeaderText = "Pool ID", DataPropertyName = "pool_id", Visible = false, ToolTipText = "ID of the resource pool" },
                        new DataGridViewTextBoxColumn() { HeaderText = "External Pool ID", DataPropertyName = "external_pool_id", Visible = false, ToolTipText = "ID of the external resource pool" },
                        new DataGridViewTextBoxColumn() { HeaderText = "Period CPU Usage (ms)", DataPropertyName = "period_cpu_usage_ms", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, ToolTipText = "Total CPU consumption in ms within the selected date range" },
                        new DataGridViewTextBoxColumn() { HeaderText = "Period CPU Cores", DataPropertyName = "period_cpu_cores", DefaultCellStyle = Common.DataGridViewNumericCellStyle, ToolTipText = "Average CPU cores used within the selected date range" },
                        new DataGridViewTextBoxColumn() { HeaderText = "Period CPU Percent", DataPropertyName = "period_cpu_percent", DefaultCellStyle = Common.DataGridViewPercentCellStyle, ToolTipText = "Percent of total CPU capacity used within the selected date range" },
                        new DataGridViewTextBoxColumn() { HeaderText = "Period CPU Share Percent", DataPropertyName = "period_cpu_share_percent", DefaultCellStyle = Common.DataGridViewPercentCellStyle, ToolTipText = "Percent of CPU used in relation to other workload groups within the selected date range" },
                        new DataGridViewTextBoxColumn() { HeaderText = "Period Requests Per Min", DataPropertyName = "period_requests_per_min", DefaultCellStyle = Common.DataGridViewNumericCellStyle, ToolTipText = "Count of requests completed per minute within the selected date range" },
                        new DataGridViewTextBoxColumn() { HeaderText = "Period Queued Request Count Per Min", DataPropertyName = "period_queued_request_count_per_min", DefaultCellStyle = Common.DataGridViewNumericCellStyle, ToolTipText = "Count of queued requests per minute within the selected date range. A non-zero value means the GROUP_MAX_REQUESTS limit was reached." },
                        new DataGridViewTextBoxColumn() { HeaderText = "Period CPU Limit Violations Per Min", DataPropertyName = "period_cpu_limit_violations_per_min", DefaultCellStyle = Common.DataGridViewNumericCellStyle, ToolTipText = "Count of requests exceeding the CPU limit per minute within the selected date range." },
                        new DataGridViewTextBoxColumn() { HeaderText = "Period Lock Waits Per Min", DataPropertyName = "period_lock_waits_per_min", DefaultCellStyle = Common.DataGridViewNumericCellStyle, ToolTipText = "Count of lock waits that occurred per minute within the selected date range" },
                        new DataGridViewTextBoxColumn() { HeaderText = "Period Lock Wait Time MS Per Sec", DataPropertyName = "period_lock_wait_time_ms_per_sec", DefaultCellStyle = Common.DataGridViewNumericCellStyle, ToolTipText = "Lock wait time in milliseconds per second within the selected date range" },
                        new DataGridViewTextBoxColumn() { HeaderText = "Period Query Optimizations Per Min", DataPropertyName = "period_query_optimizations_per_min", DefaultCellStyle = Common.DataGridViewNumericCellStyle, ToolTipText = "Count of query optimizations within the selected date range" },
                        new DataGridViewTextBoxColumn() { HeaderText = "Period Suboptimal Plan Generation Count Per Min", DataPropertyName = "period_suboptimal_plan_generation_count_per_min", DefaultCellStyle = Common.DataGridViewNumericCellStyle, ToolTipText = "Count of suboptimal plan generations that occurred due to memory pressure within the selected date range." },
                        new DataGridViewTextBoxColumn() { HeaderText = "Period Reduced Memgrant Count Per Min", DataPropertyName = "period_reduced_memgrant_count_per_min", DefaultCellStyle = Common.DataGridViewNumericCellStyle, ToolTipText = "Count of memory grants that reached the per-request memory grant size within the selected date range." },
                        new DataGridViewTextBoxColumn() { HeaderText = "Period CPU Usage Preemptive MS Per Min", DataPropertyName = "period_cpu_usage_preemptive_ms_per_min", DefaultCellStyle = Common.DataGridViewNumericCellStyle, ToolTipText = "Period CPU time used while in preemptive mode scheduling in milliseconds per minute (e.g. extended stored procedures, distributed queries)" },
                        new DataGridViewTextBoxColumn() { HeaderText = "Period Tempdb Data Limit Violations Per Min", DataPropertyName = "period_tempdb_data_limit_violations_per_min", DefaultCellStyle = Common.DataGridViewNumericCellStyle, ToolTipText = "The number of queries aborted per minute because they exceeded the limit on tempdb space for the workload group" },
                        new DataGridViewTextBoxColumn() { HeaderText = "Period Avg Active Request Count", DataPropertyName = "period_avg_active_request_count", DefaultCellStyle = Common.DataGridViewNumericCellStyle, ToolTipText = "Average 'active_request_count' for the selected date range." },
                        new DataGridViewTextBoxColumn() { HeaderText = "Period Avg Queued Request Count", DataPropertyName = "period_avg_queued_request_count", DefaultCellStyle = Common.DataGridViewNumericCellStyle, ToolTipText = "Average 'queued_request_count' for the selected date range." },
                        new DataGridViewTextBoxColumn() { HeaderText = "Period Avg Blocked Task Count", DataPropertyName = "period_avg_blocked_task_count", DefaultCellStyle = Common.DataGridViewNumericCellStyle, ToolTipText = "Average 'blocked_task_count' for the selected date range." },
                        new DataGridViewTextBoxColumn() { HeaderText = "Period Avg Active Parallel Thread Count", DataPropertyName = "period_avg_active_parallel_thread_count", DefaultCellStyle = Common.DataGridViewNumericCellStyle, ToolTipText = "Average 'avg_active_parallel_thread_count' for the selected date range." },
                        new DataGridViewTextBoxColumn() { HeaderText = "Period Avg Tempdb Data Space KB", DataPropertyName = "period_avg_tempdb_data_space_kb", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, ToolTipText = "Average 'avg_tempdb_data_space_kb' for the selected date range." },
                        new DataGridViewTextBoxColumn() { HeaderText = "Statistics Start Time", DataPropertyName = "statistics_start_time", DefaultCellStyle = Common.DataGridViewDateCellStyle, ToolTipText = "The time when statistics collection for the workload group started" },
                        new DataGridViewTextBoxColumn() { HeaderText = "Snapshot Date", DataPropertyName = "SnapshotDate", DefaultCellStyle = Common.DataGridViewDateCellStyle, ToolTipText = "Date/Time of last collection from sys.dm_resource_governor_workload_groups" },
                        new DataGridViewTextBoxColumn() { HeaderText = "Total Request Count", DataPropertyName = "total_request_count", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, ToolTipText = "Cumulative count of completed requests in the workload group" },
                        new DataGridViewTextBoxColumn() { HeaderText = "Total Queued Request Count", DataPropertyName = "total_queued_request_count", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, ToolTipText = "Cumulative count of requests queued after the GROUP_MAX_REQUESTS limit was reached" },
                        new DataGridViewTextBoxColumn() { HeaderText = "Active Request Count", DataPropertyName = "active_request_count", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, ToolTipText = "Current request count" },
                        new DataGridViewTextBoxColumn() { HeaderText = "Queued Request Count", DataPropertyName = "queued_request_count", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, ToolTipText = "Current queued request count" },
                        new DataGridViewTextBoxColumn() { HeaderText = "Total CPU Limit Violation Count", DataPropertyName = "total_cpu_limit_violation_count", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, ToolTipText = "Cumulative count of requests exceeding the CPU limit" },
                        new DataGridViewTextBoxColumn() { HeaderText = "Total CPU Usage (ms)", DataPropertyName = "total_cpu_usage_ms", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, ToolTipText = "Cumulative CPU usage in milliseconds by this workload group" },
                        new DataGridViewTextBoxColumn() { HeaderText = "Max Request CPU Time MS", DataPropertyName = "max_request_cpu_time_ms", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, ToolTipText = "Maximum CPU usage in milliseconds for a single request. This is a measured value, unlike request_max_cpu_time_sec which is a configurable setting." },
                        new DataGridViewTextBoxColumn() { HeaderText = "Blocked Task Count", DataPropertyName = "blocked_task_count", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, ToolTipText = "Current count of blocked tasks" },
                        new DataGridViewTextBoxColumn() { HeaderText = "Total Lock Wait Count", DataPropertyName = "total_lock_wait_count", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, ToolTipText = "Cumulative count of lock waits that occurred" },
                        new DataGridViewTextBoxColumn() { HeaderText = "Total Lock Wait Time MS", DataPropertyName = "total_lock_wait_time_ms", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, ToolTipText = "Cumulative sum of elapsed time in milliseconds that a lock is held" },
                        new DataGridViewTextBoxColumn() { HeaderText = "Total Query Optimization Count", DataPropertyName = "total_query_optimization_count", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, ToolTipText = "Cumulative count of query optimizations in this workload group" },
                        new DataGridViewTextBoxColumn() { HeaderText = "Total Suboptimal Plan Generation Count", DataPropertyName = "total_suboptimal_plan_generation_count", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, ToolTipText = "Cumulative count of suboptimal plan generations that occurred in this workload group due to memory pressure" },
                        new DataGridViewTextBoxColumn() { HeaderText = "Total Reduced Memgrant Count", DataPropertyName = "total_reduced_memgrant_count", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, ToolTipText = "Cumulative count of memory grants that reached the maximum limit on the per-request memory grant size" },
                        new DataGridViewTextBoxColumn() { HeaderText = "Max Request Grant Memory KB", DataPropertyName = "max_request_grant_memory_kb", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, ToolTipText = "Maximum memory grant size in kilobytes of a single request since the statistics were reset" },
                        new DataGridViewTextBoxColumn() { HeaderText = "Active Parallel Thread Count", DataPropertyName = "active_parallel_thread_count", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, ToolTipText = "Current count of parallel thread usage" },
                        new DataGridViewTextBoxColumn() { HeaderText = "Importance", DataPropertyName = "importance", ToolTipText = "Current configuration value for the relative importance of a request in this workload group. Importance is one of the following: Low, Medium, or High (default is Medium)" },
                        new DataGridViewTextBoxColumn() { HeaderText = "Request Max Memory Grant Percent", DataPropertyName = "request_max_memory_grant_percent", DefaultCellStyle = Common.DataGridViewWholeNumberPercentCellStyle, ToolTipText = "Current setting for the maximum memory grant as a percentage for a single request" },
                        new DataGridViewTextBoxColumn() { HeaderText = "Request Max CPU Time Sec", DataPropertyName = "request_max_cpu_time_sec", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, ToolTipText = "Current setting for maximum CPU use limit in seconds for a single request" },
                        new DataGridViewTextBoxColumn() { HeaderText = "Request Memory Grant Timeout Sec", DataPropertyName = "request_memory_grant_timeout_sec", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, ToolTipText = "Current setting for memory grant timeout in seconds for a single request" },
                        new DataGridViewTextBoxColumn() { HeaderText = "Group Max Requests", DataPropertyName = "group_max_requests", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, ToolTipText = "Current setting for the maximum number of concurrent requests in the workload group" },
                        new DataGridViewTextBoxColumn() { HeaderText = "Max DOP", DataPropertyName = "max_dop", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, ToolTipText = "Configured maximum degree of parallelism for the workload group. The default value 0 uses global settings." },
                        new DataGridViewTextBoxColumn() { HeaderText = "Effective Max DOP", DataPropertyName = "effective_max_dop", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, ToolTipText = "Effective maximum degree of parallelism for the workload group" },
                        new DataGridViewTextBoxColumn() { HeaderText = "Total CPU Usage Preemptive MS", DataPropertyName = "total_cpu_usage_preemptive_ms", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, ToolTipText = "Total CPU time used while in preemptive mode scheduling in milliseconds for the workload group (e.g. extended stored procedures and distributed queries)" },
                        new DataGridViewTextBoxColumn() { HeaderText = "Request Max Memory Grant Percent Numeric", DataPropertyName = "request_max_memory_grant_percent_numeric", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, Visible = false, ToolTipText = "Current setting for the maximum memory grant as a percentage for a single request (float value). Supports decimal values from 0-100." },
                        new DataGridViewTextBoxColumn() { HeaderText = "Tempdb Data Space KB", DataPropertyName = "tempdb_data_space_kb", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, ToolTipText = "Current data space consumed in the tempdb data files by all sessions in the workload group in kilobytes" },
                        new DataGridViewTextBoxColumn() { HeaderText = "Peak Tempdb Data Space KB", DataPropertyName = "peak_tempdb_data_space_kb", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, ToolTipText = "Peak data space consumed in the tempdb data files by all sessions in the workload group since server startup or since resource governor statistics were reset, in kilobytes" },
                        new DataGridViewTextBoxColumn() { HeaderText = "Total Tempdb Data Limit Violation Count", DataPropertyName = "total_tempdb_data_limit_violation_count", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, ToolTipText = "Number of times a request was aborted because it would exceed the limit on tempdb data space consumption for the workload group" }

                    );

                    dgv.ReplaceSpaceWithNewLineInHeaderTextToImproveColumnAutoSizing();
                    dgv.DataSource = dt;
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
    }
}