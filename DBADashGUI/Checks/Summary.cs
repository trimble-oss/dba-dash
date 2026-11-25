using DBADash.DailyReport;
using DBADashGUI.AgentJobs;
using DBADashGUI.Checks;
using Humanizer;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static DBADashGUI.DBADashStatus;
using static DBADashGUI.Main;

namespace DBADashGUI
{
    public partial class Summary : UserControl, ISetContext, IRefreshData
    {
        private List<int> refreshInstanceIDs;
        private bool refreshIncludeHidden;

        private DateTime? lastRefresh;

        /// <summary>The summary as last loaded, which the Report view is built from</summary>
        private DataTable lastSummaryData;
        /// <summary>The Report view is shown in place of the grids</summary>
        private bool showReport;
        /// <summary>Counts report builds, so a build that finishes after a newer one (or after the view is closed) isn't shown</summary>
        private int reportGeneration;
        private readonly WebView2Wrapper reportView = new() { Dock = DockStyle.Fill, Visible = false };

        public Summary()
        {
            InitializeComponent();
            Controls.Add(reportView);
            reportView.SendToBack(); // Fill docks last, so the toolbar keeps its place at the top
            reportView.AppLinkRequested += ReportView_AppLinkRequested;
            dgvTests.AutoGenerateColumns = false;
            dgvTests.Columns.AddRange(TestCols.ToArray());
            dgvTests.GridFilterChanged += (s, e) => UpdateClearFilter();
            dgvSummary.GridFilterChanged += (s, e) => UpdateClearFilter();
            dgvSummary.ReplaceSpaceWithNewLineInHeaderTextToImproveColumnAutoSizing();
            InitializeTestSummaryMaxPctMenuItems();
        }

        private bool IsDefaultFilter => (string.IsNullOrEmpty(dgvTests.RowFilter) || dgvTests.RowFilter == TestRowFilter) && (string.IsNullOrEmpty(dgvSummary.RowFilter) || dgvSummary.RowFilter == SummaryRowFilter);

        private void UpdateClearFilter()
        {
            if (string.IsNullOrEmpty(dgvSummary.RowFilter) && !string.IsNullOrEmpty(SummaryRowFilter))
            {
                dgvSummary.SetFilter(SummaryRowFilter);
            }

            if (string.IsNullOrEmpty(dgvTests.RowFilter) && !string.IsNullOrEmpty(TestRowFilter))
            {
                dgvTests.SetFilter(TestRowFilter);
            }
            tsClearFilter.Enabled = !IsDefaultFilter;
            tsClearFilter.Font = tsClearFilter.Enabled ? new Font(tsClearFilter.Font, FontStyle.Bold) : new Font(tsClearFilter.Font, FontStyle.Regular);
            tsClearFilter.ToolTipText = tsClearFilter.Enabled ? "Clear Filter" : "No Filter to Clear";
        }

        private DataView dv;

        private Dictionary<string, bool> statusColumns;

        private bool FocusedView => focusedViewToolStripMenuItem.Checked;
        private DBADashContext context;
        private bool ShowHidden => context.InstanceIDs.Count == 1 || Common.ShowHidden;

        private bool WasRefreshed;

        private readonly Dictionary<string, Tabs?> tabMapping = new() { { "FullBackupStatus",Tabs.Backups }, { "LogShippingStatus", Tabs.LogShipping}, { "DiffBackupStatus", Tabs.Backups }, { "LogBackupStatus", Tabs.Backups }, { "DriveStatus", Tabs.Drives },
                                                            { "JobStatus", Tabs.Jobs }, { "CollectionErrorStatus", Tabs.CollectionHealth}, { "AGStatus", Tabs.AG }, {"LastGoodCheckDBStatus", Tabs.LastGood}, {"SnapshotAgeStatus",Tabs.CollectionDates  },
                                                            {"MemoryDumpStatus", null}, {"UptimeStatus",null}, {"CorruptionStatus",null }, {"AlertStatus",Tabs.SQLAgentAlerts }, {"FileFreeSpaceStatus", Tabs.Files },
                                                            {"CustomCheckStatus",Tabs.CustomChecks  }, {"MirroringStatus",Tabs.Mirroring },{"ElasticPoolStorageStatus",Tabs.AzureSummary},{"PctMaxSizeStatus",Tabs.Files}, {"QueryStoreStatus",Tabs.QS },
                                                            {"LogFreeSpaceStatus",Tabs.Files},{"DBMailStatus",null },{"IdentityStatus",Tabs.IdentityColumns }, {"IsAgentRunningStatus",null },{"DatabaseStateStatus",Tabs.DBOptions }, {"ServiceBrokerStatus",Tabs.ServiceBroker }};

        private void ResetStatusCols()
        {
            statusColumns = new Dictionary<string, bool> { { "FullBackupStatus", false }, { "LogShippingStatus", false }, { "DiffBackupStatus", false }, { "LogBackupStatus", false }, { "DriveStatus", false },
                                                            { "JobStatus", false }, { "CollectionErrorStatus", false }, { "AGStatus", false }, {"LastGoodCheckDBStatus",false}, {"SnapshotAgeStatus",false },
                                                            {"MemoryDumpStatus",false }, {"UptimeStatus",false }, {"CorruptionStatus",false }, {"AlertStatus",false }, {"FileFreeSpaceStatus",false },
                                                            {"CustomCheckStatus",false }, {"MirroringStatus",false },{"ElasticPoolStorageStatus",false},{"PctMaxSizeStatus",false}, {"QueryStoreStatus",false },
                                                            {"LogFreeSpaceStatus",false },{"DBMailStatus",false },{"IdentityStatus",false },{"IsAgentRunningStatus",false },{"DatabaseStateStatus",false}, {"ServiceBrokerStatus",false}};
        }

