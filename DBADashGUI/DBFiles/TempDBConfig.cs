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
            using (var cn = new SqlConnection(Common.ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.TempDBConfig_Get", cn) { CommandType = CommandType.StoredProcedure })
                {
                    cn.Open();
                    cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgvTempDB.AutoGenerateColumns = false;
                    dgvTempDB.DataSource = dt;
                }
            }
        }

        private void tsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgvTempDB);
        }

        private void tsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void dgvTempDB_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
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
                dgvTempDB.Rows[idx].Cells[colNumberOfDataFiles.Index].Style.BackColor = insufficientFiles ? Color.Yellow : Color.White;
                dgvTempDB.Rows[idx].Cells[colInsufficientFiles.Index].Style.BackColor = insufficientFiles ? Color.Yellow : Color.White;
                dgvTempDB.Rows[idx].Cells[colEvenSize.Index].Style.BackColor = evenSized ? Color.Green : Color.Yellow;
                dgvTempDB.Rows[idx].Cells[colEvenGrowth.Index].Style.BackColor = evenGrowth ? Color.Green : Color.Yellow;
                dgvTempDB.Rows[idx].Cells[colT1117.Index].Style.BackColor = traceFlagReq ? (T1117 ? Color.Green : Color.Yellow) : (T1117 ? Color.LightYellow : Color.LightGray);
                dgvTempDB.Rows[idx].Cells[colT1118.Index].Style.BackColor = traceFlagReq ? (T1118 ? Color.Green : Color.Yellow) : (T1118 ? Color.LightYellow : Color.LightGray);
                dgvTempDB.Rows[idx].Cells[colTempDBMemoryOpt.Index].Style.BackColor = row["IsTempDBMetadataMemoryOptimized"] == DBNull.Value ? Color.LightGray : ((bool)row["IsTempDBMetadataMemoryOptimized"] ? Color.Green : Color.LightBlue);
                dgvTempDB.Rows[idx].Cells[colNumberOfLogFiles.Index].Style.BackColor = (Int32)row["NumberOfLogFiles"] > 1 ? Color.Yellow : Color.Green;
            }
        }

        private void tsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgvTempDB);
        }
    }
}
