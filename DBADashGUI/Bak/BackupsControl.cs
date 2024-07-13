using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DBADashGUI.Interface;
using DBADashGUI.Theme;
using static DBADashGUI.DBADashStatus;
using DBADash;
using DBADashGUI.Messaging;
using DataTable = System.Data.DataTable;
using System.Threading;

namespace DBADashGUI.Backups
{
    public partial class BackupsControl : UserControl, INavigation, ISetContext, ISetStatus, IRefreshData
    {
        public bool IncludeCritical
        {
            get => statusFilterToolStrip1.Critical;
            set => statusFilterToolStrip1.Critical = value;
        }

        public bool IncludeWarning
        {
            get => statusFilterToolStrip1.Warning;
            set => statusFilterToolStrip1.Warning = value;
        }

        public bool IncludeNA
        {
            get => statusFilterToolStrip1.NA;
            set => statusFilterToolStrip1.NA = value;
        }

        public bool IncludeOK
        {
            get => statusFilterToolStrip1.OK;
            set => statusFilterToolStrip1.OK = value;
        }

        public bool CanNavigateBack => tsBack.Enabled;

        private List<int> InstanceIDs { get; set; }
        private int DatabaseID { get; set; }

        private List<int> backupInstanceIDs;

        private DBADashContext CurrentContext;
        private DateTime LastRefresh = DateTime.MinValue;
        private const int BackupDataIsStaleThreshold = 600; // seconds
        private CancellationTokenSource cancellationToken;

        public void SetContext(DBADashContext _context)
        {
            if (CurrentContext == _context &&  DateTime.Now.Subtract(LastRefresh).TotalSeconds < BackupDataIsStaleThreshold) return;
            LastRefresh = DateTime.Now;
            InstanceIDs = _context.RegularInstanceIDs.ToList();
            IncludeNA = _context.RegularInstanceIDs.Count == 1;
            IncludeOK = _context.RegularInstanceIDs.Count == 1;
            IncludeWarning = true;
            IncludeCritical = true;

            DatabaseID = 0;
            dgvBackups.DataSource = null;
            dgvBackups.Columns.Clear();

            backupInstanceIDs = new ();
            tsBack.Enabled = false;
            lblStatus.Text = "";
            tsTrigger.Visible = _context.CanMessage;

            CurrentContext = _context;
         
            RunRefreshDataLocal();
        }

        public void RefreshData()
        {
           RunRefreshDataLocal();
        }

        private void RunRefreshDataLocal()
        {
            cancellationToken?.Cancel();
            cancellationToken = new CancellationTokenSource();
            Task.Run(()=>RefreshDataLocal(cancellationToken.Token));
        }

        private async Task RefreshDataLocal(CancellationToken token)
        {
            Invoke(() =>
            {
                splitContainer1.Visible = false;
                refresh1.ShowRefresh();
            });

            var summaryTask = GetBackupSummaryAsync(token);
            var detailTask = DatabaseID > 0 ? GetLastBackupAsync(token) : GetBackupsAsync(token);

       
            try
            {
                await Task.WhenAll(summaryTask, detailTask);
                if (token.IsCancellationRequested) return;

                Invoke(() =>
                {
                    RefreshSummary(summaryTask.Result);
                    if (DatabaseID > 0)
                    {
                        RefreshLastBackup(detailTask.Result);
                    }
                    else
                    {
                        RefreshBackupsLocal(detailTask.Result);
                    }
                    splitContainer1.Panel2Collapsed = (InstanceIDs.Count > 0 &&
                                                       (splitContainer1.SplitterDistance + 200) > splitContainer1.Height); // Summary is taking up all the room so don't show DB level data.

                    splitContainer1.Visible = true;
                    refresh1.HideRefresh();
                });
            }
            catch (Exception ex)
            {
                if (token.IsCancellationRequested) return;
                Invoke(() =>
                {
                    refresh1.SetFailed(ex.Message);
                });
            }

        }

        private async Task<DataTable> GetLastBackupAsync(CancellationToken token)
        {
            await using var cn = new SqlConnection(Common.ConnectionString);
            await using SqlCommand cmd = new("dbo.LastBackup_Get", cn) { CommandType = CommandType.StoredProcedure };
            using SqlDataAdapter da = new(cmd);
            await cn.OpenAsync(token);
            cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
            DataTable dtBackups = new();
            await Task.Run(() => { da.Fill(dtBackups); }, token);
            DateHelper.ConvertUTCToAppTimeZone(ref dtBackups);
            return dtBackups;
        }

