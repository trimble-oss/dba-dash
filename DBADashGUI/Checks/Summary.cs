using DBADashGUI.Checks;
using Humanizer;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static DBADashGUI.DBADashStatus;
using static DBADashGUI.Main;

namespace DBADashGUI
{
    public partial class Summary : UserControl, ISetContext
    {
        private List<Int32> refreshInstanceIDs;

        private DateTime lastRefresh;

        public Summary()
        {
            InitializeComponent();
        }

        private DataView dv;

        private Dictionary<string, bool> statusColumns;

        private bool focusedView = false;
        private DBADashContext context;

        private void ResetStatusCols()
        {
            statusColumns = new Dictionary<string, bool> { { "FullBackupStatus", false }, { "LogShippingStatus", false }, { "DiffBackupStatus", false }, { "LogBackupStatus", false }, { "DriveStatus", false },
                                                            { "JobStatus", false }, { "CollectionErrorStatus", false }, { "AGStatus", false }, {"LastGoodCheckDBStatus",false}, {"SnapshotAgeStatus",false },
                                                            {"MemoryDumpStatus",false }, {"UptimeStatus",false }, {"CorruptionStatus",false }, {"AlertStatus",false }, {"FileFreeSpaceStatus",false },
                                                            {"CustomCheckStatus",false }, {"MirroringStatus",false },{"ElasticPoolStorageStatus",false},{"PctMaxSizeStatus",false}, {"QueryStoreStatus",false },
                                                            {"LogFreeSpaceStatus",false },{"DBMailStatus",false },{"IdentityStatus",false } };
        }

