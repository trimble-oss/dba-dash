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
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace DBADashGUI
{
    public partial class Summary : UserControl, ISetContext
    {
        private List<int> refreshInstanceIDs;
        private bool refreshIncludeHidden;

        private DateTime? lastRefresh;

        public Summary()
        {
            InitializeComponent();
            dgvTests.AutoGenerateColumns = false;
            dgvTests.Columns.AddRange(TestCols.ToArray());
        }

        private DataView dv;

        private Dictionary<string, bool> statusColumns;

        private bool FocusedView { get => focusedViewToolStripMenuItem.Checked; }
        private DBADashContext context;
        private bool ShowHidden => context.InstanceIDs.Count == 1 || Common.ShowHidden;

        private CorruptionViewer CorruptionFrm = null;
        private bool WasRefreshed;

        private readonly Dictionary<string, string> tabMapping = new() { { "FullBackupStatus", "tabBackups" }, { "LogShippingStatus", "tabLogShipping" }, { "DiffBackupStatus", "tabBackups" }, { "LogBackupStatus", "tabBackups" }, { "DriveStatus", "tabDrives" },
                                                            { "JobStatus", "tabJobs" }, { "CollectionErrorStatus", "tabDBADashErrorLog"}, { "AGStatus", "tabAG" }, {"LastGoodCheckDBStatus","tabLastGood"}, {"SnapshotAgeStatus","tabCollectionDates"  },
                                                            {"MemoryDumpStatus","" }, {"UptimeStatus","" }, {"CorruptionStatus","" }, {"AlertStatus","tabAlerts" }, {"FileFreeSpaceStatus","tabFiles" },
                                                            {"CustomCheckStatus","tabCustomChecks"  }, {"MirroringStatus","tabMirroring" },{"ElasticPoolStorageStatus","tabAzureSummary"},{"PctMaxSizeStatus","tabFiles"}, {"QueryStoreStatus","tabQS" },
                                                            {"LogFreeSpaceStatus","tabFiles"},{"DBMailStatus","" },{"IdentityStatus","tabIdentityColumns"  }, {"IsAgentRunningStatus","" },{"DatabaseStateStatus","tabDBOptions" }};

        private void ResetStatusCols()
        {
            statusColumns = new Dictionary<string, bool> { { "FullBackupStatus", false }, { "LogShippingStatus", false }, { "DiffBackupStatus", false }, { "LogBackupStatus", false }, { "DriveStatus", false },
                                                            { "JobStatus", false }, { "CollectionErrorStatus", false }, { "AGStatus", false }, {"LastGoodCheckDBStatus",false}, {"SnapshotAgeStatus",false },
                                                            {"MemoryDumpStatus",false }, {"UptimeStatus",false }, {"CorruptionStatus",false }, {"AlertStatus",false }, {"FileFreeSpaceStatus",false },
                                                            {"CustomCheckStatus",false }, {"MirroringStatus",false },{"ElasticPoolStorageStatus",false},{"PctMaxSizeStatus",false}, {"QueryStoreStatus",false },
                                                            {"LogFreeSpaceStatus",false },{"DBMailStatus",false },{"IdentityStatus",false },{"IsAgentRunningStatus",false },{"DatabaseStateStatus",false} };
        }

        private Task<DataTable> GetSummaryAsync(bool forceRefresh, DateTime? forceRefreshDate)
        {
            return Task<DataTable>.Factory.StartNew(() =>
            {
                using (var cn = new SqlConnection(Common.ConnectionString))
                using (var cmd = new SqlCommand("dbo.Summary_Get", cn) { CommandType = CommandType.StoredProcedure })
                using (var da = new SqlDataAdapter(cmd))
                {
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
                }
            });
        }

        public void SetContext(DBADashContext context)
        {
            this.context = context;
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

        public void RefreshData(bool forceRefresh = false, DateTime? forceRefreshDate = null)
        {
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
                    refresh1.Invoke(() => { refresh1.SetFailed("Error:" + task.Exception.ToString()); });
                    return Task.CompletedTask;
                }
                DataTable dt = task.Result;
                try
                {
                    GroupSummaryByTest(ref dt);
                    UpdateSummary(ref dt);
                }
                catch (Exception ex)
                {
                    refresh1.Invoke(() => { refresh1.SetFailed("Error:" + ex.ToString()); });
                }

                return Task.CompletedTask;
            }, cancellationTS.Token);
        }

        public void LoadSavedLayout()
        {
            try
            {
                SummarySavedView saved = SummarySavedView.GetDefaultSavedView();

                if (saved == null) return;
                Common.ShowHidden = saved.ShowHidden;
                focusedViewToolStripMenuItem.Checked = saved.FocusedView;
                showTestSummaryToolStripMenuItem.Checked = saved.ShowTestSummary;
                splitContainer1.Panel1Collapsed = !showTestSummaryToolStripMenuItem.Checked;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading saved view\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            DataTable grouped = GroupedByTestSchema();
            Dictionary<string, DataRow> tests = new();

            // Add a row for each test with zeros/defaults
            foreach (string statusCol in statusColumns.Keys)
            {
                DataRow row = grouped.NewRow();
                row["Test"] = statusCol;
                row["DisplayText"] = dgvSummary.Columns[statusCol].HeaderText;
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
                foreach (string statusCol in statusColumns.Keys)
                {
                    DataRow groupedRow = tests[statusCol];
                    var status = (DBADashStatus.DBADashStatusEnum)Convert.ToInt32(row[statusCol] == DBNull.Value ? 3 : row[statusCol]);
                    groupedRow[status.ToString()] = (int)groupedRow[status.ToString()] + 1;
                    groupedRow["Total"] = (int)groupedRow["Total"] + 1;
                    if (status == DBADashStatusEnum.Warning || status == DBADashStatusEnum.Critical)
                    {
                        groupedRow["IsFocusedRow"] = true;
                    }
                }
            }
            tests.Values.CopyToDataTable(grouped, LoadOption.OverwriteChanges);

            dv = new DataView(grouped, TestRowFilter, "DisplayText", DataViewRowState.CurrentRows);
            dgvTests.Invoke((Action)(() =>
            {
                dgvTests.DataSource = dv;
            }));
            // Auto size split container
            if (dgvTests.Rows.Count > 0)
            {
                splitContainer1.Invoke(() =>
                {
                    splitContainer1.SplitterDistance = ((dgvTests.Rows[0].Height + 1) * dgvTests.Rows.Count) + dgvTests.ColumnHeadersHeight;
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
            var cols = (statusColumns.Keys).ToList<string>();
            dt.Columns.Add("IsFocusedRow", typeof(bool));
            foreach (DataRow row in dt.Rows)
            {
                bool isFocusedRow = false;
                foreach (string col in cols)
                {
                    var status = (DBADashStatus.DBADashStatusEnum)Convert.ToInt32(row[col] == DBNull.Value ? 3 : row[col]);
                    if (!(status == DBADashStatus.DBADashStatusEnum.NA || (status == DBADashStatus.DBADashStatusEnum.OK && FocusedView)))
                    {
                        statusColumns[col] = true;
                        isFocusedRow = true;
                    }
                }

                if (row["IsAgentRunning"] != DBNull.Value && (bool)row["IsAgentRunning"] == false)
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
                dgvSummary.AutoResizeColumn(Instance.Index, DataGridViewAutoSizeColumnMode.DisplayedCells);
            }
            ));

            toolStrip1.Invoke(() =>
            {
                UpdateRefreshTime();
                lblRefreshTime.ForeColor = DBADashStatusEnum.OK.GetColor();
            }
            );
            refresh1.Invoke((Action)(() => refresh1.Visible = false));
            splitContainer1.Invoke(() => splitContainer1.Visible = true);
            timer1.Enabled = true;
        }

        private string SummaryRowFilter => FocusedView ? "IsFocusedRow=1" : "";
        private string TestRowFilter => FocusedView ? "IsFocusedRow=1" : "OK>0 OR Warning>0 OR Critical>0";

        private void DgvSummary_RowAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (int idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)dgvSummary.Rows[idx].DataBoundItem;
                bool isAzure = row["InstanceID"] == DBNull.Value;
                var cols = (statusColumns.Keys).ToList<string>();
                foreach (var col in cols)
                {
                    var status = (DBADashStatus.DBADashStatusEnum)Convert.ToInt32(row[col] == DBNull.Value ? 3 : row[col]);
                    dgvSummary.Rows[idx].Cells[col].SetStatusColor(status);
                }
                string DBMailStatus = Convert.ToString(row["DBMailStatusDescription"]);
                dgvSummary.Rows[idx].Cells["DBMailStatus"].ToolTipText = DBMailStatus;

                dgvSummary.Rows[idx].Cells["FullBackupStatus"].Value = isAzure ? "" : "View";
                dgvSummary.Rows[idx].Cells["DiffBackupStatus"].Value = isAzure ? "" : "View";
                dgvSummary.Rows[idx].Cells["LogBackupStatus"].Value = isAzure ? "" : "View";
                dgvSummary.Rows[idx].Cells["DriveStatus"].Value = isAzure ? "" : "View";
                dgvSummary.Rows[idx].Cells["JobStatus"].Value = isAzure ? "" : "View";
                dgvSummary.Rows[idx].Cells["LogShippingStatus"].Value = isAzure ? "" : "View";
                dgvSummary.Rows[idx].Cells["AGStatus"].Value = (int)row["AGStatus"] == 3 ? "" : "View";
                dgvSummary.Rows[idx].Cells["QueryStoreStatus"].Value = (int)row["QueryStoreStatus"] == 3 ? "" : "View";
                dgvSummary.Rows[idx].Cells["CorruptionStatus"].Value = row["DetectedCorruptionDateUtc"] == DBNull.Value
                    ? ""
                    : DateTime.UtcNow.Subtract((DateTime)row["DetectedCorruptionDateUtc"]).Humanize(1);
                if (row["IsAgentRunning"] != DBNull.Value && (bool)row["IsAgentRunning"] == false)
                {
                    dgvSummary.Rows[idx].Cells["JobStatus"].SetStatusColor(Color.Black);
                    dgvSummary.Rows[idx].Cells["JobStatus"].Value = "Not Running";
                }

                string uptimeString;
                if (row["sqlserver_uptime"] != DBNull.Value)
                {
                    int uptime = (int)row["sqlserver_uptime"];
                    int addUptime = (int)row["AdditionalUptime"];
                    if (uptime < 120)
                    {
                        uptimeString = uptime.ToString() + " Mins (+" + addUptime.ToString() + "mins)";
                    }
                    else if (uptime < 1440)
                    {
                        uptimeString = (uptime / 60).ToString("0") + " Hours  (+" + addUptime.ToString() + "mins)";
                    }
                    else
                    {
                        uptimeString = (uptime / 1440).ToString("0") + " days";
                    }
                    dgvSummary.Rows[idx].Cells["UptimeStatus"].Value = uptimeString;
                }

                if (row["SnapshotAgeMin"] == DBNull.Value || row["SnapshotAgeMax"] == DBNull.Value)
                {
                    dgvSummary.Rows[idx].Cells["SnapshotAgeStatus"].Value = "N/A";
                }
                else
                {
                    int snapshotAgeMin = (int)row["SnapshotAgeMin"];
                    int snapshotAgeMax = (int)row["SnapshotAgeMax"];
                    if (snapshotAgeMax == snapshotAgeMin)
                    {
                        dgvSummary.Rows[idx].Cells["SnapshotAgeStatus"].Value = snapshotAgeMax + "mins";
                    }
                    else
                    {
                        dgvSummary.Rows[idx].Cells["SnapshotAgeStatus"].Value = snapshotAgeMin.ToString() + " to " + snapshotAgeMax.ToString() + "mins";
                    }
                }
                if (row["DaysSinceLastGoodCheckDB"] != DBNull.Value)
                {
                    dgvSummary.Rows[idx].Cells["LastGoodCheckDBStatus"].Value = ((int)row["DaysSinceLastGoodCheckDB"]).ToString() + " days";
                }
                string oldestLastGoodCheckDB = "Unknown";
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
                    ;
                }
                if (row["LastMemoryDump"] != DBNull.Value)
                {
                    DateTime lastMemoryDump = (DateTime)row["LastMemoryDump"];
                    DateTime lastMemoryDumpUTC = (DateTime)row["LastMemoryDumpUTC"];
                    int memoryDumpCount = (int)row["MemoryDumpCount"];
                    string lastMemoryDumpStr;

                    if (Math.Abs(lastMemoryDumpUTC.ToAppTimeZone().Subtract(lastMemoryDump).TotalMinutes) > 10)
                    {
                        lastMemoryDumpStr = "Last Memory Dump (local time): " + lastMemoryDumpUTC.ToAppTimeZone().ToString() + Environment.NewLine +
                           "Last Memory Dump (server time): " + lastMemoryDump.ToString() + Environment.NewLine +
                           "Total Memory Dumps: " + memoryDumpCount; ;
                    }
                    else
                    {
                        lastMemoryDumpStr = "Last Memory Dump: " + lastMemoryDumpUTC.ToAppTimeZone().ToString() + Environment.NewLine +
                           "Total Memory Dumps: " + memoryDumpCount; ;
                    }

                    dgvSummary.Rows[idx].Cells["MemoryDumpStatus"].Value = DateTime.UtcNow.Subtract(lastMemoryDumpUTC).Humanize(1);

                    dgvSummary.Rows[idx].Cells["MemoryDumpStatus"].ToolTipText = lastMemoryDumpStr;
                }
                string lastAlert = "Never";
                string lastAlertDays = "Never";
                string lastCriticalAlert = "Never";
                if (row["LastAlert"] != DBNull.Value)
                {
                    DateTime lastAlertD = (DateTime)row["LastAlert"];
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
                int totalAlerts = row["TotalAlerts"] == DBNull.Value ? 0 : (int)row["TotalAlerts"];

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
            DBADashContext ctx = new() { InstanceIDs = new HashSet<int>() { instanceID }, InstanceID = instanceID, InstanceName = instance };
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
                lblRefreshTime.ForeColor = DBADashStatusEnum.Critical.GetColor();
                timer1.Enabled = false;
            }
            else if (DateTime.UtcNow.Subtract(lastRefresh.Value).TotalMinutes > 10)
            {
                lblRefreshTime.ForeColor = DBADashStatusEnum.Warning.GetColor();
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
            for (int idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var gridRow = dgvTests.Rows[idx];
                var row = (DataRowView)gridRow.DataBoundItem;
                var rowStatus = DBADashStatusEnum.NA;
                foreach (var status in (DBADashStatusEnum[])Enum.GetValues(typeof(DBADashStatusEnum)))
                {
                    if (!row.Row.Table.Columns.Contains(status.ToString())) return;
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
            DataRowView row = (DataRowView)dgvTests.Rows[e.RowIndex].DataBoundItem;
            string test = (string)row["Test"];
            string tab = (string)tabMapping[test];
            if (e.ColumnIndex == 0)
            {
                if (test == CorruptionStatus.Name)
                {
                    ShowCorruptionViewer(context);
                }
                else if (string.IsNullOrEmpty(tab))
                {
                    FilterByStatus(new List<DBADashStatusEnum>() { DBADashStatusEnum.Warning, DBADashStatusEnum.Critical }, test);
                }
                else
                {
                    Instance_Selected?.Invoke(this, new InstanceSelectedEventArgs() { InstanceID = context.InstanceID, Instance = context.InstanceName, Tab = tab });
                }
            }
            else if (e.ColumnIndex is >= 1 and <= 5)
            {
                DBADashStatusEnum status = e.ColumnIndex switch
                {
                    1 => DBADashStatusEnum.OK,
                    2 => DBADashStatusEnum.Warning,
                    3 => DBADashStatusEnum.Critical,
                    4 => DBADashStatusEnum.NA,
                    5 => DBADashStatusEnum.Acknowledged,
                    _ => throw new Exception("Invalid ColumnIndex"),
                };
                FilterByStatus(status, test);
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
            foreach (DBADashStatusEnum status in statuses)
            {
                if (sbFilter.Length > 0)
                {
                    sbFilter.Append(" OR ");
                }
                sbFilter.Append(test + " = " + Convert.ToInt32(status).ToString());
            }
            dv.RowFilter = sbFilter.ToString();
            HideStatusColumns();
            dgvSummary.Columns[test].Visible = true;
            tsClearFilter.Enabled = true;
        }

        private void TsClearFilter_Click(object sender, EventArgs e)
        {
            var dv = (DataView)dgvSummary.DataSource;
            dv.RowFilter = SummaryRowFilter;
            tsClearFilter.Enabled = false;
            SetStatusColumnVisibility();
        }

        private void ShowTestSummaryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            splitContainer1.Panel1Collapsed = !showTestSummaryToolStripMenuItem.Checked;
        }

        private void ExportSummaryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgvSummary);
        }

        private void ExportTestSummaryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgvTests);
        }

        private void CopySummaryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgvSummary);
        }

        private void CopyTestSummaryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgvTests);
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
                MessageBox.Show("Error saving view options\n", ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
    }
}