        private Task<DataTable> GetSummaryAsync(bool forceRefresh, DateTime? forceRefreshDate)
        {
            return Task<DataTable>.Factory.StartNew(() =>
            {
                using var cn = new SqlConnection(Common.ConnectionString);
                using var cmd = new SqlCommand("dbo.Summary_Get", cn) { CommandType = CommandType.StoredProcedure };
                using var da = new SqlDataAdapter(cmd);
                cn.Open();
                cmd.Parameters.AddWithValue("ForceRefresh", forceRefresh);
                cmd.Parameters.AddWithNullableValue("ForceRefreshDate", forceRefreshDate);
                cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", context.InstanceIDs));
                cmd.Parameters.AddWithValue("ShowHidden", ShowHidden);
                cmd.CommandTimeout = Config.SummaryCommandTimeout;
                var pWasRefreshed = new SqlParameter("WasRefreshed", SqlDbType.Bit)
                { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(pWasRefreshed);

                DataTable dt = new();
                da.Fill(dt);
                WasRefreshed = (bool)pWasRefreshed.Value;
                return dt;
            });
        }

        public void SetContext(DBADashContext _context)
        {
            this.context = _context;
            RefreshDataIfStale();
        }

        public void RefreshDataIfStale()
        {
            if (lastRefresh == null || DateTime.UtcNow.Subtract(lastRefresh.Value).TotalSeconds > Config.ClientSummaryCacheDuration || ParametersChanged)
            {
                RefreshData();
            }
            else
            {
                UpdateRefreshTime(); // Ensure refresh time is in correct time zone in case of switching time zones.
                dgvSummary.Columns[0].Frozen = Common.FreezeKeyColumn;
            }
        }

        private bool ParametersChanged => !(context.InstanceIDs.Count == refreshInstanceIDs.Count && refreshInstanceIDs.All(context.InstanceIDs.Contains) && refreshIncludeHidden == Common.ShowHidden);

        private CancellationTokenSource cancellationTS = new();
        private bool savedLayoutLoaded;

        public void RefreshData()
        {
            RefreshData(false);
        }

        public void RefreshData(bool forceRefresh, DateTime? forceRefreshDate = null) => RefreshData(forceRefresh, forceRefreshDate, keepFilters: true);

        /// <summary>
        /// The filters to put back once a refresh has rebuilt the grids' views.  Null means the defaults
        /// (<see cref="SummaryRowFilter"/> / <see cref="TestRowFilter"/>).  <paramref name="StatusTest"/> is the test
        /// <see cref="FilterByStatus(List{DBADashStatusEnum}, string)"/> narrowed the status columns to.
        /// </summary>
        private sealed record CarriedFilters(string Summary, string Tests, string StatusTest);

        /// <summary>The test the instance grid was filtered to by clicking the test summary, while that filter is on</summary>
        private string statusFilterTest;

        /// <summary>
        /// The instances (and hidden setting) the data in the grids is for.  Not <see cref="refreshInstanceIDs"/>, which
        /// is set when a load starts: if it fails, the grids still show the previous instances' data.
        /// </summary>
        private sealed record LoadedInstances(List<int> InstanceIDs, bool IncludeHidden)
        {
            public bool Matches(DBADashContext ctx) => ctx.InstanceIDs.Count == InstanceIDs.Count &&
                                                       InstanceIDs.All(ctx.InstanceIDs.Contains) &&
                                                       IncludeHidden == Common.ShowHidden;
        }

        private LoadedInstances boundInstances;

        /// <summary>
        /// The filters the user added, so a refresh keeps them.  Not when the instances shown have changed (another
        /// tree node): a filter belongs to the view it was set on.
        /// </summary>
        private CarriedFilters CaptureFilters()
        {
            if (dgvSummary.DataSource is not DataView || boundInstances?.Matches(context) != true) return null;
            var summary = dgvSummary.RowFilter == SummaryRowFilter ? null : dgvSummary.RowFilter;
            var tests = dgvTests.RowFilter == TestRowFilter ? null : dgvTests.RowFilter;
            return summary == null && tests == null ? null : new CarriedFilters(summary, tests, summary == null ? null : statusFilterTest);
        }

        private void ApplyCarriedFilters(CarriedFilters carried)
        {
            statusFilterTest = null;
            if (carried == null) return;
            // Quietly dropped if one no longer applies
            if (carried.Tests != null) dgvTests.TrySetFilter(carried.Tests);
            if (carried.Summary != null && dgvSummary.TrySetFilter(carried.Summary) && carried.StatusTest != null
                && dgvSummary.Columns.Contains(carried.StatusTest))
            {
                HideStatusColumns();
                dgvSummary.Columns[carried.StatusTest].Visible = true;
                statusFilterTest = carried.StatusTest;
            }
        }

        private void RefreshData(bool forceRefresh, DateTime? forceRefreshDate, bool keepFilters)
        {
            if (context == null) return;
            if (!savedLayoutLoaded)
            {
                LoadSavedLayout();
                savedLayoutLoaded = true;
            }
            var carriedFilters = keepFilters ? CaptureFilters() : null;
            cancellationTS.Cancel(); // Cancel previous execution
            cancellationTS = new();
            dgvSummary.Columns[0].Frozen = Common.FreezeKeyColumn;
            ResetStatusCols();
            refresh1.ShowRefresh();
            splitContainer1.Visible = false;
            refreshInstanceIDs = new List<int>(context.InstanceIDs);
            refreshIncludeHidden = Common.ShowHidden;
            var loading = new LoadedInstances(refreshInstanceIDs, refreshIncludeHidden);
            tsRefresh.Enabled = false;
            _ = GetSummaryAsync(forceRefresh, forceRefreshDate).ContinueWith(task =>
            {
                toolStrip1.Invoke(() => { tsRefresh.Enabled = true; tsClearFilter.Enabled = false; });
                if (task.Exception != null)
                {
                    refresh1.Invoke(() => { refresh1.SetFailed("Error:" + task.Exception); });
                    return Task.CompletedTask;
                }
                var dt = task.Result;
                try
                {
                    GroupSummaryByTest(ref dt);
                    UpdateSummary(ref dt, loading, carriedFilters);
                    AutoSizeSplitter();
                    lastSummaryData = dt;
                    if (showReport)
                    {
                        toolStrip1.Invoke((Action)(() => _ = ShowReportAsync()));
                    }
                }
                catch (Exception ex)
                {
                    refresh1.Invoke(() => { refresh1.SetFailed("Error:" + ex); });
                }

                return Task.CompletedTask;
            }, cancellationTS.Token);
        }

        public void LoadSavedLayout()
        {
            try
            {
                var saved = SummarySavedView.GetDefaultSavedView();

                if (saved == null) return;
                Common.ShowHidden = saved.ShowHidden;
                focusedViewToolStripMenuItem.Checked = saved.FocusedView;
                showTestSummaryToolStripMenuItem.Checked = saved.ShowTestSummary;
                splitContainer1.Panel1Collapsed = !showTestSummaryToolStripMenuItem.Checked;
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error loading saved view");
            }
        }

        private readonly List<DataGridViewColumn> TestCols = new()
        {           new DataGridViewLinkColumn(){ Name="Test", HeaderText="Test", DataPropertyName="DisplayText", SortMode = DataGridViewColumnSortMode.Automatic, Width=200, ToolTipText = "Name of test"},
                    new DataGridViewLinkColumn(){ Name="OK", HeaderText="Instance Count OK", DataPropertyName="OK", SortMode = DataGridViewColumnSortMode.Automatic, ToolTipText="Count of instances where status of check is OK." },
                    new DataGridViewLinkColumn(){ Name = "Warning",  HeaderText="Instance Count Warning", DataPropertyName="Warning", SortMode = DataGridViewColumnSortMode.Automatic, ToolTipText = "Count of instances where status of check is Warning" },
                    new DataGridViewLinkColumn(){ Name = "Critical",  HeaderText="Instance Count Critical", DataPropertyName="Critical", SortMode = DataGridViewColumnSortMode.Automatic,ToolTipText = "Count of instances where status of check is Critical"  },
                    new DataGridViewLinkColumn(){ Name = "NA",  HeaderText="Instance Count N/A", DataPropertyName="NA",SortMode = DataGridViewColumnSortMode.Automatic,ToolTipText = "Count of instances where status of check is Not Applicable.  The check might not apply due to configured thresholds or based on what features are in use."  },
                    new DataGridViewLinkColumn(){ Name = "Acknowledged",  HeaderText="Instance Count Acknowledged", DataPropertyName="Acknowledged", SortMode = DataGridViewColumnSortMode.Automatic, ToolTipText = "Count of instance where the status of check is Acknowledged." }
        };

        private static DataTable GroupedByTestSchema()
        {
            DataTable grouped = new();
            grouped.Columns.Add("Test", typeof(string));
            grouped.Columns.Add("DisplayText", typeof(string));
            grouped.Columns.Add("OK", typeof(int));
            grouped.Columns.Add("Warning", typeof(int));
            grouped.Columns.Add("Critical", typeof(int));
            grouped.Columns.Add("NA", typeof(int));
            grouped.Columns.Add("Acknowledged", typeof(int));
            grouped.Columns.Add("Total", typeof(int));
            grouped.Columns.Add("Status", typeof(DBADashStatusEnum));
            grouped.Columns.Add("IsFocusedRow", typeof(bool));
            return grouped;
        }

        private void GroupSummaryByTest(ref DataTable dt)
        {
            var grouped = GroupedByTestSchema();
            Dictionary<string, DataRow> tests = new();

            // Add a row for each test with zeros/defaults
            foreach (var statusCol in statusColumns.Keys)
            {
                var row = grouped.NewRow();
                row["Test"] = statusCol;
                row["DisplayText"] = dgvSummary.Columns[statusCol].HeaderText.Replace("\n", " ");
                row["OK"] = 0;
                row["Warning"] = 0;
                row["Critical"] = 0;
                row["NA"] = 0;
                row["Acknowledged"] = 0;
                row["Total"] = 0;
                row["Status"] = DBADashStatusEnum.NA;
                row["IsFocusedRow"] = false;
                tests.Add(statusCol, row);
            }
            // Add count of instances by status for each test
            foreach (DataRow row in dt.Rows)
            {
                foreach (var statusCol in statusColumns.Keys)
                {
                    var groupedRow = tests[statusCol];
                    var status = (DBADashStatusEnum)Convert.ToInt32(row[statusCol] == DBNull.Value ? 3 : row[statusCol]);
                    groupedRow[status.ToString()] = (int)groupedRow[status.ToString()] + 1;
                    groupedRow["Total"] = (int)groupedRow["Total"] + 1;
                    if (status == DBADashStatusEnum.Warning || status == DBADashStatusEnum.Critical)
                    {
                        groupedRow["IsFocusedRow"] = true;
                    }
                }
            }
            tests.Values.CopyToDataTable(grouped, LoadOption.OverwriteChanges);

            var dvTestSummary = new DataView(grouped, TestRowFilter, "DisplayText", DataViewRowState.CurrentRows);
            dgvTests.Invoke((Action)(() =>
            {
                dgvTests.DataSource = dvTestSummary;
                dgvTests.AutoResizeColumnsWithMaxColumnWidth(DataGridViewAutoSizeColumnsMode.AllCells);
            }));
        }

        private void AutoSizeSplitter()
        {
            var maxHeightPct = Math.Max(Math.Min(Properties.Settings.Default.TestSummaryMaxHeightPct, 0.99M), 0.01M);
            // Auto size split container
            if (dgvTests.Rows.Count > 0)
            {
                splitContainer1.Invoke(() =>
                {
                    var distance = ((dgvTests.Rows[0].Height + 1) * dgvTests.Rows.Count) + dgvTests.ColumnHeadersHeight;
                    distance = Math.Min(distance, Convert.ToInt32(splitContainer1.Height * maxHeightPct));
                    splitContainer1.SplitterDistance = distance;
                });
            }
        }

        private void SetStatusColumnVisibility()
        {
            // hide columns that all have status N/A
            foreach (var col in statusColumns)
            {
                dgvSummary.Columns[col.Key].Visible = col.Value;
            }
        }

        private void HideStatusColumns()
        {
            // hide all status columns
            foreach (var col in statusColumns)
            {
                dgvSummary.Columns[col.Key].Visible = false;
            }
        }

        private void UpdateSummary(ref DataTable dt, LoadedInstances loading, CarriedFilters carriedFilters)
        {
            GroupSummaryByTest(ref dt);
            dgvSummary.AutoGenerateColumns = false;
            var cols = (statusColumns.Keys).ToList();
            dt.Columns.Add("IsFocusedRow", typeof(bool));
            foreach (DataRow row in dt.Rows)
            {
                var isFocusedRow = false;
                foreach (var col in cols)
                {
                    var status = (DBADashStatusEnum)Convert.ToInt32(row[col] == DBNull.Value ? 3 : row[col]);
                    if (!(status == DBADashStatusEnum.NA || (status == DBADashStatusEnum.OK && FocusedView)))
                    {
                        statusColumns[col] = true;
                        isFocusedRow = true;
                    }
                }
                var agentRunningStatus = (DBADashStatusEnum)row["IsAgentRunningStatus"];
                if (!(agentRunningStatus == DBADashStatusEnum.NA || (agentRunningStatus == DBADashStatusEnum.OK && FocusedView)))
                {
                    isFocusedRow = true;
                    statusColumns["JobStatus"] = true;
                }
                // A paused instance's collections report N/A, which would hide the column showing the pause (and the row in
                // the focused view) - but a pause is something to notice, so always show it
                if (row.Table.Columns.Contains("CollectionsPausedUntil") && row["CollectionsPausedUntil"] != DBNull.Value)
                {
                    isFocusedRow = true;
                    statusColumns["SnapshotAgeStatus"] = true;
                }
                row["IsFocusedRow"] = isFocusedRow;
                lastRefresh = (DateTime)row["RefreshDate"];
            }
            dgvSummary.Invoke(SetStatusColumnVisibility);
            dgvSummary.Invoke((Action)(() => colHidden.Visible = ShowHidden));

            dv = new DataView(dt, SummaryRowFilter, "Instance", DataViewRowState.CurrentRows);
            dgvSummary.Invoke((Action)(() =>
            {
                dgvSummary.DataSource = dv;
                boundInstances = loading;
                // Both views are new (the test summary's was set in GroupSummaryByTest), so put back the user's filters
                ApplyCarriedFilters(carriedFilters);
                dgvSummary.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            }
            ));

            toolStrip1.Invoke(() =>
            {
                UpdateRefreshTime();
                SetRefreshLabelStatus(DBADashStatusEnum.OK);
            }
            );
            refresh1.Invoke((Action)(() => refresh1.Visible = false));
            splitContainer1.Invoke(() => splitContainer1.Visible = !showReport);
            timer1.Enabled = true;
        }

        private string SummaryRowFilter => FocusedView ? "IsFocusedRow=1" : "";
        private string TestRowFilter => FocusedView ? "IsFocusedRow=1" : "(OK>0 OR Warning>0 OR Critical>0)";

        private void DgvSummary_RowAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (var idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)dgvSummary.Rows[idx].DataBoundItem;
                var isAzure = row["InstanceID"] == DBNull.Value;
                var cols = (statusColumns.Keys).ToList();
                foreach (var col in cols)
                {
                    var status = (DBADashStatusEnum)Convert.ToInt32(row[col] == DBNull.Value ? 3 : row[col]);
                    dgvSummary.Rows[idx].Cells[col].SetStatusColor(status);
                }
                var DBMailStatus = Convert.ToString(row["DBMailStatusDescription"]);
                dgvSummary.Rows[idx].Cells["DBMailStatus"].ToolTipText = DBMailStatus;
                dgvSummary.Rows[idx].Cells["ServiceBrokerStatus"].ToolTipText = Convert.ToString(row["ServiceBrokerStatusDescription"]);

                dgvSummary.Rows[idx].Cells["FullBackupStatus"].Value = isAzure ? "" : "View";
                dgvSummary.Rows[idx].Cells["DiffBackupStatus"].Value = isAzure ? "" : "View";
                dgvSummary.Rows[idx].Cells["LogBackupStatus"].Value = isAzure ? "" : "View";
                dgvSummary.Rows[idx].Cells["DriveStatus"].Value = isAzure ? "" : "View";
                // Azure: the status of elastic jobs, where a database on the server is the job database of an elastic job agent
                var hasElasticJobStatus = isAzure && Convert.ToInt32(row["JobStatus"]) != 3;
                dgvSummary.Rows[idx].Cells["JobStatus"].Value = !isAzure || hasElasticJobStatus ? "View" : "";
                if (hasElasticJobStatus)
                {
                    dgvSummary.Rows[idx].Cells["JobStatus"].ToolTipText = "Elastic jobs of the server's elastic job agents";
                }
                SetLogShippingCell(dgvSummary.Rows[idx].Cells["LogShippingStatus"], row, isAzure);
                dgvSummary.Rows[idx].Cells["AGStatus"].Value = (int)row["AGStatus"] == 3 ? "" : "View";
                dgvSummary.Rows[idx].Cells["QueryStoreStatus"].Value = (int)row["QueryStoreStatus"] == 3 ? "" : "View";
                var identPct = (row["MaxIdentityPctUsed"] == DBNull.Value
                    ? string.Empty : ((decimal)row["MaxIdentityPctUsed"]).ToString("P1"));
                var identDays = (long?)row["MinIdentityEstimatedDays"].DBNullToNull();
                var identDaysString = string.Empty;
                if (identDays.HasValue)
                {
                    identDaysString = identDays.Value < TimeSpan.MaxValue.Days
                        ? TimeSpan.FromDays(identDays.Value).Humanize(1, minUnit: TimeUnit.Day, maxUnit: TimeUnit.Year)
                        : "∞";
                }
                dgvSummary.Rows[idx].Cells["IdentityStatus"].Value = string.Join(" / ", new[] { identPct, identDaysString }.Where(s => !string.IsNullOrEmpty(s)));

                dgvSummary.Rows[idx].Cells["CorruptionStatus"].Value = row["DetectedCorruptionDateUtc"] == DBNull.Value
                    ? ""
                    : DateTime.UtcNow.Subtract((DateTime)row["DetectedCorruptionDateUtc"]).Humanize();
                if (row["IsAgentRunning"] != DBNull.Value && (bool)row["IsAgentRunning"] == false)
                {
                    var status = (DBADashStatusEnum)row["IsAgentRunningStatus"];
                    dgvSummary.Rows[idx].Cells["JobStatus"].SetStatusColor(status);
                    dgvSummary.Rows[idx].Cells["JobStatus"].Value = "Not Running";
                }

                if (row["sqlserver_uptime"] != DBNull.Value)
                {
                    try
                    {
                        var offlineSince = (DateTime?)row["OfflineSince"].DBNullToNull();
                        var startTime = (DateTime?)row["sqlserver_start_time_utc"].DBNullToNull();
                        var lastFail = (DateTime?)row["LastFail"].DBNullToNull();
                        var hostStart = (DateTime?)row["host_start_time_utc"].DBNullToNull();
                        var osInfoSnapshot = (DateTime?)row["OSInfoSnapshotDate"].DBNullToNull();
                        var uptimeString = string.Empty;
                        if (offlineSince.HasValue)
                        {
                            uptimeString = $"Offline {DateTime.UtcNow.Subtract(offlineSince.Value).Humanize(precision: 2, minUnit: TimeUnit.Second)}";
                        }
                        else if (startTime.HasValue)
                        {
                            uptimeString = DateTime.UtcNow.Subtract(startTime.Value).Humanize(precision: 2, minUnit: TimeUnit.Second);

                            if (lastFail > startTime.Value.AddMinutes(2))
                            {
                                uptimeString += " / " + DateTime.UtcNow.Subtract(lastFail.Value).Humanize(precision: 2, minUnit: TimeUnit.Second);
                            }
                        }

                        var toolTip = string.Empty;
                        if (startTime.HasValue)
                        {
                            toolTip = $"SQL Server start time {startTime.Value.ToAppTimeZone()}\nTime since SQL Server start: {DateTime.UtcNow.Subtract(startTime.Value).Humanize(precision: 2, minUnit: TimeUnit.Second)}\n";
                        }

                        if (hostStart.HasValue)
                        {
                            toolTip += $"OS boot Time: {hostStart.Value.ToAppTimeZone()}\nTime since OS boot: {DateTime.UtcNow.Subtract(hostStart.Value).Humanize(precision: 2, minUnit: TimeUnit.Second)}\n";
                        }
                        if (osInfoSnapshot.HasValue)
                        {
                            toolTip +=
                                $"Uptime info checked: {osInfoSnapshot.Value.ToAppTimeZone()} ({DateTime.UtcNow.Subtract(osInfoSnapshot.Value).Humanize(precision: 2, minUnit: TimeUnit.Second)})\n";
                        }

                        if (startTime.HasValue && lastFail > startTime.Value.AddMinutes(2))
                        {
                            toolTip +=
                                $"Last Connectivity Failure: {lastFail.Value.ToAppTimeZone()}\nTime since failure: {DateTime.UtcNow.Subtract(lastFail.Value).Humanize(precision: 2, minUnit: TimeUnit.Second)}\n";
                        }

                        if (offlineSince.HasValue)
                        {
                            toolTip = $"Instance appears to be offline since {offlineSince.Value.ToAppTimeZone()} ({DateTime.UtcNow.Subtract(offlineSince.Value).Humanize(precision: 2, minUnit: TimeUnit.Second)})\n" + toolTip;
                        }

                        dgvSummary.Rows[idx].Cells["UptimeStatus"].Value = uptimeString;
                        dgvSummary.Rows[idx].Cells["UptimeStatus"].ToolTipText = toolTip;
                    }
                    catch (Exception ex)
                    {
                        dgvSummary.Rows[idx].Cells["UptimeStatus"].Value = "Error";
                        dgvSummary.Rows[idx].Cells["UptimeStatus"].ToolTipText = ex.Message;
                    }
                }

                if (row["SnapshotAgeMin"] == DBNull.Value || row["SnapshotAgeMax"] == DBNull.Value)
                {
                    dgvSummary.Rows[idx].Cells["SnapshotAgeStatus"].Value = "N/A";
                }
                else
                {
                    var snapshotAgeMin = (int)row["SnapshotAgeMin"];
                    var snapshotAgeMax = (int)row["SnapshotAgeMax"];
                    if (TimeSpan.FromMinutes(snapshotAgeMax).Humanize(minUnit: TimeUnit.Minute, maxUnit: TimeUnit.Year) == TimeSpan.FromMinutes(snapshotAgeMin).Humanize(minUnit: TimeUnit.Minute, maxUnit: TimeUnit.Year))
                    {
                        dgvSummary.Rows[idx].Cells["SnapshotAgeStatus"].Value = TimeSpan.FromMinutes(snapshotAgeMax).Humanize(minUnit: TimeUnit.Minute, maxUnit: TimeUnit.Year);
                    }
                    else
                    {
                        dgvSummary.Rows[idx].Cells["SnapshotAgeStatus"].Value = TimeSpan.FromMinutes(snapshotAgeMin).Humanize(minUnit: TimeUnit.Minute, maxUnit: TimeUnit.Year) + " to " + TimeSpan.FromMinutes(snapshotAgeMax).Humanize(minUnit: TimeUnit.Minute, maxUnit: TimeUnit.Year);
                    }
                }
                // A pause takes over the collection dates cell (and its status colour) so it can't be missed - unless
                // collections that aren't paused (other databases on an Azure server) have a problem, which still shows
                if (row.Row.Table.Columns.Contains("CollectionsPausedUntil") && row["CollectionsPausedUntil"] is DateTime pausedUntil)
                {
                    var pausedCount = Convert.ToInt32(row["PausedCount"]);
                    var pausedCell = dgvSummary.Rows[idx].Cells["SnapshotAgeStatus"];
                    var pausedText = isAzure
                        ? $"{pausedCount} DB(s) paused"
                        : $"Paused until {CollectionDates.PauseCollections.FormatPausedUntil(pausedUntil)}";
                    var snapshotStatus = (DBADashStatusEnum)Convert.ToInt32(row["SnapshotAgeStatus"] == DBNull.Value ? 3 : row["SnapshotAgeStatus"]);
                    if (snapshotStatus is DBADashStatusEnum.Warning or DBADashStatusEnum.Critical)
                    {
                        pausedCell.Value = $"{pausedCell.Value} ({pausedText})";
                    }
                    else
                    {
                        pausedCell.Value = pausedText;
                        pausedCell.SetColor(DashColors.BluePale, DashColors.BlueDark, DashColors.BlueLight, Color.White);
                    }
                    pausedCell.ToolTipText = CollectionDates.PauseCollections.PausedToolTip(pausedUntil);
                    dgvSummary.Rows[idx].Cells["Instance"].ToolTipText = pausedCell.ToolTipText;
                }
                if (row["DaysSinceLastGoodCheckDB"] != DBNull.Value)
                {
                    dgvSummary.Rows[idx].Cells["LastGoodCheckDBStatus"].Value = ((int)row["DaysSinceLastGoodCheckDB"]) + " days";
                }
                var oldestLastGoodCheckDB = "Unknown";
                if (row["OldestLastGoodCheckDBTime"] != DBNull.Value)
                {
                    if ((DateTime)row["OldestLastGoodCheckDBTime"] == DateTime.Parse("1900-01-01"))
                    {
                        oldestLastGoodCheckDB = "Never";
                        dgvSummary.Rows[idx].Cells["LastGoodCheckDBStatus"].Value = "Never";
                    }
                    else
                    {
                        oldestLastGoodCheckDB = ((DateTime)row["OldestLastGoodCheckDBTime"]).ToAppTimeZone().ToString("yyyy-MM-dd HH:mm");
                    }
                }
                if (row["LastGoodCheckDBCriticalCount"] != DBNull.Value)
                {
                    dgvSummary.Rows[idx].Cells["LastGoodCheckDBStatus"].ToolTipText = "Last Good CheckDB Critical:" + (int)row["LastGoodCheckDBCriticalCount"] + Environment.NewLine +
                                                                               "Last Good CheckDB Warning:" + (int)row["LastGoodCheckDBWarningCount"] + Environment.NewLine +
                                                                               "Last Good CheckDB Good:" + (int)row["LastGoodCheckDBHealthyCount"] + Environment.NewLine +
                                                                               "Last Good CheckDB NA:" + (int)row["LastGoodCheckDBNACount"] + Environment.NewLine +
                                                                               "Oldest Last Good CheckDB:" + oldestLastGoodCheckDB;
                }
                if (row["LastMemoryDump"] != DBNull.Value)
                {
                    var lastMemoryDump = (DateTime)row["LastMemoryDump"];
                    var lastMemoryDumpUTC = (DateTime)row["LastMemoryDumpUTC"];
                    var memoryDumpCount = (int)row["MemoryDumpCount"];
                    string lastMemoryDumpStr;

                    if (Math.Abs(lastMemoryDumpUTC.ToAppTimeZone().Subtract(lastMemoryDump).TotalMinutes) > 10)
                    {
                        lastMemoryDumpStr = "Last Memory Dump (local time): " + lastMemoryDumpUTC.ToAppTimeZone().ToString(CultureInfo.CurrentCulture) + Environment.NewLine +
                           "Last Memory Dump (server time): " + lastMemoryDump.ToString(CultureInfo.CurrentCulture) + Environment.NewLine +
                           "Total Memory Dumps: " + memoryDumpCount;
                    }
                    else
                    {
                        lastMemoryDumpStr = "Last Memory Dump: " + lastMemoryDumpUTC.ToAppTimeZone().ToString(CultureInfo.CurrentCulture) + Environment.NewLine +
                           "Total Memory Dumps: " + memoryDumpCount;
                    }

                    dgvSummary.Rows[idx].Cells["MemoryDumpStatus"].Value = DateTime.UtcNow.Subtract(lastMemoryDumpUTC).Humanize();

                    dgvSummary.Rows[idx].Cells["MemoryDumpStatus"].ToolTipText = lastMemoryDumpStr;
                }
                var lastAlert = "Never";
                var lastAlertDays = "Never";
                var lastCriticalAlert = "Never";
                if (row["LastAlert"] != DBNull.Value)
                {
                    var lastAlertD = (DateTime)row["LastAlert"];
                    lastAlert = lastAlertD.ToAppTimeZone().ToString("yyyy-MM-dd HH:mm");
                    if (DateTime.UtcNow.Subtract(lastAlertD).TotalHours < 24)
                    {
                        lastAlertDays = DateTime.UtcNow.Subtract(lastAlertD).TotalHours.ToString("0") + "hrs";
                    }
                    else
                    {
                        lastAlertDays = DateTime.UtcNow.Subtract(lastAlertD).TotalDays.ToString("0") + " days";
                    }
                }
                if (row["LastCritical"] != DBNull.Value)
                {
                    lastCriticalAlert = (((DateTime)row["LastCritical"]).ToAppTimeZone()).ToString("yyyy-MM-dd HH:mm");
                }
                var totalAlerts = row["TotalAlerts"] == DBNull.Value ? 0 : (int)row["TotalAlerts"];

                dgvSummary.Rows[idx].Cells["AlertStatus"].Value = lastAlertDays;
                dgvSummary.Rows[idx].Cells["AlertStatus"].ToolTipText = "Last Alert:" + lastAlert + Environment.NewLine +
                                                                        "Last Critical Alert:" + lastCriticalAlert + Environment.NewLine +
                                                                        "Total Alerts:" + totalAlerts;
                dgvSummary.Rows[idx].Cells["ElasticPoolStorageStatus"].Value = isAzure ? "View" : "";
            }
        }

