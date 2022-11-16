using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Changes
{
    public partial class AzureServiceObjectivesHistory : UserControl, ISetContext
    {
        public AzureServiceObjectivesHistory()
        {
            InitializeComponent();
        }

        private List<Int32> InstanceIDs;

        public void SetContext(DBADashContext context)
        {
            InstanceIDs = context.AzureInstanceIDs.ToList();
            RefreshDB();
            RefreshPool();
        }

        private void RefreshDB()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            {
                using (SqlCommand cmd = new("dbo.AzureServiceObjectivesHistory_Get", cn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                    SqlDataAdapter da = new(cmd);
                    DataTable dt = new();
                    da.Fill(dt);
                    Common.ConvertUTCToLocal(ref dt);
                    dgv.AutoGenerateColumns = false;
                    dgv.DataSource = dt;
                    dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                }
            }
        }

        private void RefreshPool()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            {
                using (SqlCommand cmd = new("dbo.AzureDBElasticPoolHistory_Get", cn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                    SqlDataAdapter da = new(cmd);
                    DataTable dt = new();
                    da.Fill(dt);
                    Common.ConvertUTCToLocal(ref dt);
                    dgvPool.AutoGenerateColumns = false;
                    dgvPool.DataSource = dt;
                    dgvPool.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                }
            }
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshDB();
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgv);
        }

        private void TsRefreshPool_Click(object sender, EventArgs e)
        {
            RefreshPool();
        }

        private void TsCopyPool_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgvPool);
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgv);
        }

        private void TsPoolExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgvPool);
        }
    }
}
