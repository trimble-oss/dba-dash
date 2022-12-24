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
    public partial class DBFilesControl : UserControl, ISetContext
    {
        public DBFilesControl()
        {
            InitializeComponent();
        }

        private List<Int32> InstanceIDs;
        private Int32? DatabaseID;

        public bool IncludeCritical
        {
            get => criticalToolStripMenuItem.Checked; set => criticalToolStripMenuItem.Checked = value;
        }

        public bool IncludeWarning
        {
            get => warningToolStripMenuItem.Checked; set => warningToolStripMenuItem.Checked = value;
        }

        public bool IncludeNA
        {
            get => undefinedToolStripMenuItem.Checked; set => undefinedToolStripMenuItem.Checked = value;
        }

        public bool IncludeOK
        {
            get => OKToolStripMenuItem.Checked; set => OKToolStripMenuItem.Checked = value;
        }

        public List<short> FileTypes
        {
            get
            {
                var selected = new List<short>();
                foreach (ToolStripMenuItem itm in tsType.DropDownItems)
                {
                    if (itm.Checked)
                    {
                        selected.Add(Convert.ToInt16(itm.Tag));
                    }
                }
                return selected;
            }
        }

        private DataTable GetDBFiles()
        {
            var selectedTypes = FileTypes;
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.DBFiles_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
                cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                if (DatabaseID != null) { cmd.Parameters.AddWithValue("DatabaseID", DatabaseID); }
                cmd.Parameters.AddWithValue("IncludeNA", IncludeNA);
                cmd.Parameters.AddWithValue("IncludeOK", IncludeOK);
                cmd.Parameters.AddWithValue("IncludeWarning", IncludeWarning);
                cmd.Parameters.AddWithValue("IncludeCritical", IncludeCritical);
                cmd.Parameters.AddWithValue("FilegroupLevel", tsFilegroup.Checked);
                cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);
                if (selectedTypes.Count is > 0 and < 4)
                {
                    cmd.Parameters.AddWithValue("Types", string.Join(",", selectedTypes));
                }

                DataTable dt = new();
                da.Fill(dt);
                return dt;
            }
        }

        public void SetContext(DBADashContext context)
        {
            InstanceIDs = context.InstanceIDs.ToList();
            DatabaseID = (context.DatabaseID > 0 ? (Int32?)context.DatabaseID : null);
            IncludeCritical = true;
            IncludeWarning = true;
            IncludeNA = DatabaseID != null;
            IncludeOK = DatabaseID != null;

            RefreshData();
        }

        public void RefreshData()
        {
            var dt = GetDBFiles();
            dgvFiles.AutoGenerateColumns = false;
            dgvFiles.DataSource = new DataView(dt);
            dgvFiles.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);

            configureInstanceThresholdsToolStripMenuItem.Enabled = InstanceIDs.Count == 1;
            configureDatabaseThresholdsToolStripMenuItem.Enabled = InstanceIDs.Count == 1 && DatabaseID > 0;
        }

        private void Status_Selected(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void DgvFiles_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var row = (DataRowView)dgvFiles.Rows[e.RowIndex].DataBoundItem;
                if (dgvFiles.Columns[e.ColumnIndex].HeaderText == "Configure")
                {
                    ConfigureThresholds((Int32)row["InstanceID"], (Int32)row["DatabaseID"], (Int32)row["data_space_id"]);
                }
                else if (dgvFiles.Columns[e.ColumnIndex].HeaderText == "History")
                {
                    var frm = new DBSpaceHistoryView
                    {
                        DatabaseID = (Int32)row["DatabaseID"],
                        DataSpaceID = row["data_space_id"] == DBNull.Value ? null : (Int32?)row["data_space_id"],
                        InstanceGroupName = (string)row["InstanceGroupName"],
                        DBName = (string)row["name"],
                        FileName = row["file_name"] == DBNull.Value ? null : (string)row["file_name"]
                    };
                    frm.Show();
                }
            }
        }

        public void ConfigureThresholds(Int32 InstanceID, Int32 DatabaseID, Int32 DataSpaceID)
        {
            var frm = new FileThresholdConfig
            {
                InstanceID = InstanceID,
                DataSpaceID = DataSpaceID,
                DatabaseID = DatabaseID
            };
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                RefreshData();
            }
        }

        private void DgvFiles_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)dgvFiles.Rows[idx].DataBoundItem;
                var Status = (DBADashStatus.DBADashStatusEnum)row["FreeSpaceStatus"];
                var snapshotStatus = (DBADashStatus.DBADashStatusEnum)row["FileSnapshotAgeStatus"];
                var maxSizeStatus = (DBADashStatus.DBADashStatusEnum)row["PctMaxSizeStatus"];
                var fgAutogrowStatus = (DBADashStatus.DBADashStatusEnum)row["FilegroupAutogrowStatus"];
                string checkType = row["FreeSpaceCheckType"] == DBNull.Value ? "-" : (string)row["FreeSpaceCheckType"];
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
                ConfigureThresholds(InstanceIDs[0], (Int32)DatabaseID, -1);
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
            Common.CopyDataGridViewToClipboard(dgvFiles);
            Configure.Visible = true;
            History.Visible = true;
        }

        private void TsFilegroup_Click(object sender, EventArgs e)
        {
            ToggleFileLevel(false);
            RefreshData();
        }

        private void TsFile_Click(object sender, EventArgs e)
        {
            ToggleFileLevel(true);
            RefreshData();
        }

        private void ToggleFileLevel(bool isFileLevel)
        {
            tsFilegroup.Checked = !isFileLevel;
            tsFile.Checked = isFileLevel;
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
            if (selectedTypes.Count is > 0 and < 4)
            {
                tsType.Font = new Font(tsType.Font, FontStyle.Bold);
            }
            else
            {
                tsType.Font = new Font(tsType.Font, FontStyle.Regular);
            }
            foreach (ToolStripMenuItem itm in tsType.DropDownItems)
            {
                if (itm.Checked)
                {
                    itm.Font = new Font(itm.Font, FontStyle.Bold);
                }
                else
                {
                    itm.Font = new Font(itm.Font, FontStyle.Regular);
                }
            }
            RefreshData();
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgvFiles);
        }

        private void DBFilesControl_Load(object sender, EventArgs e)
        {
            Common.StyleGrid(ref dgvFiles);
        }
    }
}