        private Task<DataTable> GetSummaryAsync()
        {
            return Task<DataTable>.Factory.StartNew(() =>
            {
                using (var cn = new SqlConnection(Common.ConnectionString))
                using (var cmd = new SqlCommand("dbo.Summary_Get", cn) { CommandType = CommandType.StoredProcedure })
                using (var da = new SqlDataAdapter(cmd))
                {
                    cn.Open();

                    cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", context.InstanceIDs));
                    cmd.Parameters.AddWithValue("IncludeHidden", showHiddenToolStripMenuItem.Checked);
                    DataTable dt = new();
                    da.Fill(dt);
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
            if (DateTime.Now.Subtract(lastRefresh).TotalMinutes > 5 || InstanceIDsChanged())
            {
                RefreshData();
            }
            else
            {
                dgvSummary.Columns[0].Frozen = Common.FreezeKeyColumn;
            }
        }

        private bool InstanceIDsChanged()
        {
            return !(context.InstanceIDs.Count == refreshInstanceIDs.Count && refreshInstanceIDs.All(context.InstanceIDs.Contains));
        }

        private CancellationTokenSource cancellationTS = new();

        public void RefreshData()
        {
            cancellationTS.Cancel(); // Cancel previous execution
            cancellationTS = new();
            dgvSummary.Columns[0].Frozen = Common.FreezeKeyColumn;
            ResetStatusCols();
            refresh1.ShowRefresh();
            dgvSummary.Visible = false;
            refreshInstanceIDs = new List<int>(context.InstanceIDs);
            tsRefresh.Enabled = false;
            GetSummaryAsync().ContinueWith(task =>
            {
                toolStrip1.Invoke(() => tsRefresh.Enabled = true);
                if (task.Exception != null)
                {
                    refresh1.SetFailed("Error:" + task.Exception.ToString());
                    return;
                }
                DataTable dt = task.Result;
                UpdateSummary(ref dt);
            }, cancellationTS.Token);
        }

        private void UpdateSummary(ref DataTable dt)
        {
            dgvSummary.AutoGenerateColumns = false;
            var cols = (statusColumns.Keys).ToList<string>();
            dt.Columns.Add("IsFocusedRow", typeof(bool));
            foreach (DataRow row in dt.Rows)
            {
                bool isFocusedRow = false;
                foreach (string col in cols)
                {
                    var status = (DBADashStatus.DBADashStatusEnum)Convert.ToInt32(row[col] == DBNull.Value ? 3 : row[col]);
                    if (!(status == DBADashStatus.DBADashStatusEnum.NA || (status == DBADashStatus.DBADashStatusEnum.OK && focusedView)))
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
            }
            // hide columns that all have status N/A
            foreach (var col in statusColumns)
            {
                dgvSummary.Invoke((Action)(() => dgvSummary.Columns[col.Key].Visible = col.Value));
            }
            dgvSummary.Invoke((Action)(() => colShowInSummary.Visible = showHiddenToolStripMenuItem.Checked));
            string rowFilter = "";
            if (focusedView)
            {
                rowFilter = "IsFocusedRow=1";
            }
            dv = new DataView(dt, rowFilter, "Instance", DataViewRowState.CurrentRows);
            dgvSummary.Invoke((Action)(() =>
            {
                dgvSummary.DataSource = dv;
                dgvSummary.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            }
            ));
            lastRefresh = DateTime.Now;
            toolStrip1.Invoke((Action)(() =>
            {
                lblRefreshTime.Text = "Refresh Time: " + lastRefresh.ToString();
                lblRefreshTime.ForeColor = DBADashStatusEnum.OK.GetColor();
            }
            ));
            refresh1.Invoke((Action)(() => refresh1.Visible = false));
            dgvSummary.Invoke((Action)(() => dgvSummary.Visible = true));
            timer1.Enabled = true;
        }

        private void DgvSummary_RowAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
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
                if (row["IsAgentRunning"] != DBNull.Value && (bool)row["IsAgentRunning"] == false)
                {
                    dgvSummary.Rows[idx].Cells["JobStatus"].SetStatusColor(Color.Black);
                    dgvSummary.Rows[idx].Cells["JobStatus"].Value = "Not Running";
                }

                string uptimeString;
                if (row["sqlserver_uptime"] != DBNull.Value)
                {
                    Int32 uptime = (Int32)row["sqlserver_uptime"];
                    Int32 addUptime = (Int32)row["AdditionalUptime"];
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
                    Int32 snapshotAgeMin = (Int32)row["SnapshotAgeMin"];
                    Int32 snapshotAgeMax = (Int32)row["SnapshotAgeMax"];
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
                    dgvSummary.Rows[idx].Cells["LastGoodCheckDBStatus"].Value = ((Int32)row["DaysSinceLastGoodCheckDB"]).ToString() + " days";
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
                    dgvSummary.Rows[idx].Cells["LastGoodCheckDBStatus"].ToolTipText = "Last Good CheckDB Critical:" + (Int32)row["LastGoodCheckDBCriticalCount"] + Environment.NewLine +
                                                                               "Last Good CheckDB Warning:" + (Int32)row["LastGoodCheckDBWarningCount"] + Environment.NewLine +
                                                                               "Last Good CheckDB Good:" + (Int32)row["LastGoodCheckDBHealthyCount"] + Environment.NewLine +
                                                                               "Last Good CheckDB NA:" + (Int32)row["LastGoodCheckDBNACount"] + Environment.NewLine +
                                                                               "Oldest Last Good CheckDB:" + oldestLastGoodCheckDB;
                    ;
                }
                if (row["LastMemoryDump"] != DBNull.Value)
                {
                    DateTime lastMemoryDump = (DateTime)row["LastMemoryDump"];
                    DateTime lastMemoryDumpUTC = (DateTime)row["LastMemoryDumpUTC"];
                    Int32 memoryDumpCount = (Int32)row["MemoryDumpCount"];
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
                Int32 totalAlerts = row["TotalAlerts"] == DBNull.Value ? 0 : (Int32)row["TotalAlerts"];

                dgvSummary.Rows[idx].Cells["AlertStatus"].Value = lastAlertDays;
                dgvSummary.Rows[idx].Cells["AlertStatus"].ToolTipText = "Last Alert:" + lastAlert + Environment.NewLine +
                                                                        "Last Critical Alert:" + lastCriticalAlert + Environment.NewLine +
                                                                        "Total Alerts:" + totalAlerts;
                dgvSummary.Rows[idx].Cells["ElasticPoolStorageStatus"].Value = isAzure ? "View" : "";
            }
        }

        private void DgvSummary_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            var sortCol = "";
            if (dgvSummary.Columns[e.ColumnIndex] == FullBackupStatus)
            {
                sortCol = "FullBackupStatus";
            }
            if (dgvSummary.Columns[e.ColumnIndex] == AlertStatus)
            {
                sortCol = "AlertStatus";
            }
            if (dgvSummary.Columns[e.ColumnIndex] == SnapshotAgeStatus)
            {
                sortCol = "SnapshotAgeMax";
            }
            if (dgvSummary.Columns[e.ColumnIndex] == JobStatus)
            {
                sortCol = "JobStatus";
            }
            if (dgvSummary.Columns[e.ColumnIndex] == AGStatus)
            {
                sortCol = "AGStatus";
            }
            if (dgvSummary.Columns[e.ColumnIndex] == CorruptionStatus)
            {
                sortCol = "CorruptionStatus";
            }
            if (dgvSummary.Columns[e.ColumnIndex] == UptimeStatus)
            {
                sortCol = "sqlserver_uptime";
            }
            if (dgvSummary.Columns[e.ColumnIndex] == DriveStatus)
            {
                sortCol = "DriveStatus";
            }
            if (dgvSummary.Columns[e.ColumnIndex] == MemoryDumpStatus)
            {
                sortCol = "LastMemoryDump";
            }
            if (dgvSummary.Columns[e.ColumnIndex] == LastGoodCheckDBStatus)
            {
                sortCol = "LastGoodCheckDBStatus";
            }
            if (dgvSummary.Columns[e.ColumnIndex] == DiffBackupStatus)
            {
                sortCol = "DiffBackupStatus";
            }
            if (dgvSummary.Columns[e.ColumnIndex] == LogBackupStatus)
            {
                sortCol = "LogBackupStatus";
            }
            if (dgvSummary.Columns[e.ColumnIndex] == LogShippingStatus)
            {
                sortCol = "LogShippingStatus";
            }
            if (dgvSummary.Columns[e.ColumnIndex] == FileFreeSpaceStatus)
            {
                sortCol = "FileFreeSpaceStatus";
            }
            if (dgvSummary.Columns[e.ColumnIndex] == LogFreeSpaceStatus)
            {
                sortCol = "LogFreeSpaceStatus";
            }
            if (dgvSummary.Columns[e.ColumnIndex] == CollectionErrorStatus)
            {
                sortCol = "CollectionErrorStatus";
            }
            if (dgvSummary.Columns[e.ColumnIndex] == ElasticPoolStorageStatus)
            {
                sortCol = "ElasticPoolStorageStatus";
            }
            if (dgvSummary.Columns[e.ColumnIndex] == PctMaxSizeStatus)
            {
                sortCol = "PctMaxSizeStatus";
            }
            if (sortCol != "")
            {
                if (dv.Sort == sortCol)
                {
                    dv.Sort = sortCol += " DESC";
                }
                else
                {
                    dv.Sort = sortCol;
                }
            }
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgvSummary);
        }

        public event EventHandler<InstanceSelectedEventArgs> Instance_Selected;

        private void DgvSummary_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataRowView row = (DataRowView)dgvSummary.Rows[e.RowIndex].DataBoundItem;
                if (e.ColumnIndex == LastGoodCheckDBStatus.Index)
                {
                    if (row["InstanceID"] != DBNull.Value)
                    {
                        Instance_Selected(this, new InstanceSelectedEventArgs() { InstanceID = (Int32)row["InstanceID"], Tab = "tabLastGood" });
                    }
                }
                else if (e.ColumnIndex == FullBackupStatus.Index || e.ColumnIndex == DiffBackupStatus.Index || e.ColumnIndex == LogBackupStatus.Index)
                {
                    if (row["InstanceID"] != DBNull.Value)
                    {
                        Instance_Selected(this, new InstanceSelectedEventArgs() { InstanceID = (Int32)row["InstanceID"], Tab = "tabBackups" });
                    }
                }
                else if (e.ColumnIndex == LogShippingStatus.Index)
                {
                    if (row["InstanceID"] != DBNull.Value)
                    {
                        Instance_Selected(this, new InstanceSelectedEventArgs() { InstanceID = (Int32)row["InstanceID"], Tab = "tabLogShipping" });
                    }
                }
                else if (e.ColumnIndex == DriveStatus.Index)
                {
                    if (row["InstanceID"] != DBNull.Value)
                    {
                        Instance_Selected(this, new InstanceSelectedEventArgs() { InstanceID = (Int32)row["InstanceID"], Tab = "tabDrives" });
                    }
                }
                else if (e.ColumnIndex == JobStatus.Index)
                {
                    if (row["InstanceID"] != DBNull.Value)
                    {
                        Instance_Selected(this, new InstanceSelectedEventArgs() { InstanceID = (Int32)row["InstanceID"], Tab = "tabJobs" });
                    }
                }
                else if (e.ColumnIndex == FileFreeSpaceStatus.Index || e.ColumnIndex == PctMaxSizeStatus.Index || e.ColumnIndex == LogFreeSpaceStatus.Index)
                {
                    Instance_Selected(this, new InstanceSelectedEventArgs() { Instance = (string)row["InstanceGroupName"], Tab = "tabFiles" });
                }
                else if (e.ColumnIndex == CustomCheckStatus.Index)
                {
                    Instance_Selected(this, new InstanceSelectedEventArgs() { Instance = (string)row["InstanceGroupName"], Tab = "tabCustomChecks" });
                }
                else if (e.ColumnIndex == CollectionErrorStatus.Index)
                {
                    if (row["InstanceID"] != DBNull.Value)
                    {
                        Instance_Selected(this, new InstanceSelectedEventArgs() { InstanceID = (Int32)row["InstanceID"], Tab = "tabDBADashErrorLog" });
                    }
                    else
                    {
                        Instance_Selected(this, new InstanceSelectedEventArgs() { InstanceID = -1, Instance = (string)row["InstanceGroupName"], Tab = "tabDBADashErrorLog" });
                    }
                }
                else if (e.ColumnIndex == SnapshotAgeStatus.Index)
                {
                    Instance_Selected(this, new InstanceSelectedEventArgs() { Instance = (string)row["InstanceGroupName"], Tab = "tabCollectionDates" });
                }
                else if (e.ColumnIndex == AlertStatus.Index)
                {
                    if (row["InstanceID"] != DBNull.Value)
                    {
                        Instance_Selected(this, new InstanceSelectedEventArgs() { InstanceID = (Int32)row["InstanceID"], Tab = "tabAlerts" });
                    }
                }
                else if (e.ColumnIndex == ElasticPoolStorageStatus.Index)
                {
                    Instance_Selected(this, new InstanceSelectedEventArgs() { Instance = (string)row["InstanceGroupName"], Tab = "tabAzureSummary" });
                }
                else if (e.ColumnIndex == AGStatus.Index)
                {
                    Instance_Selected(this, new InstanceSelectedEventArgs() { Instance = (string)row["InstanceGroupName"], Tab = "tabAG" });
                }
                else if (e.ColumnIndex == QueryStoreStatus.Index)
                {
                    Instance_Selected(this, new InstanceSelectedEventArgs() { Instance = (string)row["InstanceGroupName"], Tab = "tabQS" });
                }
                else if (e.ColumnIndex == UptimeStatus.Index)
                {
                    var frm = new UptimeThresholdConfig() { InstanceID = (Int32)row["InstanceID"] };
                    frm.ShowDialog();
                    if (frm.DialogResult == DialogResult.OK)
                    {
                        RefreshData();
                    }
                }
                else if (e.ColumnIndex == IdentityStatus.Index)
                {
                    Instance_Selected(this, new InstanceSelectedEventArgs() { Instance = (string)row["InstanceGroupName"], Tab = "tabIdentityColumns" });
                }
            }
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgvSummary);
        }

        private void FocusedViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            focusedView = focusedViewToolStripMenuItem.Checked;
            RefreshData();
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            if (DateTime.Now.Subtract(lastRefresh).TotalMinutes > 60)
            {
                lblRefreshTime.ForeColor = DBADashStatusEnum.Critical.GetColor();
                timer1.Enabled = false;
            }
            else if (DateTime.Now.Subtract(lastRefresh).TotalMinutes > 10)
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
                RefreshData();
            }
        }

        private void AcknowledgeDumpsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MemoryDumpThresholds.Acknowledge();
            MessageBox.Show("Memory dump acknowledge date updated", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            RefreshData();
        }
    }
}