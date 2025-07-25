using DBADash;
using DBADashGUI.Interface;
using DBADashGUI.Messaging;
using DBADashGUI.Theme;
using Humanizer;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static DBADashGUI.DBADashStatus;

namespace DBADashGUI.DBFiles
{
    public partial class DBFilesControl : UserControl, ISetContext, ISetStatus
    {
        public DBFilesControl()
        {
            InitializeComponent();
            dgvFiles.RegisterClearFilter(tsClearFilter);
            dgvFiles.GridFilterChanged += (sender, e) => UpdateTotals();
        }

        public bool FileLevel
        {
            get => tsFile.Checked;
            set
            {
                tsFile.Checked = value;
                tsFilegroup.Checked = !value;
            }
        }

        private const int RefreshOnReloadThreshold = 20; // minutes
        private List<int> InstanceIDs => CurrentContext?.InstanceIDs.ToList();
        private int? DatabaseID => (CurrentContext?.DatabaseID > 0 ? CurrentContext?.DatabaseID : null);
        private string DriveName => CurrentContext?.DriveName;
        private DBADashContext CurrentContext;
        private DateTime LastRefresh = DateTime.MinValue;

        public bool IncludeCritical
        {
            get => statusFilterToolStrip1.Critical; set => statusFilterToolStrip1.Critical = value;
        }

        public bool IncludeWarning
        {
            get => statusFilterToolStrip1.Warning; set => statusFilterToolStrip1.Warning = value;
        }

        public bool IncludeNA
        {
            get => statusFilterToolStrip1.NA; set => statusFilterToolStrip1.NA = value;
        }

        public bool IncludeOK
        {
            get => statusFilterToolStrip1.OK; set => statusFilterToolStrip1.OK = value;
        }

        public List<short> FileTypes => (from ToolStripMenuItem itm in tsType.DropDownItems where itm.Checked select Convert.ToInt16(itm.Tag)).ToList();

        public bool IsFileGroupLevel => tsFilegroup.Checked && tsLevel.Visible;

        public string GridFilter
        {
            get => dgvFiles.RowFilter;
            set => dgvFiles.SetFilter(value);
        }

        private DataTable GetDBFiles()
        {
            var selectedTypes = FileTypes;
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.DBFiles_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cn.Open();
            cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
            if (DatabaseID != null) { cmd.Parameters.AddWithValue("DatabaseID", DatabaseID); }
            cmd.Parameters.AddRange(statusFilterToolStrip1.GetSQLParams());
            cmd.Parameters.AddWithValue("FilegroupLevel", IsFileGroupLevel);
            cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);
            cmd.Parameters.AddWithNullableValue("DriveName", DriveName);
            if (selectedTypes.Count is > 0 and < 4)
            {
                cmd.Parameters.AddWithValue("Types", string.Join(",", selectedTypes));
            }

            DataTable dt = new();
            da.Fill(dt);
            return dt;
        }

        public void SetContext(DBADashContext _context, bool resetFilters, bool refreshData)
        {
            SetStatus(string.Empty, string.Empty, ThemeExtensions.CurrentTheme.ForegroundColor);
            if (resetFilters)
            {
                dgvFiles.SetFilter(string.Empty);
                IncludeCritical = true;
                IncludeWarning = true;
                IncludeNA = DatabaseID != null || _context.DriveName != null;
                IncludeOK = DatabaseID != null || _context.DriveName != null;
                tsLevel.Visible = _context.DriveName == null;
            }
            CurrentContext = _context;
            tsTrigger.Visible = _context.CanMessage;
            if (refreshData)
            {
                RefreshData();
            }
        }

        public void SetContext(DBADashContext _context)
        {
            if (_context == CurrentContext)
            {
                if (DateTime.UtcNow.Subtract(LastRefresh).TotalMinutes > RefreshOnReloadThreshold)
                {
                    RefreshData();
                }
                return;
            }
            SetContext(_context, true, true);
        }

