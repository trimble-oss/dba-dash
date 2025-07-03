using DBADashGUI.Checks;
using Humanizer;
using Humanizer.Localisation;
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

        public Summary()
        {
            InitializeComponent();
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

        private CorruptionViewer CorruptionFrm;
        private bool WasRefreshed;

        private readonly Dictionary<string, string> tabMapping = new() { { "FullBackupStatus", "tabBackups" }, { "LogShippingStatus", "tabLogShipping" }, { "DiffBackupStatus", "tabBackups" }, { "LogBackupStatus", "tabBackups" }, { "DriveStatus", "tabDrives" },
                                                            { "JobStatus", "tabJobs" }, { "CollectionErrorStatus", "tabDBADashErrorLog"}, { "AGStatus", "tabAG" }, {"LastGoodCheckDBStatus","tabLastGood"}, {"SnapshotAgeStatus","tabCollectionDates"  },
                                                            {"MemoryDumpStatus","" }, {"UptimeStatus","" }, {"CorruptionStatus","" }, {"AlertStatus","tabSQLAgentAlerts" }, {"FileFreeSpaceStatus","tabFiles" },
                                                            {"CustomCheckStatus","tabCustomChecks"  }, {"MirroringStatus","tabMirroring" },{"ElasticPoolStorageStatus","tabAzureSummary"},{"PctMaxSizeStatus","tabFiles"}, {"QueryStoreStatus","tabQS" },
                                                            {"LogFreeSpaceStatus","tabFiles"},{"DBMailStatus","" },{"IdentityStatus","tabIdentityColumns"  }, {"IsAgentRunningStatus","" },{"DatabaseStateStatus","tabDBOptions" }};

        private void ResetStatusCols()
        {
            statusColumns = new Dictionary<string, bool> { { "FullBackupStatus", false }, { "LogShippingStatus", false }, { "DiffBackupStatus", false }, { "LogBackupStatus", false }, { "DriveStatus", false },
                                                            { "JobStatus", false }, { "CollectionErrorStatus", false }, { "AGStatus", false }, {"LastGoodCheckDBStatus",false}, {"SnapshotAgeStatus",false },
                                                            {"MemoryDumpStatus",false }, {"UptimeStatus",false }, {"CorruptionStatus",false }, {"AlertStatus",false }, {"FileFreeSpaceStatus",false },
                                                            {"CustomCheckStatus",false }, {"MirroringStatus",false },{"ElasticPoolStorageStatus",false},{"PctMaxSizeStatus",false}, {"QueryStoreStatus",false },
                                                            {"LogFreeSpaceStatus",false },{"DBMailStatus",false },{"IdentityStatus",false },{"IsAgentRunningStatus",false },{"DatabaseStateStatus",false}};
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

        public void RefreshData(bool forceRefresh, DateTime? forceRefreshDate = null)
        {
            if (context == null) return;
            if (!savedLayoutLoaded)
            {
                LoadSavedLayout();
                savedLayoutLoaded = true;
            }
            cancellationTS.Cancel(); // Cancel previous execution
            cancellationTS = new();
            dgvSummary.Columns[0].Frozen = Common.FreezeKeyColumn;
            ResetStatusCols();
            refresh1.ShowRefresh();
            splitContainer1.Visible = false;
            refreshInstanceIDs = new List<int>(context.InstanceIDs);
            refreshIncludeHidden = Common.ShowHidden;
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
                    UpdateSummary(ref dt);
                    AutoSizeSplitter();
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
        {           new DataGridViewLinkColumn(){ Name="Test", HeaderText="Test", DataPropertyName="DisplayText", SortMode = DataGridViewColumnSortMode.Automatic, Width=200},
                    new DataGridViewLinkColumn(){ Name="OK", HeaderText="Instance Count OK", DataPropertyName="OK", SortMode = DataGridViewColumnSortMode.Automatic },
                    new DataGridViewLinkColumn(){ Name = "Warning",  HeaderText="Instance Count Warning", DataPropertyName="Warning", SortMode = DataGridViewColumnSortMode.Automatic },
                    new DataGridViewLinkColumn(){ Name = "Critical",  HeaderText="Instance Count Critical", DataPropertyName="Critical", SortMode = DataGridViewColumnSortMode.Automatic },
                    new DataGridViewLinkColumn(){ Name = "NA",  HeaderText="Instance Count N/A", DataPropertyName="NA",SortMode = DataGridViewColumnSortMode.Automatic },
                    new DataGridViewLinkColumn(){ Name = "Acknowledged",  HeaderText="Instance Count Acknowledged", DataPropertyName="Acknowledged", SortMode = DataGridViewColumnSortMode.Automatic }
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

        private void UpdateSummary(ref DataTable dt)
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
                row["IsFocusedRow"] = isFocusedRow;
                lastRefresh = (DateTime)row["RefreshDate"];
            }
            dgvSummary.Invoke(SetStatusColumnVisibility);
            dgvSummary.Invoke((Action)(() => colHidden.Visible = ShowHidden));

            dv = new DataView(dt, SummaryRowFilter, "Instance", DataViewRowState.CurrentRows);
            dgvSummary.Invoke((Action)(() =>
            {
                dgvSummary.DataSource = dv;
                dgvSummary.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            }
            ));

            toolStrip1.Invoke(() =>
            {
                UpdateRefreshTime();
                lblRefreshTime.ForeColor = DBADashStatusEnum.OK.GetBackColor();
            }
            );
            refresh1.Invoke((Action)(() => refresh1.Visible = false));
            splitContainer1.Invoke(() => splitContainer1.Visible = true);
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

                dgvSummary.Rows[idx].Cells["FullBackupStatus"].Value = isAzure ? "" : "View";
                dgvSummary.Rows[idx].Cells["DiffBackupStatus"].Value = isAzure ? "" : "View";
                dgvSummary.Rows[idx].Cells["LogBackupStatus"].Value = isAzure ? "" : "View";
                dgvSummary.Rows[idx].Cells["DriveStatus"].Value = isAzure ? "" : "View";
                dgvSummary.Rows[idx].Cells["JobStatus"].Value = isAzure ? "" : "View";
                dgvSummary.Rows[idx].Cells["LogShippingStatus"].Value = isAzure ? "" : "View";
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

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData(false, lastRefresh);
        }

        public event EventHandler<InstanceSelectedEventArgs> Instance_Selected;

        private void DgvSummary_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var col = dgvSummary.Columns[e.ColumnIndex].Name;
            var tab = tabMapping.ContainsKey(col) ? tabMapping[dgvSummary.Columns[e.ColumnIndex].Name] : string.Empty;
            var row = (DataRowView)dgvSummary.Rows[e.RowIndex].DataBoundItem;
            if (e.ColumnIndex == UptimeStatus.Index)
            {
                ShowUptimeThresholdConfig((int)row["InstanceID"]);
            }
            else if (e.ColumnIndex == CorruptionStatus.Index)
            {
                ShowCorruptionViewer((string)row["InstanceGroupName"], (int)row["InstanceID"]);
            }
            else if (tab != string.Empty)
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
            if (CorruptionFrm == null)
            {
                CorruptionFrm = new();
                CorruptionFrm.FormClosed += delegate { CorruptionFrm = null; };
            }
            CorruptionFrm.SetContext(ctx);
            CorruptionFrm.Show();
            CorruptionFrm.Focus();
        }

        private void FocusedViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshData();
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
                lblRefreshTime.ForeColor = DBADashStatusEnum.Critical.GetBackColor();
                timer1.Enabled = false;
            }
            else if (DateTime.UtcNow.Subtract(lastRefresh.Value).TotalMinutes > 10)
            {
                lblRefreshTime.ForeColor = DBADashStatusEnum.Warning.GetBackColor();
            }
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
            switch (e.ColumnIndex)
            {
                case 0 when test == CorruptionStatus.Name:
                    ShowCorruptionViewer(context);
                    break;

                case 0 when string.IsNullOrEmpty(tab):
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
            tsClearFilter.Enabled = true;
        }

        private void TsClearFilter_Click(object sender, EventArgs e)
        {
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