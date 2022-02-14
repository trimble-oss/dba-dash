using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace DBADashGUI
{
    public partial class SQLPatching : UserControl
    {
        public SQLPatching()
        {
            InitializeComponent();
        }

        public string ConnectionString;
        public List<Int32> InstanceIDs;

        public void RefreshData()
        {
            refreshHistory();
            refreshVersion();
        }

        private void refreshVersion()
        {
            using (var cn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.InstanceVersionInfo_Get", cn) { CommandType = CommandType.StoredProcedure })
                {
                    cn.Open();
                    cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    Common.ConvertUTCToLocal(ref dt);
                    dgvVersion.AutoGenerateColumns = false;
                    dgvVersion.DataSource = dt;
                    dgvVersion.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                }
            }
        }

        private void refreshHistory()
        {
            SqlConnection cn = new SqlConnection(ConnectionString);
            using (cn)
            {
                using (var cmd = new SqlCommand("dbo.SQLPatching_Get", cn) { CommandType = CommandType.StoredProcedure })
                {
                    cn.Open();
                    cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    Common.ConvertUTCToLocal(ref dt);
                    dgv.AutoGenerateColumns = false;
                    dgv.DataSource = dt;
                    dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                }
            }
        }

        private void tsCopyVersion_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgvVersion);
        }

        private void tsCopyHistory_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgv);
        }

        private void tsRefreshVersion_Click(object sender, EventArgs e)
        {
            refreshVersion();
        }

        private void tsRefreshHistory_Click(object sender, EventArgs e)
        {
            refreshHistory();
        }

        private void tsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgvVersion);
        }

        private void tsExcelHistory_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgv);
        }

        private void tsCols_Click(object sender, EventArgs e)
        {
            using (var frm = new SelectColumns() { Columns = dgvVersion.Columns })
            {
                frm.ShowDialog(this);
            }
        }
    }
}