        public void RefreshData()
        {
            var filter = dgvFiles.RowFilter;
            ToggleFileLevel(!IsFileGroupLevel);
            var dt = GetDBFiles();
            Invoke(() =>
            {
                dgvFiles.AutoGenerateColumns = false;
                dgvFiles.DataSource = new DataView(dt);
                dgvFiles.ReplaceSpaceWithNewLineInHeaderTextToImproveColumnAutoSizing();
                dgvFiles.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);

                configureInstanceThresholdsToolStripMenuItem.Enabled = InstanceIDs.Count == 1;
                configureDatabaseThresholdsToolStripMenuItem.Enabled = InstanceIDs.Count == 1 && DatabaseID > 0;
                if (!string.IsNullOrEmpty(filter))
                {
                    dgvFiles.SetFilter(filter);
                }
                UpdateTotals();
                LastRefresh = DateTime.UtcNow;
            });
        }

        public void SetStatus(string message, string tooltip, Color color)
        {
            tsSep.Visible = !string.IsNullOrEmpty(message);
            lblInfo.InvokeSetStatus(message, tooltip, color);
        }

        private void UpdateTotals()
        {
            double totalSize;
            int totalFiles;
            if (IsFileGroupLevel)
            {
                totalFiles = dgvFiles.Rows
                    .Cast<DataGridViewRow>()
                    .Where(row => !row.IsNewRow && row.Cells["NumberOfFiles"].Value != null)
                    .Sum(row => int.Parse(row.Cells["NumberOfFiles"].Value.ToString()!));

                totalSize = dgvFiles.Rows
                    .Cast<DataGridViewRow>()
                    .Where(row => !row.IsNewRow && row.Cells["SizeMB"].Value != null)
                    .Sum(row => Convert.ToDouble(row.Cells["SizeMB"].Value.ToString()));
            }
            else
            {
                totalFiles = dgvFiles.RowCount;
                totalSize = dgvFiles.Rows
                    .Cast<DataGridViewRow>()
                    .Where(row => !row.IsNewRow && row.Cells["FileLevel_FileSizeMB"].Value != null)
                    .Sum(row => Convert.ToDouble(row.Cells["FileLevel_FileSizeMB"].Value.ToString()));
            }
            lblStats.InvokeSetStatus($"File Count {totalFiles}. Total Size: {totalSize.Megabytes()}", "", ThemeExtensions.CurrentTheme.ForegroundColor);
        }

        private void Status_Selected(object sender, EventArgs e)
        {
            RefreshData();
        }

        private static DBSpaceHistoryView DBSpaceHistoryViewForm;

        private static void LoadDBSpaceHistory(int dbid, int? dbSpaceID, string instance, string dbName, string fileName)
        {
            DBSpaceHistoryViewForm?.Close();
            DBSpaceHistoryViewForm = new()
            {
                DatabaseID = dbid,
                DataSpaceID = dbSpaceID,
                InstanceGroupName = instance,
                DBName = dbName,
                FileName = fileName
            };
            DBSpaceHistoryViewForm.FormClosed += delegate { DBSpaceHistoryViewForm = null; };
            DBSpaceHistoryViewForm.Show();
        }

        private void DgvFiles_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = (DataRowView)dgvFiles.Rows[e.RowIndex].DataBoundItem;
            switch (dgvFiles.Columns[e.ColumnIndex].HeaderText)
            {
                case "Configure":
                    ConfigureThresholds((int)row["InstanceID"], (int)row["DatabaseID"], (int)row["data_space_id"]);
                    break;

                case "History":
                    LoadDBSpaceHistory((int)row["DatabaseID"], row["data_space_id"] == DBNull.Value ? null : (int?)row["data_space_id"], (string)row["InstanceGroupName"], (string)row["name"], row["file_name"] == DBNull.Value ? null : (string)row["file_name"]);
                    break;
            }
        }

        public void ConfigureThresholds(int instanceID, int databaseID, int dataSpaceID)
        {
            var frm = new FileThresholdConfig
            {
                InstanceID = instanceID,
                DataSpaceID = dataSpaceID,
                DatabaseID = databaseID
            };
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                RefreshData();
            }
        }

        private void DgvFiles_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (var idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)dgvFiles.Rows[idx].DataBoundItem;
                var Status = (DBADashStatusEnum)row["FreeSpaceStatus"];
                var snapshotStatus = (DBADashStatusEnum)row["FileSnapshotAgeStatus"];
                var maxSizeStatus = (DBADashStatusEnum)row["PctMaxSizeStatus"];
                var fgAutogrowStatus = (DBADashStatusEnum)row["FilegroupAutogrowStatus"];
                var checkType = row["FreeSpaceCheckType"] == DBNull.Value ? "-" : (string)row["FreeSpaceCheckType"];
                dgvFiles.Rows[idx].Cells["FileSnapshotAge"].SetStatusColor(snapshotStatus);
                dgvFiles.Rows[idx].Cells["FilegroupPctMaxSize"].SetStatusColor(maxSizeStatus);
                dgvFiles.Rows[idx].Cells["FilegroupAutogrow"].SetStatusColor(fgAutogrowStatus);
                dgvFiles.Rows[idx].Cells["FilegroupPctFree"].SetStatusColor(checkType == "%" ? Status : DBADashStatusEnum.NA);
                dgvFiles.Rows[idx].Cells["FilegroupFreeMB"].SetStatusColor(checkType == "M" ? Status : DBADashStatusEnum.NA);

                if (row["ConfiguredLevel"] != DBNull.Value && (string)row["ConfiguredLevel"] == "FG")
                {
                    dgvFiles.Rows[idx].Cells["Configure"].Style.Font = new Font(dgvFiles.Font, FontStyle.Bold);
                }
                else if (row["ConfiguredLevel"] != DBNull.Value && (string)row["ConfiguredLevel"] == "DB" && (int)row["data_space_id"] == 0)
                {
                    dgvFiles.Rows[idx].Cells["Configure"].Style.Font = new Font(dgvFiles.Font, FontStyle.Bold);
                }
                else
                {
                    dgvFiles.Rows[idx].Cells["Configure"].Style.Font = new Font(dgvFiles.Font, FontStyle.Regular);
                }
            }
        }

        private void ConfigureDatabaseThresholdsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (InstanceIDs.Count == 1 && DatabaseID > 0)
            {
                ConfigureThresholds(InstanceIDs[0], (int)DatabaseID, -1);
            }
        }

        private void ConfigureInstanceThresholdsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (InstanceIDs.Count == 1)
            {
                ConfigureThresholds(InstanceIDs[0], -1, -1);
            }
        }

        private void ConfigureRootThresholdsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConfigureThresholds(-1, -1, -1);
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            Configure.Visible = false;
            History.Visible = false;
            dgvFiles.CopyGrid();
            Configure.Visible = true;
            History.Visible = true;
        }

        private void TsFilegroup_Click(object sender, EventArgs e)
        {
            tsFilegroup.Checked = true;
            tsFile.Checked = false;
            RefreshData();
        }

        private void TsFile_Click(object sender, EventArgs e)
        {
            tsFilegroup.Checked = false;
            tsFile.Checked = true;
            RefreshData();
        }

        private void ToggleFileLevel(bool isFileLevel)
        {
            foreach (DataGridViewColumn col in dgvFiles.Columns)
            {
                if (col.Name.StartsWith("FileLevel_"))
                {
                    col.Visible = isFileLevel;
                }
            }
        }

        private void TsTypes_Click(object sender, EventArgs e)
        {
            var selectedTypes = FileTypes;
            tsType.Font = selectedTypes.Count is > 0 and < 4 ? new Font(tsType.Font, FontStyle.Bold) : new Font(tsType.Font, FontStyle.Regular);
            foreach (ToolStripMenuItem itm in tsType.DropDownItems)
            {
                itm.Font = itm.Checked ? new Font(itm.Font, FontStyle.Bold) : new Font(itm.Font, FontStyle.Regular);
            }
            RefreshData();
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            dgvFiles.ExportToExcel();
        }

        private async void TsTrigger_Click(object sender, EventArgs e)
        {
            await CollectionMessaging.TriggerCollection(CurrentContext.InstanceID, CollectionType.DBFiles, this);
        }
    }
}