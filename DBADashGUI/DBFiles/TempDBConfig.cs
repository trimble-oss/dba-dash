using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
namespace DBADashGUI.DBFiles
{
    public partial class TempDBConfig : UserControl
    {
        public TempDBConfig()
        {
            InitializeComponent();
        }
        public List<Int32> InstanceIDs;

        public void RefreshData()
        {
            dgvTempDB.Columns[0].Frozen = Common.FreezeKeyColumn;
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.TempDBConfig_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
                cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));

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
            for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var r = dgvTempDB.Rows[idx];
                var row = (DataRowView)r.DataBoundItem;
                var insufficientFiles = (bool)row["InsufficientFiles"];
                var evenSized = (bool)row["IsEvenlySized"];
                var evenGrowth = (bool)row["IsEvenGrowth"];
                var traceFlagReq = (bool)row["IsTraceFlagRequired"];
                var T1118 = (bool)row["T1118"];
                var T1117 = (bool)row["T1117"];
                Color insufficientFilesColor = insufficientFiles ? DashColors.Warning : Color.White;
                dgvTempDB.Rows[idx].Cells[colNumberOfDataFiles.Index].SetStatusColor(insufficientFilesColor);
                dgvTempDB.Rows[idx].Cells[colInsufficientFiles.Index].SetStatusColor(insufficientFilesColor);
                dgvTempDB.Rows[idx].Cells[colEvenSize.Index].SetStatusColor(evenSized ? DashColors.Success : DashColors.Warning);
                dgvTempDB.Rows[idx].Cells[colEvenGrowth.Index].SetStatusColor(evenGrowth ? DashColors.Success : DashColors.Warning);
                dgvTempDB.Rows[idx].Cells[colT1117.Index].SetStatusColor(traceFlagReq ? (T1117 ? DashColors.Success : DashColors.Warning) : (T1117 ? DashColors.YellowLight : DashColors.GrayLight));
                dgvTempDB.Rows[idx].Cells[colT1118.Index].SetStatusColor(traceFlagReq ? (T1118 ? DashColors.Success : DashColors.Warning) : (T1118 ? DashColors.YellowLight : DashColors.GrayLight));
                Color memoryOptimizedColor = row["IsTempDBMetadataMemoryOptimized"] == DBNull.Value ? DashColors.GrayLight : ((bool)row["IsTempDBMetadataMemoryOptimized"] ? DashColors.Success : DashColors.BlueLight);
                Color logFilesColor = (Int32)row["NumberOfLogFiles"] > 1 ? DashColors.Warning : DashColors.Success;
                dgvTempDB.Rows[idx].Cells[colTempDBMemoryOpt.Index].SetStatusColor(memoryOptimizedColor);
                dgvTempDB.Rows[idx].Cells[colNumberOfLogFiles.Index].SetStatusColor(logFilesColor);

            }
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgvTempDB);
        }
    }
}