        /// <summary>
        /// The log shipping cell: the databases needing attention (critical + warning) of those checked, or how many are OK, with the
        /// breakdown by status in the tooltip.  "View" where there are no counts (no log shipped databases, or only N/A ones).
        /// </summary>
        private static void SetLogShippingCell(DataGridViewCell cell, DataRowView row, bool isAzure)
        {
            if (isAzure)
            {
                cell.Value = "";
                return;
            }
            var (text, toolTip) = LogShippingCellText(
                Convert.ToInt32(row["LogShippingCriticalCount"].DBNullToNull() ?? 0),
                Convert.ToInt32(row["LogShippingWarningCount"].DBNullToNull() ?? 0),
                Convert.ToInt32(row["LogShippingOKCount"].DBNullToNull() ?? 0),
                Convert.ToInt32(row["LogShippingNACount"].DBNullToNull() ?? 0),
                (decimal?)row["LogShippingMaxTotalTimeBehind"].DBNullToNull(),
                Convert.ToInt32(row["LogShippingNoRestoreCount"].DBNullToNull() ?? 0));
            cell.Value = text;
            cell.ToolTipText = toolTip;
        }

        internal static (string Text, string ToolTip) LogShippingCellText(int critical, int warning, int ok, int na, decimal? maxTotalTimeBehindMins, int noRestore)
        {
            var problems = critical + warning;
            var checkedCount = problems + ok;
            if (checkedCount == 0 && na == 0) return ("View", null);

            var text = problems > 0 ? $"{problems} of {checkedCount}"
                : checkedCount > 0 ? $"{checkedCount} OK"
                : "View";

            var sb = new StringBuilder();
            sb.AppendLine($"Critical: {critical}");
            sb.AppendLine($"Warning: {warning}");
            sb.AppendLine($"OK: {ok}");
            sb.AppendLine($"N/A: {na} (excluded, new or no thresholds)");
            if (checkedCount > 0)
            {
                sb.AppendLine($"Needing attention: {problems} of {checkedCount} ({(decimal)problems / checkedCount:P1})");
            }
            if (maxTotalTimeBehindMins.HasValue)
            {
                sb.AppendLine("Furthest behind: " + TimeSpan.FromMinutes((double)maxTotalTimeBehindMins.Value).Humanize(2, minUnit: TimeUnit.Minute, maxUnit: TimeUnit.Day));
            }
            if (noRestore > 0)
            {
                // These have no time behind, so aren't included in furthest behind
                sb.AppendLine($"No log restore recorded: {noRestore}" + (maxTotalTimeBehindMins.HasValue ? " (not included in furthest behind)" : ""));
            }
            sb.Append("Click to view the Log Shipping tab");
            return (text, sb.ToString());
        }