        private void RefreshLastBackup(DataTable dtBackups)
        {
            dgvBackups.DataSource = null;
            dgvBackups.AutoGenerateColumns = false;
            dgvBackups.Columns.Clear();
            dgvBackups.Columns.AddRange(
                new DataGridViewTextBoxColumn { HeaderText = "Database", DataPropertyName = "name", SortMode = DataGridViewColumnSortMode.Automatic, Frozen = Common.FreezeKeyColumn },
                new DataGridViewTextBoxColumn() { HeaderText = "Recovery Model", DataPropertyName = "recovery_model" },
                new DataGridViewTextBoxColumn() { HeaderText = "Type", DataPropertyName = "backup_type_desc" },
                new DataGridViewTextBoxColumn() { HeaderText = "Start Date", DataPropertyName = "backup_start_date_utc" },
                new DataGridViewTextBoxColumn() { HeaderText = "Finish Date", DataPropertyName = "backup_finish_date_utc" },
                new DataGridViewTextBoxColumn() { HeaderText = "Duration", DataPropertyName = "BackupDuration" },
                new DataGridViewTextBoxColumn() { HeaderText = "Backup Size (GB)", DataPropertyName = "BackupSizeGB", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N1" } },
                new DataGridViewTextBoxColumn() { HeaderText = "Backup MB/sec", DataPropertyName = "BackupMBsec", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N1" } },
                new DataGridViewTextBoxColumn() { HeaderText = "Backup Size (Compressed) (GB)", DataPropertyName = "BackupSizeCompressedGB", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N1" } },
                new DataGridViewTextBoxColumn() { HeaderText = "Compression Saving %", DataPropertyName = "CompressionSavingPct", DefaultCellStyle = new DataGridViewCellStyle() { Format = "P1" } },
                new DataGridViewTextBoxColumn() { HeaderText = "Backup Write MB/sec", DataPropertyName = "BackupWriteMBsec", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N1" } },
                new DataGridViewCheckBoxColumn() { HeaderText = "Is Partner Backup", DataPropertyName = "IsPartnerBackup", SortMode = DataGridViewColumnSortMode.Automatic },
                new DataGridViewTextBoxColumn() { HeaderText = "Partner", DataPropertyName = "Partner" },
                new DataGridViewCheckBoxColumn() { HeaderText = "Is Damaged", DataPropertyName = "is_damaged", SortMode = DataGridViewColumnSortMode.Automatic },
                new DataGridViewCheckBoxColumn() { HeaderText = "Checksum", DataPropertyName = "has_backup_checksums", SortMode = DataGridViewColumnSortMode.Automatic },
                new DataGridViewCheckBoxColumn() { HeaderText = "Is Compressed", DataPropertyName = "IsCompressed", SortMode = DataGridViewColumnSortMode.Automatic },
                new DataGridViewCheckBoxColumn() { HeaderText = "Password Protected", DataPropertyName = "is_password_protected", SortMode = DataGridViewColumnSortMode.Automatic },
                new DataGridViewCheckBoxColumn() { HeaderText = "Encrypted", DataPropertyName = "IsEncrypted", SortMode = DataGridViewColumnSortMode.Automatic },
                new DataGridViewCheckBoxColumn() { HeaderText = "Snapshot Backup", DataPropertyName = "is_snapshot", SortMode = DataGridViewColumnSortMode.Automatic },
                new DataGridViewCheckBoxColumn() { HeaderText = "Has Bulk Logged Data", DataPropertyName = "has_bulk_logged_data", SortMode = DataGridViewColumnSortMode.Automatic },
                new DataGridViewCheckBoxColumn() { HeaderText = "Readonly", DataPropertyName = "is_readonly", SortMode = DataGridViewColumnSortMode.Automatic },
                new DataGridViewCheckBoxColumn() { HeaderText = "Force Offline", DataPropertyName = "is_force_offline", SortMode = DataGridViewColumnSortMode.Automatic },
                new DataGridViewCheckBoxColumn() { HeaderText = "Single User", DataPropertyName = "is_single_user", SortMode = DataGridViewColumnSortMode.Automatic },
                new DataGridViewTextBoxColumn() { HeaderText = "Key Algorythm", DataPropertyName = "key_algorithm" },
                new DataGridViewTextBoxColumn() { HeaderText = "Encryptor_Type", DataPropertyName = "encryptor_type" },
                new DataGridViewTextBoxColumn() { HeaderText = "Compression Algorithm", DataPropertyName = "compression_algorithm" }
            );
            dgvBackups.ApplyTheme();
            dgvBackups.DataSource = dtBackups;
            dgvBackups.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private async Task<DataTable> GetBackupsAsync(CancellationToken token)
        {
            await using var cn = new SqlConnection(Common.ConnectionString);
            await using SqlCommand cmd = new("dbo.Backups_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            await cn.OpenAsync(token);
            cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
            cmd.Parameters.AddRange(statusFilterToolStrip1.GetSQLParams());
            cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);
            DataTable dtBackups = new();
            await Task.Run(() =>
            {
                da.Fill(dtBackups);
            }, token);
            DateHelper.ConvertUTCToAppTimeZone(ref dtBackups);
            return dtBackups;
        }

        private void RefreshBackupsLocal(DataTable dt)
        {
            UseWaitCursor = true;
            configureInstanceThresholdsToolStripMenuItem.Enabled = (InstanceIDs.Count == 1);
            dgvBackups.AutoGenerateColumns = false;
            dgvBackups.DataSource = null;
            dgvBackups.Columns.Clear();
            dgvBackups.Columns.AddRange(
                new DataGridViewTextBoxColumn() { HeaderText = "Instance", DataPropertyName = "InstanceDisplayName", Name = "Instance", Frozen = Common.FreezeKeyColumn },
                new DataGridViewLinkColumn { HeaderText = "Database", DataPropertyName = "name", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor, Frozen = Common.FreezeKeyColumn },
                new DataGridViewTextBoxColumn() { HeaderText = "Created", DataPropertyName = "create_date_utc" },
                new DataGridViewTextBoxColumn() { HeaderText = "Recovery Model", DataPropertyName = "recovery_model_desc" },
                new DataGridViewTextBoxColumn() { HeaderText = "Last Full", DataPropertyName = "LastFull", Name = "LastFull" },
                new DataGridViewTextBoxColumn() { HeaderText = "Last Diff", DataPropertyName = "LastDiff", Name = "LastDiff" },
                new DataGridViewTextBoxColumn() { HeaderText = "Last Log", DataPropertyName = "LastLog", Name = "LastLog" },
                new DataGridViewTextBoxColumn() { HeaderText = "Last Filegroup", DataPropertyName = "LastFG", Name = "LastFG" },
                new DataGridViewTextBoxColumn() { HeaderText = "Last Filegroup Diff", DataPropertyName = "LastFGDiff", Name = "LastFGDiff" },
                new DataGridViewTextBoxColumn() { HeaderText = "Last Partial", DataPropertyName = "LastPartial", Name = "LastPartial" },
                new DataGridViewTextBoxColumn() { HeaderText = "Last Partial Diff", DataPropertyName = "LastPartial", Name = "LastPartialDiff" },
                new DataGridViewCheckBoxColumn() { HeaderText = "Is Partner Backup", DataPropertyName = "IsPartnerBackup", SortMode = DataGridViewColumnSortMode.Automatic },
                new DataGridViewCheckBoxColumn() { HeaderText = "Is Full Damaged", DataPropertyName = "IsFullDamaged", SortMode = DataGridViewColumnSortMode.Automatic },
                new DataGridViewCheckBoxColumn() { HeaderText = "Is Diff Damaged", DataPropertyName = "IsDiffDamaged", SortMode = DataGridViewColumnSortMode.Automatic },
                new DataGridViewCheckBoxColumn() { HeaderText = "Is Log Damaged", DataPropertyName = "IsLogDamaged", SortMode = DataGridViewColumnSortMode.Automatic },
                new DataGridViewTextBoxColumn() { HeaderText = "Snapshot Date", DataPropertyName = "SnapshotDate" },
                new DataGridViewTextBoxColumn() { HeaderText = "Snapshot Age", DataPropertyName = "SnapshotAge", Name = "SnapshotAge" },
                new DataGridViewTextBoxColumn() { HeaderText = "Threshold Configured Level", DataPropertyName = "ThresholdsConfiguredLevel" },
                new DataGridViewTextBoxColumn() { HeaderText = "Full Backup Excluded Reason", DataPropertyName = "FullBackupExcludedReason" },
                new DataGridViewTextBoxColumn() { HeaderText = "Diff Backup Excluded Reason", DataPropertyName = "DiffBackupExcludedReason" },
                new DataGridViewTextBoxColumn() { HeaderText = "Log Backup Excluded Reason", DataPropertyName = "LogBackupExcludedReason" },
                new DataGridViewTextBoxColumn() { HeaderText = "Log Backup Warning Threshold", DataPropertyName = "LogBackupWarningThreshold" },
                new DataGridViewTextBoxColumn() { HeaderText = "Log Backup Critical Threshold", DataPropertyName = "LogBackupCriticalThreshold" },
                new DataGridViewTextBoxColumn() { HeaderText = "Full Backup Warning Threshold", DataPropertyName = "FullBackupWarningThreshold" },
                new DataGridViewTextBoxColumn() { HeaderText = "Full Backup Critical Threshold", DataPropertyName = "FullBackupCriticalThreshold" },
                new DataGridViewTextBoxColumn() { HeaderText = "Diff Backup Warning Threshold", DataPropertyName = "DiffBackupWarningThreshold" },
                new DataGridViewTextBoxColumn() { HeaderText = "Diff Backup Critical Threshold", DataPropertyName = "DiffBackupCriticalThreshold" },
                new DataGridViewCheckBoxColumn() { HeaderText = "Consider Partial Backups", DataPropertyName = "ConsiderPartialBackups", SortMode = DataGridViewColumnSortMode.Automatic },
                new DataGridViewCheckBoxColumn() { HeaderText = "Consider FG Backups", DataPropertyName = "ConsiderFGBackups", SortMode = DataGridViewColumnSortMode.Automatic },
                new DataGridViewTextBoxColumn() { HeaderText = "Last Full Duration", DataPropertyName = "LastFullDuration" },
                new DataGridViewTextBoxColumn() { HeaderText = "Full Backup Size (GB)", DataPropertyName = "FullBackupSizeGB", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N1" } },
                new DataGridViewTextBoxColumn() { HeaderText = "Full Backup MB/sec", DataPropertyName = "FullBackupMBsec", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N1" } },
                new DataGridViewTextBoxColumn() { HeaderText = "Full Backup Size (Compressed) (GB)", DataPropertyName = "FullBackupSizeCompressedGB", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N1" } },
                new DataGridViewTextBoxColumn() { HeaderText = "Full Compression Saving %", DataPropertyName = "FullCompressionSavingPct", DefaultCellStyle = new DataGridViewCellStyle() { Format = "P1" } },
                new DataGridViewTextBoxColumn() { HeaderText = "Full Backup Write MB/sec", DataPropertyName = "FullBackupWriteMBsec", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N1" } },
                new DataGridViewTextBoxColumn() { HeaderText = "Diff Backup Size (GB)", DataPropertyName = "DiffBackupSizeGB", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N1" } },
                new DataGridViewTextBoxColumn() { HeaderText = "Diff Backup MB/sec", DataPropertyName = "DiffBackupMBsec", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N1" } },
                new DataGridViewTextBoxColumn() { HeaderText = "Diff Backup Size (Compressed) (GB)", DataPropertyName = "DiffBackupSizeCompressedGB", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N1" } },
                new DataGridViewTextBoxColumn() { HeaderText = "Diff Compression Saving %", DataPropertyName = "DiffCompressionSavingPct", DefaultCellStyle = new DataGridViewCellStyle() { Format = "P1" } },
                new DataGridViewTextBoxColumn() { HeaderText = "Diff Backup Write MB/sec", DataPropertyName = "DiffBackupWriteMBsec", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N1" } },
                new DataGridViewCheckBoxColumn { HeaderText = "Full Checksum", DataPropertyName = "IsFullChecksum", SortMode = DataGridViewColumnSortMode.Automatic },
                new DataGridViewCheckBoxColumn { HeaderText = "Diff Checksum", DataPropertyName = "IsDiffChecksum", SortMode = DataGridViewColumnSortMode.Automatic },
                new DataGridViewCheckBoxColumn { HeaderText = "Log Checksum", DataPropertyName = "IsLogChecksum", SortMode = DataGridViewColumnSortMode.Automatic },
                new DataGridViewCheckBoxColumn() { HeaderText = "Full Compressed", DataPropertyName = "IsFullCompressed", SortMode = DataGridViewColumnSortMode.Automatic },
                new DataGridViewCheckBoxColumn() { HeaderText = "Diff Compressed", DataPropertyName = "IsDiffCompressed", SortMode = DataGridViewColumnSortMode.Automatic },
                new DataGridViewCheckBoxColumn() { HeaderText = "Log Compressed", DataPropertyName = "IsLogCompressed", SortMode = DataGridViewColumnSortMode.Automatic },
                new DataGridViewCheckBoxColumn() { HeaderText = "Full Password Protected", DataPropertyName = "IsFullPasswordProtected", SortMode = DataGridViewColumnSortMode.Automatic },
                new DataGridViewCheckBoxColumn() { HeaderText = "Diff Password Protected", DataPropertyName = "IsDiffPasswordProtected", SortMode = DataGridViewColumnSortMode.Automatic },
                new DataGridViewCheckBoxColumn() { HeaderText = "Log Password Protected", DataPropertyName = "IsLogPasswordProtected", SortMode = DataGridViewColumnSortMode.Automatic },
                new DataGridViewCheckBoxColumn() { HeaderText = "Full Encrypted", DataPropertyName = "IsFullEncrypted", SortMode = DataGridViewColumnSortMode.Automatic },
                new DataGridViewCheckBoxColumn() { HeaderText = "Diff Encrypted", DataPropertyName = "IsDiffEncrypted", SortMode = DataGridViewColumnSortMode.Automatic },
                new DataGridViewCheckBoxColumn() { HeaderText = "Log Encrypted", DataPropertyName = "IsLogEncrypted", SortMode = DataGridViewColumnSortMode.Automatic },
                new DataGridViewTextBoxColumn() { HeaderText = "Snapshot Backup", DataPropertyName = "SnapshotBackups" },
                new DataGridViewTextBoxColumn() { HeaderText = "Full Compression Algorithm", DataPropertyName = "FullCompressionAlgorithm" },
                new DataGridViewTextBoxColumn() { HeaderText = "Diff Compression Algorithm", DataPropertyName = "DiffCompressionAlgorithm" },
                new DataGridViewTextBoxColumn() { HeaderText = "Log Compression Algorithm", DataPropertyName = "LogCompressionAlgorithm" },
                new DataGridViewLinkColumn() { HeaderText = "Configure", Text = "Configure", UseColumnTextForLinkValue = true, SortMode = DataGridViewColumnSortMode.NotSortable, Name = "Configure", LinkColor = DashColors.LinkColor }
            );
            dgvBackups.ApplyTheme();
            dgvBackups.DataSource = new DataView(dt);
            dgvBackups.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);

            UseWaitCursor = false;
        }

        private async Task<DataTable> GetBackupSummaryAsync(CancellationToken token)
        {
            await using var cn = new SqlConnection(Common.ConnectionString);
            await using SqlCommand cmd = new("dbo.BackupSummary_Get", cn) { CommandType = CommandType.StoredProcedure };
            await cn.OpenAsync(token);
            cmd.Parameters.AddWithValue("InstanceIDs", InstanceIDs.AsDataTable());
            cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);

            await using var rdr = await cmd.ExecuteReaderAsync(token);
            DataTable dt = new();
            dt.Load(rdr);

            DateHelper.ConvertUTCToAppTimeZone(ref dt);
            return dt;
        }

        private void RefreshSummary(DataTable dt)
        {
            UseWaitCursor = true;
            dgvSummary.AutoGenerateColumns = false;
            if (dgvSummary.Columns.Count == 0)
            {
                dgvSummary.Columns.AddRange(
                    new DataGridViewLinkColumn() { HeaderText = "Instance", DataPropertyName = "InstanceDisplayName", SortMode = DataGridViewColumnSortMode.Automatic, Name = "Instance", LinkColor = DashColors.LinkColor, Frozen = Common.FreezeKeyColumn },
                    new DataGridViewTextBoxColumn() { HeaderText = "Database Count", DataPropertyName = "DatabaseCount" },
                    new DataGridViewTextBoxColumn() { HeaderText = "Full Backup OK", DataPropertyName = "FullOK", Name = "FullOK" },
                    new DataGridViewTextBoxColumn() { HeaderText = "Full Backup N/A", DataPropertyName = "FullNA", Name = "FullNA" },
                    new DataGridViewTextBoxColumn() { HeaderText = "Full Backup Warning", DataPropertyName = "FullWarning", Name = "FullWarning" },
                    new DataGridViewTextBoxColumn() { HeaderText = "Full Backup Critical", DataPropertyName = "FullCritical", Name = "FullCritical" },
                    new DataGridViewTextBoxColumn() { HeaderText = "Diff Backup OK", DataPropertyName = "DiffOK", Name = "DiffOK" },
                    new DataGridViewTextBoxColumn() { HeaderText = "Diff Backup N/A", DataPropertyName = "DiffNA", Name = "DiffNA" },
                    new DataGridViewTextBoxColumn() { HeaderText = "Diff Backup Warning", DataPropertyName = "DiffWarning", Name = "DiffWarning" },
                    new DataGridViewTextBoxColumn() { HeaderText = "Diff Backup Critical", DataPropertyName = "DiffCritical", Name = "DiffCritical" },
                    new DataGridViewTextBoxColumn() { HeaderText = "Log Backup OK", DataPropertyName = "LogOK", Name = "LogOK" },
                    new DataGridViewTextBoxColumn() { HeaderText = "Log Backup N/A", DataPropertyName = "LogNA", Name = "LogNA" },
                    new DataGridViewTextBoxColumn() { HeaderText = "Log Backup Warning", DataPropertyName = "LogWarning", Name = "LogWarning" },
                    new DataGridViewTextBoxColumn() { HeaderText = "Log Backup Critical", DataPropertyName = "LogCritical", Name = "LogCritical" },
                    new DataGridViewTextBoxColumn() { HeaderText = "Full Recovery Count", DataPropertyName = "FullRecoveryCount" },
                    new DataGridViewTextBoxColumn() { HeaderText = "Bulk Logged Count", DataPropertyName = "BulkLoggedCount" },
                    new DataGridViewTextBoxColumn() { HeaderText = "Simple Count", DataPropertyName = "SimpleCount" },
                    new DataGridViewCheckBoxColumn() { HeaderText = "Is Partner Backup", DataPropertyName = "IsPartnerBackup", SortMode = DataGridViewColumnSortMode.Automatic },
                    new DataGridViewTextBoxColumn() { HeaderText = "Snapshot Date", DataPropertyName = "SnapshotDate" },
                    new DataGridViewTextBoxColumn() { HeaderText = "Snapshot Age", DataPropertyName = "SnapshotAge", Name = "SnapshotAge" },
                    new DataGridViewTextBoxColumn() { HeaderText = "Oldest Full", DataPropertyName = "OldestFull", ToolTipText = "Date of oldest full backup, excluding databases without a threshold configured" },
                    new DataGridViewTextBoxColumn() { HeaderText = "Oldest Diff", DataPropertyName = "OldestDiff", ToolTipText = "Date of oldest diff backup, excluding databases without a threshold configured" },
                    new DataGridViewTextBoxColumn() { HeaderText = "Oldest Log", DataPropertyName = "OldestLog", ToolTipText = "Date of oldest log backup, excluding databases without a threshold configured" },
                    new DataGridViewTextBoxColumn() { HeaderText = "Full Backup Size (GB)", DataPropertyName = "FullBackupSizeGB", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N1" } },
                    new DataGridViewTextBoxColumn() { HeaderText = "Full Backup MB/sec", DataPropertyName = "FullBackupMBsec", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N1" } },
                    new DataGridViewTextBoxColumn() { HeaderText = "Full Backup Size (Compressed) (GB)", DataPropertyName = "FullBackupSizeCompressedGB", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N1" } },
                    new DataGridViewTextBoxColumn() { HeaderText = "Full Compression Saving %", DataPropertyName = "FullCompressionSavingPct", DefaultCellStyle = new DataGridViewCellStyle() { Format = "P1" } },
                    new DataGridViewTextBoxColumn() { HeaderText = "Full Backup Write MB/sec", DataPropertyName = "FullBackupWriteMBsec", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N1" } },
                    new DataGridViewTextBoxColumn() { HeaderText = "Diff Backup Size (GB)", DataPropertyName = "DiffBackupSizeGB", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N1" } },
                    new DataGridViewTextBoxColumn() { HeaderText = "Diff Backup MB/sec", DataPropertyName = "DiffBackupMBsec", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N1" } },
                    new DataGridViewTextBoxColumn() { HeaderText = "Diff Backup Size (Compressed) (GB)", DataPropertyName = "DiffBackupSizeCompressedGB", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N1" } },
                    new DataGridViewTextBoxColumn() { HeaderText = "Diff Compression Saving %", DataPropertyName = "DiffCompressionSavingPct", DefaultCellStyle = new DataGridViewCellStyle() { Format = "P1" } },
                    new DataGridViewTextBoxColumn() { HeaderText = "Diff Backup Write MB/sec", DataPropertyName = "DiffBackupWriteMBsec", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N1" } },
                    new DataGridViewTextBoxColumn() { HeaderText = "Full Checksums", DataPropertyName = "FullChecksum" },
                    new DataGridViewTextBoxColumn() { HeaderText = "Diff Checksums", DataPropertyName = "DiffChecksum" },
                    new DataGridViewTextBoxColumn() { HeaderText = "Log Checksums", DataPropertyName = "LogChecksum" },
                    new DataGridViewTextBoxColumn() { HeaderText = "Full Compressed", DataPropertyName = "FullCompressed" },
                    new DataGridViewTextBoxColumn() { HeaderText = "Diff Compressed", DataPropertyName = "DiffCompressed" },
                    new DataGridViewTextBoxColumn() { HeaderText = "Log Compressed", DataPropertyName = "LogCompressed" },
                    new DataGridViewTextBoxColumn() { HeaderText = "Full Password Protected", DataPropertyName = "FullPasswordProtected" },
                    new DataGridViewTextBoxColumn() { HeaderText = "Diff Password Protected", DataPropertyName = "DiffPasswordProtected" },
                    new DataGridViewTextBoxColumn() { HeaderText = "Log Password Protected", DataPropertyName = "LogPasswordProtected" },
                    new DataGridViewTextBoxColumn() { HeaderText = "Full Encrypted", DataPropertyName = "FullEncrypted" },
                    new DataGridViewTextBoxColumn() { HeaderText = "Diff Encrypted", DataPropertyName = "DiffEncrypted" },
                    new DataGridViewTextBoxColumn() { HeaderText = "Log Encrypted", DataPropertyName = "LogEncrypted" },
                    new DataGridViewTextBoxColumn() { HeaderText = "Snapshot Backups", DataPropertyName = "SnapshotBackups" },
                    new DataGridViewTextBoxColumn() { HeaderText = "DB Level Threshold Config", DataPropertyName = "DBThresholdConfiguration" },
                    new DataGridViewTextBoxColumn() { HeaderText = "Full Compression Algorithms", DataPropertyName = "FullCompressionAlgorithms" },
                    new DataGridViewTextBoxColumn() { HeaderText = "Diff Compression Algorithms", DataPropertyName = "DiffCompressionAlgorithms" },
                    new DataGridViewTextBoxColumn() { HeaderText = "Log Compression Algorithms", DataPropertyName = "LogCompressionAlgorithms" },
                    new DataGridViewLinkColumn() { HeaderText = "Configure", Text = "Configure", UseColumnTextForLinkValue = true, SortMode = DataGridViewColumnSortMode.NotSortable, Name = "Configure", LinkColor = DashColors.LinkColor }
                    );
            }
            dgvSummary.ApplyTheme();
            dgvSummary.Columns[0].Frozen = Common.FreezeKeyColumn;
            dgvSummary.DataSource = new DataView(dt);
            splitContainer1.SplitterDistance = (dgvSummary.Rows.Count * 24) + dgvSummary.ColumnHeadersHeight + 24; // Set size based on row count, header size and scrollbar
            dgvSummary.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            UseWaitCursor = false;
        }

        public BackupsControl()
        {
            InitializeComponent();
        }

        private void DgvBackups_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            if (!dgvBackups.Columns.Contains("LastFull")) return;
            for (var idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)dgvBackups.Rows[idx].DataBoundItem;
                var fullBackupStatus = (DBADashStatusEnum)row["FullBackupStatus"];
                var diffBackupStatus = (DBADashStatusEnum)row["DiffBackupStatus"];
                var logBackupStatus = (DBADashStatusEnum)row["LogBackupStatus"];
                var snapshotStatus = (DBADashStatusEnum)row["SnapshotAgeStatus"];
                dgvBackups.Rows[idx].Cells["LastFull"].SetStatusColor(fullBackupStatus);
                dgvBackups.Rows[idx].Cells["LastDiff"].SetStatusColor(diffBackupStatus);
                dgvBackups.Rows[idx].Cells["LastLog"].SetStatusColor(logBackupStatus);
                if ((bool)row["ConsiderPartialBackups"])
                {
                    dgvBackups.Rows[idx].Cells["LastPartial"].SetStatusColor(fullBackupStatus);
                    dgvBackups.Rows[idx].Cells["LastPartialDiff"].SetStatusColor(diffBackupStatus);
                }
                else
                {
                    dgvBackups.Rows[idx].Cells["LastPartial"].SetStatusColor(DBADashStatusEnum.NA);
                    dgvBackups.Rows[idx].Cells["LastPartialDiff"].SetStatusColor(DBADashStatusEnum.NA);
                }
                if ((bool)row["ConsiderFGBackups"])
                {
                    dgvBackups.Rows[idx].Cells["LastFG"].SetStatusColor(fullBackupStatus);
                    dgvBackups.Rows[idx].Cells["LastFGDiff"].SetStatusColor(diffBackupStatus);
                }
                else
                {
                    dgvBackups.Rows[idx].Cells["LastFG"].SetStatusColor(DBADashStatusEnum.NA);
                    dgvBackups.Rows[idx].Cells["LastFGDiff"].SetStatusColor(DBADashStatusEnum.NA);
                }
                dgvBackups.Rows[idx].Cells["SnapshotAge"].SetStatusColor(snapshotStatus);
                dgvBackups.Rows[idx].Cells["Configure"].Style.Font = (string)row["ThresholdsConfiguredLevel"] == "Database" ? new Font(dgvBackups.Font, FontStyle.Bold) : new Font(dgvBackups.Font, FontStyle.Regular);
            }
        }

        private void DgvBackups_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || !dgvBackups.Columns.Contains("Configure")) return;
            var row = (DataRowView)dgvBackups.Rows[e.RowIndex].DataBoundItem;
            if (dgvBackups.Columns[e.ColumnIndex].Name == "Configure")
            {
                ConfigureThresholds((int)row["InstanceID"], (int)row["DatabaseID"]);
            }
            else if (dgvBackups.Columns[e.ColumnIndex].HeaderText == "Database")
            {
                DatabaseID = (int)row["DatabaseID"];
                tsBack.Enabled = true;
                RunRefreshDataLocal();
            }
        }

        private void ConfigureThresholds(int instanceID, int databaseID)
        {
            var frm = new BackupThresholdsConfig
            {
                InstanceID = instanceID,
                DatabaseID = databaseID
            };
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                RunRefreshDataLocal();
            }
        }

        private void ConfigureRootThresholdsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConfigureThresholds(-1, -1);
        }

        private void ConfigureInstanceThresholdsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (InstanceIDs.Count == 1)
            {
                ConfigureThresholds(InstanceIDs[0], -1);
            }
        }

        private void TsFilter_Click(object sender, EventArgs e)
        {
            RunRefreshDataLocal();
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
           RunRefreshDataLocal();
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            dgvSummary.Columns["Configure"]!.Visible = false;
            Common.CopyDataGridViewToClipboard(dgvSummary);
            dgvSummary.Columns["Configure"].Visible = true;
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            dgvSummary.Columns["Configure"]!.Visible = false;
            Common.PromptSaveDataGridView(ref dgvSummary);
            dgvSummary.Columns["Configure"]!.Visible = true;
        }

        private void DgvSummary_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (var idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)dgvSummary.Rows[idx].DataBoundItem;
                var snapshotStatus = (DBADashStatusEnum)row["SnapshotAgeStatus"];

                dgvSummary.Rows[idx].Cells["SnapshotAge"].SetStatusColor(snapshotStatus);

                var okCols = new[] { "FullOK", "DiffOK", "LogOK" };
                foreach (var col in okCols)
                {
                    var value = Convert.ToInt32(dgvSummary.Rows[idx].Cells[col].Value);
                    dgvSummary.Rows[idx].Cells[col].SetStatusColor(value > 0 ? DBADashStatusEnum.OK : DBADashStatusEnum.NA);
                }
                var warningCols = new[] { "FullWarning", "DiffWarning", "LogWarning" };
                foreach (var col in warningCols)
                {
                    var value = Convert.ToInt32(dgvSummary.Rows[idx].Cells[col].Value);
                    dgvSummary.Rows[idx].Cells[col].SetStatusColor(value > 0 ? DBADashStatusEnum.Warning : DBADashStatusEnum.NA);
                }
                var criticalCols = new[] { "FullCritical", "DiffCritical", "LogCritical" };
                foreach (var col in criticalCols)
                {
                    var value = Convert.ToInt32(dgvSummary.Rows[idx].Cells[col].Value);
                    dgvSummary.Rows[idx].Cells[col].SetStatusColor(value > 0 ? DBADashStatusEnum.Critical : DBADashStatusEnum.NA);
                }
                dgvSummary.Rows[idx].Cells["Configure"].Style.Font = Convert.ToBoolean(row["InstanceThresholdConfiguration"]) ? new Font(dgvSummary.Font, FontStyle.Bold) : new Font(dgvSummary.Font, FontStyle.Regular);
            }
        }

        private void DgvSummary_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgvSummary.Columns[e.ColumnIndex].Name == "Instance" && backupInstanceIDs.Count == 0)
            {
                DatabaseID = 0;
                var row = (DataRowView)dgvSummary.Rows[e.RowIndex].DataBoundItem;
                var instanceId = (int)row["InstanceID"];
                backupInstanceIDs = InstanceIDs;
                InstanceIDs = new List<int>() { instanceId };
                IncludeCritical = true;
                IncludeWarning = true;
                IncludeOK = true;
                IncludeNA = true;
                tsBack.Enabled = true;
                var tempContext = (DBADashContext)CurrentContext.Clone();
                tempContext.InstanceID = instanceId;
                tsTrigger.Visible = tempContext.CanMessage;
                RunRefreshDataLocal();
            }
            else if (dgvSummary.Columns[e.ColumnIndex].Name == "Configure")
            {
                var row = (DataRowView)dgvSummary.Rows[e.RowIndex].DataBoundItem;
                ConfigureThresholds((int)row["InstanceID"], -1);
            }
        }

        private void TsBack_Click(object sender, EventArgs e)
        {
            NavigateBack();
        }

        public bool NavigateBack()
        {
            if (!CanNavigateBack) return false;
            
            if (DatabaseID > 0)
            {
                DatabaseID = 0;
            }
            else
            {
                InstanceIDs = backupInstanceIDs;
                backupInstanceIDs = new();
                tsTrigger.Visible = CurrentContext.CanMessage;
            }
            tsBack.Enabled = backupInstanceIDs.Count > 0;
            IncludeCritical = true;
            IncludeWarning = true;
            IncludeOK = InstanceIDs.Count == 1;
            IncludeNA = InstanceIDs.Count == 1;
            RunRefreshDataLocal();
            return true;
        }

        private void TsExcelDetail_Click(object sender, EventArgs e)
        {
            dgvBackups.Columns["Configure"]!.Visible = false;
            Common.PromptSaveDataGridView(ref dgvBackups);
            dgvBackups.Columns["Configure"]!.Visible = true;
        }

        private void TsCopyDetail_Click(object sender, EventArgs e)
        {
            dgvBackups.Columns["Configure"]!.Visible = false;
            Common.CopyDataGridViewToClipboard(dgvBackups);
            dgvBackups.Columns["Configure"].Visible = true;
        }

        private void TsCols_Click(object sender, EventArgs e)
        {
            dgvSummary.PromptColumnSelection();
        }

        private void TsDetailCols_Click(object sender, EventArgs e)
        {
            dgvBackups.PromptColumnSelection();
        }

        public void SetStatus(string message, string tooltip, Color color)
        {
            lblStatus.InvokeSetStatus(message, tooltip, color);
        }

        private async void TsTrigger_Click(object sender, EventArgs e)
        {
            if (InstanceIDs.Count != 1)
            {
                lblStatus.Text = @"Please select a single instance to trigger a collection";
            }
            var instanceId = InstanceIDs[0];
            await CollectionMessaging.TriggerCollection(instanceId, new List<CollectionType>() { CollectionType.Backups, CollectionType.Databases }, this);
        }
    }
}