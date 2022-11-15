using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace DBADashGUI
{
    public partial class SQLPatching : UserControl
    {
        public SQLPatching()
        {
            InitializeComponent();
        }

        public List<Int32> InstanceIDs;

        public void RefreshData()
        {
            dgvVersion.Columns[0].Frozen = Common.FreezeKeyColumn;
            dgv.Columns[0].Frozen = Common.FreezeKeyColumn;
            RefreshHistory();
            RefreshVersion();
        }

        private void RefreshVersion()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (SqlCommand cmd = new("dbo.InstanceVersionInfo_Get", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
                cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
                SqlDataAdapter da = new(cmd);
                DataTable dt = new();
                da.Fill(dt);
                Common.ConvertUTCToLocal(ref dt);
                dgvVersion.AutoGenerateColumns = false;
                dgvVersion.DataSource = dt;
                dgvVersion.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            }
        }

        private void RefreshHistory()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.SQLPatching_Get", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
                cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
                SqlDataAdapter da = new(cmd);
                DataTable dt = new();
                da.Fill(dt);
                Common.ConvertUTCToLocal(ref dt);
                dgv.AutoGenerateColumns = false;
                dgv.DataSource = dt;
                dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            }
        }

        private void TsCopyVersion_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgvVersion);
        }

        private void TsCopyHistory_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgv);
        }

        private void TsRefreshVersion_Click(object sender, EventArgs e)
        {
            RefreshVersion();
        }

        private void TsRefreshHistory_Click(object sender, EventArgs e)
        {
            RefreshHistory();
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgvVersion);
        }

        private void TsExcelHistory_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgv);
        }

        private void TsCols_Click(object sender, EventArgs e)
        {
            using (var frm = new SelectColumns() { Columns = dgvVersion.Columns })
            {
                frm.ShowDialog(this);
            }
        }
    }
}
