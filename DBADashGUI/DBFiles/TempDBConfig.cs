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
    public partial class TempDBConfig : UserControl, ISetContext, IRefreshData
    {
        public TempDBConfig()
        {
            InitializeComponent();
        }

        private List<int> InstanceIDs;

        public void SetContext(DBADashContext context)
        {
            InstanceIDs = context.RegularInstanceIDs.ToList();
            RefreshData();
        }

        public void RefreshData()
        {
            dgvTempDB.Columns[0].Frozen = Common.FreezeKeyColumn;
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.TempDBConfig_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
                cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
                cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);

                DataTable dt = new();
                da.Fill(dt);
                dgvTempDB.AutoGenerateColumns = false;
                dgvTempDB.DataSource = dt;
                dgvTempDB.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            }
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgvTempDB);
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void DgvTempDB_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (int idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var r = dgvTempDB.Rows[idx];
                var row = (DataRowView)r.DataBoundItem;
                var insufficientFiles = (bool)row["InsufficientFiles"];
                var evenSized = (bool)row["IsEvenlySized"];
                var evenGrowth = (bool)row["IsEvenGrowth"];
                var traceFlagReq = (bool)row["IsTraceFlagRequired"];
                var T1118 = (bool)row["T1118"];
                var T1117 = (bool)row["T1117"];
                var insufficientFilesStatus = insufficientFiles ? DBADashStatusEnum.Warning : DBADashStatusEnum.NA;
                dgvTempDB.Rows[idx].Cells[colNumberOfDataFiles.Index].SetStatusColor(insufficientFilesStatus);
                dgvTempDB.Rows[idx].Cells[colInsufficientFiles.Index].SetStatusColor(insufficientFilesStatus);
                dgvTempDB.Rows[idx].Cells[colEvenSize.Index].SetStatusColor(evenSized ? DBADashStatusEnum.OK : DBADashStatusEnum.Warning);
                dgvTempDB.Rows[idx].Cells[colEvenGrowth.Index].SetStatusColor(evenGrowth ? DBADashStatusEnum.OK : DBADashStatusEnum.Warning);
                dgvTempDB.Rows[idx].Cells[colT1117.Index].SetStatusColor(traceFlagReq ? (T1117 ? DBADashStatusEnum.OK : DBADashStatusEnum.Warning) : (T1117 ? DBADashStatusEnum.WarningLow : DBADashStatusEnum.NA));
                dgvTempDB.Rows[idx].Cells[colT1118.Index].SetStatusColor(traceFlagReq ? (T1118 ? DBADashStatusEnum.OK : DBADashStatusEnum.Warning) : (T1118 ? DBADashStatusEnum.WarningLow : DBADashStatusEnum.NA));
                var memoryOptimizedStatus = row["IsTempDBMetadataMemoryOptimized"] == DBNull.Value ? DBADashStatusEnum.NA : ((bool)row["IsTempDBMetadataMemoryOptimized"] ? DBADashStatusEnum.OK : DBADashStatusEnum.Information);
                var logFilesStatus = (int)row["NumberOfLogFiles"] > 1 ? DBADashStatusEnum.Warning : DBADashStatusEnum.OK;
                dgvTempDB.Rows[idx].Cells[colTempDBMemoryOpt.Index].SetStatusColor(memoryOptimizedStatus);
                dgvTempDB.Rows[idx].Cells[colNumberOfLogFiles.Index].SetStatusColor(logFilesStatus);
            }
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgvTempDB);
        }
    }
}