        private void DgvSummary_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            var sortCol = dgvSummary.Columns[e.ColumnIndex] == UptimeStatus ? "sqlserver_uptime" :
                dgvSummary.Columns[e.ColumnIndex] == MemoryDumpStatus ? "LastMemoryDump" :
                dgvSummary.Columns[e.ColumnIndex] == Instance ? "InstanceGroupName" :
                dgvSummary.Columns[e.ColumnIndex] == colHidden ? "IsHidden" :
                dgvSummary.Columns[e.ColumnIndex].Name;
            if (dgvSummary.Columns[e.ColumnIndex] == SnapshotAgeStatus)
            {
                dv.Sort = (dv.Sort == "SnapshotAgeStatus ASC, SnapshotAgeMax DESC" ? "SnapshotAgeStatus DESC, SnapshotAgeMax ASC" : "SnapshotAgeStatus ASC, SnapshotAgeMax DESC");
            }
            else if (dgvSummary.Columns[e.ColumnIndex] == IdentityStatus)
            {
                dv.Sort = (dv.Sort == "IdentityStatus ASC, MaxIdentityPctUsed DESC" ? "IdentityStatus DESC, MaxIdentityPctUsed ASC" : "IdentityStatus ASC, MaxIdentityPctUsed DESC");
            }
            else
            {
                dv.Sort = sortCol + (dv.Sort == sortCol ? " DESC" : "");
            }
        }

        /// <summary>Tab for the report's object sections (check groups).  Other sections use the summary's tab for their column</summary>
        private static readonly Dictionary<string, Tabs?> ReportSectionTabs = new()
        {
            { "Backups - Full", Tabs.Backups }, { "Backups - Diff", Tabs.Backups }, { "Backups - Log", Tabs.Backups }, { "Drive space", Tabs.Drives }, { "Agent jobs", Tabs.Jobs },
            { DailyReportRenderer.ElasticJobsGroup, Tabs.ElasticJobs }
        };

        /// <summary>
        /// A link into the app for a report section: the tab for the section, on the instance (or the whole fleet when null), and the database
        /// when the item is one.  Null when the section has no tab, so it isn't a link.
        /// </summary>
        private string ReportSectionLink(string section, string instance, string database)
        {
            Tabs? tab = ReportSectionTabs.TryGetValue(section, out var groupTab) ? groupTab : tabMapping.TryGetValue(section, out var columnTab) ? columnTab : null;
            if (tab == null) return null;
            var link = $"{WebView2Wrapper.AppLinkScheme}://open?tab={Uri.EscapeDataString(tab.Value.ToString())}";
            if (!string.IsNullOrEmpty(instance)) link += "&instance=" + Uri.EscapeDataString(instance);
            if (!string.IsNullOrEmpty(database)) link += "&database=" + Uri.EscapeDataString(database);
            return link;
        }

        /// <summary>Opens the tab a report link names, on its instance, the same as clicking the summary</summary>
        private void ReportView_AppLinkRequested(string link)
        {
            var query = System.Web.HttpUtility.ParseQueryString(new Uri(link).Query);
            if (!Enum.TryParse(query["tab"], out Tabs tab)) return;
            // Links without an instance are for the group this page shows (the root, or a tag group): the tab's node under it, such as its HA/DR
            var instance = query["instance"];
            Instance_Selected?.Invoke(this, new InstanceSelectedEventArgs() { Instance = instance, Database = query["database"], Tab = tab, GroupLevel = string.IsNullOrEmpty(instance) });
        }

        private void TsReport_Click(object sender, EventArgs e)
        {
            showReport = tsReport.Checked;
            _ = ShowReportAsync();
        }

        /// <summary>
        /// Shows the Report view of the loaded summary, or the grids again.  The report reads object problems from the database, so it's
        /// built off the UI thread.  The summary as loaded is used, so the report matches what the grid shows.
        /// </summary>
        private async Task ShowReportAsync()
        {
            splitContainer1.Visible = !showReport;
            reportView.Visible = showReport;
            if (!showReport || lastSummaryData == null) return;

            var generation = ++reportGeneration;
            var summary = lastSummaryData;
            var scope = $"Summary page ({summary.Rows.Count} instances)";
            string html;
            try
            {
                // Azure rows are a server each, with no InstanceID - their databases are in scope for their object problems (elastic jobs)
                var azureInstanceIDs = context.AzureInstanceIDs.ToList();
                var content = await Task.Run(() => DailyReportRunner.BuildSummaryPage(summary, Common.ConnectionString, scope, TimeZoneInfo.Local.Id, DateTime.UtcNow, ReportSectionLink, azureInstanceIDs));
                html = content.PreviewHtml();
            }
            catch (Exception ex)
            {
                html = "<html><body style=\"font-family:Segoe UI,Arial,sans-serif;margin:16px\"><p>The report could not be built:</p><pre>" +
                       System.Net.WebUtility.HtmlEncode(ex.Message) + "</pre></body></html>";
            }
            if (IsDisposed || generation != reportGeneration || !showReport) return;
            await reportView.NavigateToLargeString(html);
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData(false, lastRefresh);
        }

        public event EventHandler<InstanceSelectedEventArgs> Instance_Selected;

        private void DgvSummary_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var col = dgvSummary.Columns[e.ColumnIndex].Name;
            var tab = tabMapping.ContainsKey(col) ? tabMapping[dgvSummary.Columns[e.ColumnIndex].Name] : null;
            var row = (DataRowView)dgvSummary.Rows[e.RowIndex].DataBoundItem;
            if (col == "JobStatus" && row["InstanceID"] == DBNull.Value)
            {
                // An Azure server's job status is its elastic jobs.  Nothing to show where there are none (no link).
                if (Convert.ToInt32(row["JobStatus"]) == 3) return;
                tab = Tabs.ElasticJobs;
            }
            if (e.ColumnIndex == UptimeStatus.Index)
            {
                ShowUptimeThresholdConfig((int)row["InstanceID"]);
            }
            else if (e.ColumnIndex == CorruptionStatus.Index)
            {
                ShowCorruptionViewer((string)row["InstanceGroupName"], (int)row["InstanceID"]);
            }
            else if (tab != null)
            {
                Instance_Selected?.Invoke(this,
                    row["InstanceID"] != DBNull.Value
                        ? new InstanceSelectedEventArgs() { InstanceID = (int)row["InstanceID"], Tab = tab }
                        : new InstanceSelectedEventArgs() { Instance = (string)row["InstanceGroupName"], Tab = tab });
            }
        }

        private void ShowUptimeThresholdConfig(int instanceId)
        {
            using var frm = new UptimeThresholdConfig() { InstanceID = instanceId };
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                RefreshData(true);
            }
        }

        private void ShowCorruptionViewer(string instance, int instanceID)
        {
            DBADashContext ctx = new() { RegularInstanceIDsWithHidden = new HashSet<int>() { instanceID }, InstanceID = instanceID, InstanceName = instance };
            ShowCorruptionViewer(ctx);
        }

        private void ShowCorruptionViewer(DBADashContext ctx)
        {
            CorruptionViewer corruptionFrm = new();
            corruptionFrm.SetContext(ctx);
            corruptionFrm.ShowSingleInstance();
        }

        private void FocusedViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // The default filters change with the focused view, so start again from them
            RefreshData(false, null, keepFilters: false);
        }

        private void UpdateRefreshTime()
        {
            lblRefreshTime.Font = WasRefreshed
                ? new Font(lblRefreshTime.Font, FontStyle.Regular)
                : new Font(lblRefreshTime.Font, FontStyle.Italic);
            lblRefreshTime.Text = "Refresh Time: " + (lastRefresh == null ? string.Empty : lastRefresh.Value.ToAppTimeZone().ToString(CultureInfo.CurrentCulture));
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            if (lastRefresh == null || DateTime.UtcNow.Subtract(lastRefresh.Value).TotalMinutes > 60)
            {
                SetRefreshLabelStatus(DBADashStatusEnum.Critical);
                timer1.Enabled = false;
            }
            else if (DateTime.UtcNow.Subtract(lastRefresh.Value).TotalMinutes > 10)
            {
                SetRefreshLabelStatus(DBADashStatusEnum.Warning);
            }
        }

        private void SetRefreshLabelStatus(DBADashStatusEnum status)
        {
            lblRefreshTime.Image = status switch
            {
                DBADashStatusEnum.Critical => Properties.Resources.StatusCriticalError_16x,
                DBADashStatusEnum.Warning => Properties.Resources.StatusAnnotations_Warning_16xLG_color,
                DBADashStatusEnum.OK => Properties.Resources.StatusOK_16x,
                _ => null
            };
        }

        private void ConfigureThresholdsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var frm = new MemoryDumpThresholdsConfig();
            frm.ShowDialog(this);
            if (frm.DialogResult == DialogResult.OK)
            {
                RefreshData(true);
            }
        }

        private void AcknowledgeDumpsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MemoryDumpThresholds.Acknowledge();
            MessageBox.Show("Memory dump acknowledge date updated", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            RefreshData(true);
        }

        private void DgvTests_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (var idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var gridRow = dgvTests.Rows[idx];
                var row = (DataRowView)gridRow.DataBoundItem;
                var rowStatus = DBADashStatusEnum.NA;
                foreach (var status in (DBADashStatusEnum[])Enum.GetValues(typeof(DBADashStatusEnum)))
                {
                    if (!row.Row.Table.Columns.Contains(status.ToString())) break;
                    if ((int)row[status.ToString()] > 0)
                    {
                        gridRow.Cells[status.ToString()].SetStatusColor(status);
                        rowStatus = status < rowStatus || rowStatus == DBADashStatusEnum.NA ? status : rowStatus;
                    }
                    else
                    {
                        gridRow.Cells[status.ToString()].SetStatusColor(DBADashStatusEnum.OK);
                    }
                }
                gridRow.Cells["Test"].SetStatusColor(rowStatus);
            }
        }

        private void DgvTests_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = (DataRowView)dgvTests.Rows[e.RowIndex].DataBoundItem;
            var test = (string)row["Test"];
            var tab = tabMapping[test];
            if (test == "JobStatus" && context.RegularInstanceIDs.Count == 0 && context.AzureInstanceIDs.Count > 0)
            {
                tab = Tabs.ElasticJobs; // Azure only - the job status is of elastic jobs
            }
            switch (e.ColumnIndex)
            {
                case 0 when test == CorruptionStatus.Name:
                    ShowCorruptionViewer(context);
                    break;

                case 0 when !tab.HasValue:
                    FilterByStatus(new List<DBADashStatusEnum>() { DBADashStatusEnum.Warning, DBADashStatusEnum.Critical }, test);
                    break;

                case 0:
                    Instance_Selected?.Invoke(this, new InstanceSelectedEventArgs() { InstanceID = context.InstanceID, Instance = context.InstanceName, Tab = tab });
                    break;

                case >= 1 and <= 5:
                    {
                        var status = e.ColumnIndex switch
                        {
                            1 => DBADashStatusEnum.OK,
                            2 => DBADashStatusEnum.Warning,
                            3 => DBADashStatusEnum.Critical,
                            4 => DBADashStatusEnum.NA,
                            5 => DBADashStatusEnum.Acknowledged,
                            _ => throw new Exception("Invalid ColumnIndex"),
                        };
                        FilterByStatus(status, test);
                        break;
                    }
            }
        }

        private void FilterByStatus(DBADashStatusEnum status, string test)
        {
            FilterByStatus(new List<DBADashStatusEnum>() { status }, test);
        }

        private void FilterByStatus(List<DBADashStatusEnum> statuses, string test)
        {
            var dv = (DataView)dgvSummary.DataSource;
            StringBuilder sbFilter = new();
            foreach (var status in statuses)
            {
                if (sbFilter.Length > 0)
                {
                    sbFilter.Append(" OR ");
                }
                sbFilter.Append(test + " = " + Convert.ToInt32(status));
            }
            dv.RowFilter = sbFilter.ToString();
            HideStatusColumns();
            dgvSummary.Columns[test].Visible = true;
            statusFilterTest = test;
            tsClearFilter.Enabled = true;
        }

        private void TsClearFilter_Click(object sender, EventArgs e)
        {
            statusFilterTest = null;
            dgvSummary.SetFilter(SummaryRowFilter);
            dgvTests.SetFilter(TestRowFilter);
            UpdateClearFilter();
        }

        private void ShowTestSummaryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            splitContainer1.Panel1Collapsed = !showTestSummaryToolStripMenuItem.Checked;
        }

        private void ExportSummaryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dgvSummary.ExportToExcel();
        }

        private void ExportTestSummaryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dgvTests.ExportToExcel();
        }

        private void CopySummaryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dgvSummary.CopyGrid();
        }

        private void CopyTestSummaryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dgvTests.CopyGrid();
        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SummarySavedView saved = new()
            {
                ShowHidden = Common.ShowHidden,
                ShowTestSummary = showTestSummaryToolStripMenuItem.Checked,
                FocusedView = focusedViewToolStripMenuItem.Checked,
                Name = "Default"
            };
            try
            {
                saved.Save();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error saving view options");
            }
        }

        private void ConfigureUptimeThresholdsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowUptimeThresholdConfig(-1);
        }

        private void AcknowledgeUptimeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CommonData.AcknowledgeInstanceUptime(-1);
            RefreshData(true);
        }

        private void SetTestSummaryMaxPct(object sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem { Tag: decimal pct })
                return;
            Properties.Settings.Default.TestSummaryMaxHeightPct = pct;
            Properties.Settings.Default.Save();
            UpdateTestSummaryMaxPctMenuItems();
            AutoSizeSplitter();
        }

        private void InitializeTestSummaryMaxPctMenuItems()
        {
            if (tsSummaryMaxHeight.DropDownItems.Count != 0) return;
            for (var i = 0.1M; i <= 0.9M; i += 0.1M)
            {
                var item = new ToolStripMenuItem($"{i:P0}")
                {
                    Tag = i,
                    Checked = i == Properties.Settings.Default.TestSummaryMaxHeightPct
                };
                item.Click += SetTestSummaryMaxPct;
                tsSummaryMaxHeight.DropDownItems.Add(item);
            }
        }

        private void UpdateTestSummaryMaxPctMenuItems()
        {
            foreach (ToolStripMenuItem item in tsSummaryMaxHeight.DropDownItems)
            {
                if (item.Tag is decimal pct)
                {
                    item.Checked = pct == Properties.Settings.Default.TestSummaryMaxHeightPct;
                }
            }
        }
    }
}