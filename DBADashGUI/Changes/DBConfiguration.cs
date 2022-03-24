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

namespace DBADashGUI.Changes
{
    public partial class DBConfiguration : UserControl
    {
        public DBConfiguration()
        {
            InitializeComponent();
        }

        public List<Int32> InstanceIDs;
        public Int32 DatabaseID=-1;


        public void RefreshData()
        {
            refreshConfig();
            refreshHistory();
        }

        private DataTable getDBConfiguration()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.DBConfiguration_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
                cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
                cmd.Parameters.AddWithValue("@ConfiguredOnly", configuredOnlyToolStripMenuItem.Checked);
                if (DatabaseID > 0)
                {
                    cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
                }

                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
         }

        private void refreshConfig()
        {
            dgvConfig.Columns.Clear();

            dgvConfig.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Instance", HeaderText = "Instance" });
            dgvConfig.Columns.Add(new DataGridViewTextBoxColumn() { Name = "DB", HeaderText = "Database" });
         
            DataTable dt =getDBConfiguration();
                   
            foreach (DataRow r in dt.DefaultView.ToTable(true, "name").Rows)
            {
                DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn() { HeaderText = (string)r["name"], Name = (string)r["name"] };
                dgvConfig.Columns.Add(col);
            }
            Int32 lastDB = -1;
            List<DataGridViewRow> rows = new List<DataGridViewRow>();
            DataGridViewRow row = null;
            foreach (DataRow r in dt.Rows)
            {
                Int32 dbid = (Int32)r["DatabaseID"];
                if (dbid != lastDB)
                {
                    row = new DataGridViewRow();
                    row.CreateCells(dgvConfig);
                    row.Cells[0].Value = (string)r["InstanceGroupName"];
                    row.Cells[1].Value = (string)r["DB"];
                    rows.Add(row);
                }

                string configName = (string)r["name"];
                var idx = dgvConfig.Columns[configName].Index;
                row.Cells[idx].Value = r["value"];
                row.Cells[idx].SetStatusColor((bool)r["IsDefault"] ? DashColors.GreenPale : DashColors.YellowPale);
                if (!(bool)r["IsDefault"])
                {
                    row.Cells[idx].Style.Font = new Font(dgvConfig.Font, FontStyle.Bold);
                }
                lastDB = dbid;
            }
            dgvConfig.Rows.AddRange(rows.ToArray());
            dgvConfig.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
       
        }

        private DataTable getDBConfigurationHistory()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.DBConfigurationHistory_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
                cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                if (DatabaseID > 0)
                {
                    cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
                }
                
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }           
        }

        private void refreshHistory()
        {   
            DataTable dt = getDBConfigurationHistory();
            dgvConfigHistory.AutoGenerateColumns = false;
            Common.ConvertUTCToLocal(ref dt);
            dgvConfigHistory.DataSource = dt;
            dgvConfigHistory.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);      
        }

        private void configuredOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            refreshConfig();
        }

        private void tsRefresh_Click(object sender, EventArgs e)
        {
            refreshConfig();
        }

        private void tsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgvConfig);
        }

        private void tsRefreshHistory_Click(object sender, EventArgs e)
        {
            refreshHistory();
        }

        private void tsCopyHistory_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgvConfigHistory);
        }

        private void tsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgvConfig);
        }

        private void tsExcelHistory_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgvConfigHistory);
        }

        private void tsCols_Click(object sender, EventArgs e)
        {
            using (var frm = new SelectColumns() { Columns = dgvConfig.Columns })
            {
                frm.ShowDialog(this);
            }
        }
    }
}
