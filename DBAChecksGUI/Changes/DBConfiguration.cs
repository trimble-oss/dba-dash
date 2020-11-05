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

namespace DBAChecksGUI.Changes
{
    public partial class DBConfiguration : UserControl
    {
        public DBConfiguration()
        {
            InitializeComponent();
        }

        public List<Int32> InstanceIDs;


        public void RefreshData()
        {
            refreshConfig();
            refreshHistory();
        }

        private void refreshConfig()
        {
            dgvConfig.Columns.Clear();

            dgvConfig.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Instance", HeaderText = "Instance" });
            dgvConfig.Columns.Add(new DataGridViewTextBoxColumn() { Name = "DB", HeaderText = "Database" });
            SqlConnection cn = new SqlConnection(Common.ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.DBConfiguration_Get", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
                cmd.Parameters.AddWithValue("@ConfiguredOnly", configuredOnlyToolStripMenuItem.Checked);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
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
                        row.Cells[0].Value = (string)r["Instance"];
                        row.Cells[1].Value = (string)r["DB"];
                        rows.Add(row);
                    }

                    string configName = (string)r["name"];
                    var idx = dgvConfig.Columns[configName].Index;
                    row.Cells[idx].Value = r["value"];
                    row.Cells[idx].Style.BackColor = (bool)r["IsDefault"] ? Color.MintCream : Color.BlanchedAlmond;
                    if (!(bool)r["IsDefault"])
                    {
                        row.Cells[idx].Style.Font = new Font(dgvConfig.Font, FontStyle.Bold);
                    }
                    lastDB = dbid;
                }
                dgvConfig.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
                dgvConfig.Rows.AddRange(rows.ToArray());
            }
        }

        private void refreshHistory()
        {
            SqlConnection cn = new SqlConnection(Common.ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.DBConfigurationHistory_Get", cn);
                cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                dgvConfigHistory.AutoGenerateColumns = false;
                Common.ConvertUTCToLocal(ref dt);
                dgvConfigHistory.DataSource = dt;

            }
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
    }
}
