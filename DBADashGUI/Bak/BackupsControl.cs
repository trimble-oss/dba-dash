using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace DBADashGUI.Backups
{
    public partial class BackupsControl : UserControl
    {

        public bool IncludeCritical
        {
            get
            {
                return criticalToolStripMenuItem.Checked;
            }
            set
            {
                criticalToolStripMenuItem.Checked = value;
            }
        }

        public bool IncludeWarning
        {
            get
            {
                return warningToolStripMenuItem.Checked;
            }
            set
            {
                warningToolStripMenuItem.Checked = value;
            }
        }
        public bool IncludeNA
        {
            get
            {
                return undefinedToolStripMenuItem.Checked;
            }
            set
            {
                undefinedToolStripMenuItem.Checked = value;
            }
        }
        public bool IncludeOK
        {
            get
            {
                return OKToolStripMenuItem.Checked;
            }
            set
            {
                OKToolStripMenuItem.Checked = value;
            }
        }

        public List<Int32> InstanceIDs { get; set; }
        private int databaseID { get; set; }

        private List<Int32> backupInstanceIDs;

        public void RefreshBackups()
        {
            databaseID = 0;
            dgvBackups.DataSource = null;
            dgvBackups.Columns.Clear();

            backupInstanceIDs = new List<int>();
            tsBack.Enabled = false;
            refresh();
        }

        private void refresh()
        {
            refreshSummary();
            if (InstanceIDs.Count>0 && (splitContainer1.SplitterDistance+200) > splitContainer1.Height) // Sumary is taking up all the room so don't show DB level data.
            {
                splitContainer1.Panel2Collapsed = true;
            }
            else
            {
                splitContainer1.Panel2Collapsed = false;
                if (databaseID > 0)
                {
                    refreshLastBackup();
                }
                else
                {
                    refreshBackups();
                }
            }
            
        }

        private void refreshLastBackup()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.LastBackup_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("DatabaseID", databaseID);
                DataTable dtBackups = new DataTable();
                Common.ConvertUTCToLocal(ref dtBackups);
                da.Fill(dtBackups);
                dgvBackups.DataSource = null;
                dgvBackups.AutoGenerateColumns =false;
                dgvBackups.Columns.Clear();
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Database", DataPropertyName = "name", SortMode = DataGridViewColumnSortMode.Automatic });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Recovery Model", DataPropertyName = "recovery_model" });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Type", DataPropertyName = "backup_type_desc" });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Start Date", DataPropertyName = "backup_start_date_utc" });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Finish Date", DataPropertyName = "backup_finish_date_utc" });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Duration", DataPropertyName = "BackupDuration" });                    
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Backup Size (GB)", DataPropertyName = "BackupSizeGB", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N1" } });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Backup MB/sec", DataPropertyName = "BackupMBsec", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N1" } });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Backup Size (Compressed) (GB)", DataPropertyName = "BackupSizeCompressedGB", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N1" } });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Compression Saving %", DataPropertyName = "CompressionSavingPct", DefaultCellStyle = new DataGridViewCellStyle() { Format = "P1" } });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Backup Write MB/sec", DataPropertyName = "BackupWriteMBsec", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N1" } });
                dgvBackups.Columns.Add(new DataGridViewCheckBoxColumn() { HeaderText = "Is Partner Backup", DataPropertyName = "IsPartnerBackup" });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Partner", DataPropertyName = "Partner" });
                dgvBackups.Columns.Add(new DataGridViewCheckBoxColumn() { HeaderText = "Is Damaged", DataPropertyName = "is_damaged" });
                dgvBackups.Columns.Add(new DataGridViewCheckBoxColumn() { HeaderText = "Checksum", DataPropertyName = "has_backup_checksums" });
                dgvBackups.Columns.Add(new DataGridViewCheckBoxColumn() { HeaderText = "Is Compressed", DataPropertyName = "IsCompressed" });
                dgvBackups.Columns.Add(new DataGridViewCheckBoxColumn() { HeaderText = "Password Protected", DataPropertyName = "is_password_protected" });
                dgvBackups.Columns.Add(new DataGridViewCheckBoxColumn() { HeaderText = "Encrypted", DataPropertyName = "IsEncrypted" });
                dgvBackups.Columns.Add(new DataGridViewCheckBoxColumn() { HeaderText = "Snapshot Backup", DataPropertyName = "is_snapshot" });
                dgvBackups.Columns.Add(new DataGridViewCheckBoxColumn() { HeaderText = "Has Bulk Logged Data", DataPropertyName = "has_bulk_logged_data" });
                dgvBackups.Columns.Add(new DataGridViewCheckBoxColumn() { HeaderText = "Readonly", DataPropertyName = "is_readonly" });
                dgvBackups.Columns.Add(new DataGridViewCheckBoxColumn() { HeaderText = "Force Offline", DataPropertyName = "is_force_offline" });
                dgvBackups.Columns.Add(new DataGridViewCheckBoxColumn() { HeaderText = "Single User", DataPropertyName = "is_single_user" });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Key Algorythm", DataPropertyName = "key_algorithm" });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Encryptor_Type", DataPropertyName = "encryptor_type" });

                dgvBackups.DataSource = dtBackups;
                dgvBackups.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            }

        }
        

        private void refreshBackups()
        {
            UseWaitCursor = true;
            configureInstanceThresholdsToolStripMenuItem.Enabled = (InstanceIDs.Count == 1);

            using (var cn = new SqlConnection(Common.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.Backups_Get", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
                cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                cmd.Parameters.AddWithValue("IncludeCritical", IncludeCritical);
                cmd.Parameters.AddWithValue("IncludeWarning", IncludeWarning);
                cmd.Parameters.AddWithValue("IncludeNA", IncludeNA);
                cmd.Parameters.AddWithValue("IncludeOK", IncludeOK);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dtBackups = new DataTable();
                da.Fill(dtBackups);
                Common.ConvertUTCToLocal(ref dtBackups);
                dgvBackups.AutoGenerateColumns = false;
                dgvBackups.DataSource = null;
                dgvBackups.Columns.Clear();
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Instance", DataPropertyName = "Instance", Name = "Instance" });
                dgvBackups.Columns.Add(new DataGridViewLinkColumn { HeaderText = "Database", DataPropertyName = "name",SortMode= DataGridViewColumnSortMode.Automatic });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Created", DataPropertyName = "create_date_utc" });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Recovery Model", DataPropertyName = "recovery_model_desc" });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Last Full", DataPropertyName = "LastFull", Name = "LastFull" });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Last Diff", DataPropertyName = "LastDiff", Name = "LastDiff" });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Last Log", DataPropertyName = "LastLog", Name = "LastLog" });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Last Filegroup", DataPropertyName = "LastFG", Name = "LastFG" });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Last Filegroup Diff", DataPropertyName = "LastFGDiff", Name = "LastFGDiff" });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Last Partial", DataPropertyName = "LastPartial", Name = "LastPartial" });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Last Partial Diff", DataPropertyName = "LastPartial", Name = "LastPartialDiff" });
                dgvBackups.Columns.Add(new DataGridViewCheckBoxColumn() { HeaderText = "Is Partner Backup", DataPropertyName = "IsPartnerBackup" });
                dgvBackups.Columns.Add(new DataGridViewCheckBoxColumn() { HeaderText = "Is Full Damaged", DataPropertyName = "IsFullDamaged" });
                dgvBackups.Columns.Add(new DataGridViewCheckBoxColumn() { HeaderText = "Is Diff Damaged", DataPropertyName = "IsDiffDamaged" });
                dgvBackups.Columns.Add(new DataGridViewCheckBoxColumn() { HeaderText = "Is Log Damaged", DataPropertyName = "IsLogDamaged" });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Snapshot Date", DataPropertyName = "SnapshotDate" });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Snapshot Age", DataPropertyName = "SnapshotAge", Name = "SnapshotAge" });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Threshold Configured Level", DataPropertyName = "ThresholdsConfiguredLevel" });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Full Backup Excluded Reason", DataPropertyName = "FullBackupExcludedReason" });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Diff Backup Excluded Reason", DataPropertyName = "DiffBackupExcludedReason" });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Log Backup Excluded Reason", DataPropertyName = "LogBackupExcludedReason" });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Log Backup Warning Threshold", DataPropertyName = "LogBackupWarningThreshold" });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Log Backup Critical Threshold", DataPropertyName = "LogBackupCriticalThreshold" });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Full Backup Warning Threshold", DataPropertyName = "FullBackupWarningThreshold" });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Full Backup Critical Threshold", DataPropertyName = "FullBackupCriticalThreshold" });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Diff Backup Warning Threshold", DataPropertyName = "DiffBackupWarningThreshold" });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Diff Backup Critical Threshold", DataPropertyName = "DiffBackupCriticalThreshold" });
                dgvBackups.Columns.Add(new DataGridViewCheckBoxColumn() { HeaderText = "Consider Partial Backups", DataPropertyName = "ConsiderPartialBackups" });
                dgvBackups.Columns.Add(new DataGridViewCheckBoxColumn() { HeaderText = "Consider FG Backups", DataPropertyName = "ConsiderFGBackups" });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Last Full Duration", DataPropertyName = "LastFullDuration" });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Full Backup Size (GB)", DataPropertyName = "FullBackupSizeGB", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N1" } });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Full Backup MB/sec", DataPropertyName = "FullBackupMBsec", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N1" } });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Full Backup Size (Compressed) (GB)", DataPropertyName = "FullBackupSizeCompressedGB", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N1" } });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Full Compression Saving %", DataPropertyName = "FullCompressionSavingPct", DefaultCellStyle = new DataGridViewCellStyle() { Format = "P1" } });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Full Backup Write MB/sec", DataPropertyName = "FullBackupWriteMBsec", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N1" } });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Diff Backup Size (GB)", DataPropertyName = "DiffBackupSizeGB", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N1" } });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Diff Backup MB/sec", DataPropertyName = "DiffBackupMBsec", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N1" } });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Diff Backup Size (Compressed) (GB)", DataPropertyName = "DiffBackupSizeCompressedGB", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N1" } });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Diff Compression Saving %", DataPropertyName = "DiffCompressionSavingPct", DefaultCellStyle = new DataGridViewCellStyle() { Format = "P1" } });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Diff Backup Write MB/sec", DataPropertyName = "DiffBackupWriteMBsec", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N1" } });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Checksums", DataPropertyName = "Checksums" });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Is Compressed", DataPropertyName = "IsCompressed" });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Password Protected", DataPropertyName = "PasswordProtected" });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Encrypted", DataPropertyName = "IsEncrypted" });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Snapshot Backup", DataPropertyName = "SnapshotBackups" });
                dgvBackups.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "Configure", Text = "Configure", UseColumnTextForLinkValue = true, SortMode = DataGridViewColumnSortMode.NotSortable, Name = "Configure" });
                
                dgvBackups.DataSource = new DataView(dtBackups);
                dgvBackups.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            }

            UseWaitCursor = false;
        }

        private void refreshSummary()
        {
            UseWaitCursor = true;
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.BackupSummary_Get", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
                cmd.Parameters.AddWithValue("InstanceIDs", InstanceIDs.AsDataTable());
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                Common.ConvertUTCToLocal(ref dt);
                dgvSummary.AutoGenerateColumns = false;
                if (dgvSummary.Columns.Count == 0)
                {
                    dgvSummary.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "Instance", DataPropertyName = "Instance", SortMode = DataGridViewColumnSortMode.Automatic, Name = "Instance" });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Database Count", DataPropertyName = "DatabaseCount" });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Full Backup OK", DataPropertyName = "FullOK", Name = "FullOK" });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Full Backup N/A", DataPropertyName = "FullNA", Name = "FullNA" });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Full Backup Warning", DataPropertyName = "FullWarning", Name = "FullWarning" });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Full Backup Critical", DataPropertyName = "FullCritical", Name = "FullCritical" });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Diff Backup OK", DataPropertyName = "DiffOK", Name = "DiffOK" });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Diff Backup N/A", DataPropertyName = "DiffNA", Name = "DiffNA" });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Diff Backup Warning", DataPropertyName = "DiffWarning", Name = "DiffWarning" });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Diff Backup Critical", DataPropertyName = "DiffCritical", Name = "DiffCritical" });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Log Backup OK", DataPropertyName = "LogOK", Name = "LogOK" });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Log Backup N/A", DataPropertyName = "LogNA", Name = "LogNA" });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Log Backup Warning", DataPropertyName = "LogWarning", Name = "LogWarning" });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Log Backup Critical", DataPropertyName = "LogCritical", Name = "LogCritical" });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Full Recovery Count", DataPropertyName = "FullRecoveryCount" });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Bulk Logged Count", DataPropertyName = "BulkLoggedCount" });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Simple Count", DataPropertyName = "SimpleCount" });
                    dgvSummary.Columns.Add(new DataGridViewCheckBoxColumn() { HeaderText = "Is Partner Backup", DataPropertyName = "IsPartnerBackup" });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Snapshot Date", DataPropertyName = "SnapshotDate" });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Snapshot Age", DataPropertyName = "SnapshotAge", Name = "SnapshotAge" });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Oldest Full", DataPropertyName = "OldestFull", ToolTipText="Date of oldest full backup, excluding databases without a threshold configured" });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Oldest Diff", DataPropertyName = "OldestDiff" , ToolTipText = "Date of oldest diff backup, excluding databases without a threshold configured" });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Oldest Log", DataPropertyName = "OldestLog", ToolTipText = "Date of oldest log backup, excluding databases without a threshold configured" });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Full Backup Size (GB)", DataPropertyName = "FullBackupSizeGB", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N1" } });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Full Backup MB/sec", DataPropertyName = "FullBackupMBsec", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N1" } });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Full Backup Size (Compressed) (GB)", DataPropertyName = "FullBackupSizeCompressedGB", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N1" } });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Full Compression Saving %", DataPropertyName = "FullCompressionSavingPct", DefaultCellStyle = new DataGridViewCellStyle() { Format = "P1" } });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Full Backup Write MB/sec", DataPropertyName = "FullBackupWriteMBsec", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N1" } });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Diff Backup Size (GB)", DataPropertyName = "DiffBackupSizeGB", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N1" } });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Diff Backup MB/sec", DataPropertyName = "DiffBackupMBsec", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N1" } });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Diff Backup Size (Compressed) (GB)", DataPropertyName = "DiffBackupSizeCompressedGB", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N1" } });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Diff Compression Saving %", DataPropertyName = "DiffCompressionSavingPct", DefaultCellStyle = new DataGridViewCellStyle() { Format = "P1" } });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Diff Backup Write MB/sec", DataPropertyName = "DiffBackupWriteMBsec", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N1" } });                    
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Full Checksums", DataPropertyName = "FullChecksum" });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Diff Checksums", DataPropertyName = "DiffChecksum" });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Log Checksums", DataPropertyName = "LogChecksum" });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Full Compressed", DataPropertyName = "FullCompressed" });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Diff Compressed", DataPropertyName = "DiffCompressed" });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Log Compressed", DataPropertyName = "LogCompressed" });
                    dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Full Password Protected", DataPropertyName = "FullPasswordProtected" });
                    dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Diff Password Protected", DataPropertyName = "DiffPasswordProtected" });
                    dgvBackups.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Log Password Protected", DataPropertyName = "LogPasswordProtected" });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Full Encrypted", DataPropertyName = "FullEncrypted" });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Diff Encrypted", DataPropertyName = "DiffEncrypted" });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Log Encrypted", DataPropertyName = "LogEncrypted" });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Snapshot Backups", DataPropertyName = "SnapshotBackups" });                   
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "DB Level Threshold Config", DataPropertyName = "DBThresholdConfiguration" });
                    dgvSummary.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "Configure", Text = "Configure", UseColumnTextForLinkValue = true, SortMode = DataGridViewColumnSortMode.NotSortable, Name = "Configure" });
                }
                dgvSummary.DataSource = new DataView(dt);               
                splitContainer1.SplitterDistance = (dgvSummary.Rows.Count * 24) + dgvSummary.ColumnHeadersHeight+24; // Set size based on row count, header size and scrollbar
                dgvSummary.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            }
            UseWaitCursor = false;
        }

        public BackupsControl()
        {
            InitializeComponent();
        }



        private void dgvBackups_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            if (dgvBackups.Columns.Contains("LastFull"))
            {
                for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
                {
                    var row = (DataRowView)dgvBackups.Rows[idx].DataBoundItem;
                    var fullBackupStatus = (DBADashStatus.DBADashStatusEnum)row["FullBackupStatus"];
                    var diffBackupStatus = (DBADashStatus.DBADashStatusEnum)row["DiffBackupStatus"];
                    var logBackupStatus = (DBADashStatus.DBADashStatusEnum)row["LogBackupStatus"];
                    var snapshotStatus = (DBADashStatus.DBADashStatusEnum)row["SnapshotAgeStatus"];
                    dgvBackups.Rows[idx].Cells["LastFull"].Style.BackColor = DBADashStatus.GetStatusColour(fullBackupStatus);
                    dgvBackups.Rows[idx].Cells["LastDiff"].Style.BackColor = DBADashStatus.GetStatusColour(diffBackupStatus);
                    dgvBackups.Rows[idx].Cells["LastLog"].Style.BackColor = DBADashStatus.GetStatusColour(logBackupStatus);
                    if ((bool)row["ConsiderPartialBackups"])
                    {
                        dgvBackups.Rows[idx].Cells["LastPartial"].Style.BackColor = dgvBackups.Rows[idx].Cells["LastFull"].Style.BackColor;
                        dgvBackups.Rows[idx].Cells["LastPartialDiff"].Style.BackColor = dgvBackups.Rows[idx].Cells["LastDiff"].Style.BackColor;
                    }
                    else
                    {
                        dgvBackups.Rows[idx].Cells["LastPartial"].Style.BackColor = Color.White;
                        dgvBackups.Rows[idx].Cells["LastPartialDiff"].Style.BackColor = Color.White;
                    }
                    if ((bool)row["ConsiderFGBackups"])
                    {
                        dgvBackups.Rows[idx].Cells["LastFG"].Style.BackColor = dgvBackups.Rows[idx].Cells["LastFull"].Style.BackColor;
                        dgvBackups.Rows[idx].Cells["LastFGDiff"].Style.BackColor = dgvBackups.Rows[idx].Cells["LastDiff"].Style.BackColor;
                    }
                    else
                    {
                        dgvBackups.Rows[idx].Cells["LastFG"].Style.BackColor = Color.White;
                        dgvBackups.Rows[idx].Cells["LastFGDiff"].Style.BackColor = Color.White;
                    }
                    dgvBackups.Rows[idx].Cells["SnapshotAge"].Style.BackColor = DBADashStatus.GetStatusColour(snapshotStatus);
                    if ((string)row["ThresholdsConfiguredLevel"] == "Database")
                    {
                        dgvBackups.Rows[idx].Cells["Configure"].Style.Font = new Font(dgvBackups.Font, FontStyle.Bold);
                    }
                    else
                    {
                        dgvBackups.Rows[idx].Cells["Configure"].Style.Font = new Font(dgvBackups.Font, FontStyle.Regular);
                    }
                }
            }

        }

        private void dgvBackups_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgvBackups.Columns.Contains("Configure"))
            {
                var row = (DataRowView)dgvBackups.Rows[e.RowIndex].DataBoundItem;
                if (dgvBackups.Columns[e.ColumnIndex].Name == "Configure")
                {                   
                    ConfigureThresholds((Int32)row["InstanceID"], (Int32)row["DatabaseID"]);
                }
                else if (dgvBackups.Columns[e.ColumnIndex].HeaderText == "Database")
                {
                    databaseID = (Int32)row["DatabaseID"];
                    tsBack.Enabled = true;
                    refresh();
                }
            }
        }

        private void ConfigureThresholds(Int32 InstanceID, Int32 DatabaseID)
        {
            var frm = new BackupThresholdsConfig
            {
                InstanceID = InstanceID,
                DatabaseID = DatabaseID
            };
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                refresh();
            }

        }

        private void configureRootThresholdsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConfigureThresholds(-1, -1);
        }

        private void configureInstanceThresholdsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (InstanceIDs.Count == 1)
            {
                ConfigureThresholds(InstanceIDs[0], -1);
            }
        }

        private void criticalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshBackups();

        }

        private void tsFilter_Click(object sender, EventArgs e)
        {
            refresh();
        }


        private void tsRefresh_Click(object sender, EventArgs e)
        {
            refresh();
        }

        private void tsCopy_Click(object sender, EventArgs e)
        {
            dgvSummary.Columns["Configure"].Visible = false;
            Common.CopyDataGridViewToClipboard(dgvSummary);
            dgvSummary.Columns["Configure"].Visible = true;
        }

        private void tsExcel_Click(object sender, EventArgs e)
        {
            dgvSummary.Columns["Configure"].Visible = false;
            Common.PromptSaveDataGridView(ref dgvSummary);
            dgvSummary.Columns["Configure"].Visible =true;
        }

        private void dgvSummary_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {

                var row = (DataRowView)dgvSummary.Rows[idx].DataBoundItem;
                var snapshotStatus = (DBADashStatus.DBADashStatusEnum)row["SnapshotAgeStatus"];

                dgvSummary.Rows[idx].Cells["SnapshotAge"].Style.BackColor = DBADashStatus.GetStatusColour(snapshotStatus);

                var okCols = new string[] { "FullOK", "DiffOK", "LogOK" };
                foreach (string col in okCols)
                {
                    int value = Convert.ToInt32(dgvSummary.Rows[idx].Cells[col].Value);
                    dgvSummary.Rows[idx].Cells[col].Style.BackColor = value > 0 ? Color.Green : Color.White;
                }
                var warningCols = new string[] { "FullWarning", "DiffWarning", "LogWarning" };
                foreach (string col in warningCols)
                {
                    int value = Convert.ToInt32(dgvSummary.Rows[idx].Cells[col].Value);
                    dgvSummary.Rows[idx].Cells[col].Style.BackColor = value > 0 ? Color.Yellow : Color.White;
                }
                var criticalCols = new string[] { "FullCritical", "DiffCritical", "LogCritical" };
                foreach (string col in criticalCols)
                {
                    int value = Convert.ToInt32(dgvSummary.Rows[idx].Cells[col].Value);
                    dgvSummary.Rows[idx].Cells[col].Style.BackColor = value > 0 ? Color.Red : Color.White;
                }
                dgvSummary.Rows[idx].Cells["Configure"].Style.Font = Convert.ToBoolean(row["InstanceThresholdConfiguration"]) ? new Font(dgvSummary.Font, FontStyle.Bold) : new Font(dgvSummary.Font, FontStyle.Regular);
                
            }
        }

        private void dgvSummary_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                if (dgvSummary.Columns[e.ColumnIndex].Name == "Instance" && backupInstanceIDs.Count == 0)
                {
                    databaseID = 0;
                    DataRowView row = (DataRowView)dgvSummary.Rows[e.RowIndex].DataBoundItem;
                    backupInstanceIDs = InstanceIDs;
                    InstanceIDs = new List<int>() { (int)row["InstanceID"] };
                    IncludeCritical = true;
                    IncludeWarning = true;
                    IncludeOK = true;
                    IncludeNA = true;
                    tsBack.Enabled = true;
                    refresh();
                }
                else if (dgvSummary.Columns[e.ColumnIndex].Name == "Configure")
                {
                    var row = (DataRowView)dgvSummary.Rows[e.RowIndex].DataBoundItem;
                    ConfigureThresholds((Int32)row["InstanceID"], -1);
                }
            }
        }

    

        private void tsBack_Click(object sender, EventArgs e)
        {
            if (databaseID > 0)
            {
                databaseID = 0;
            }
            else
            {
                InstanceIDs = backupInstanceIDs;
                backupInstanceIDs = new List<int>();
            }
            tsBack.Enabled = backupInstanceIDs.Count>0;
            IncludeCritical = true;
            IncludeWarning = true;
            IncludeOK = InstanceIDs.Count==1;
            IncludeNA = InstanceIDs.Count==1;
            refresh();
        }

        private void tsExcelDetail_Click(object sender, EventArgs e)
        {
            dgvBackups.Columns["Configure"].Visible = false;
            Common.PromptSaveDataGridView(ref dgvBackups);
            dgvBackups.Columns["Configure"].Visible = true;
        }

        private void tsCopyDetail_Click(object sender, EventArgs e)
        {
            dgvBackups.Columns["Configure"].Visible = false;
            Common.CopyDataGridViewToClipboard(dgvBackups);
            dgvBackups.Columns["Configure"].Visible = true;
        }
    }
}
