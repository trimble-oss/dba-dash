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

namespace DBADashGUI.Changes
{
    public partial class Configuration : UserControl
    {
        public Configuration()
        {
            InitializeComponent();
        }

        public string ConnectionString;
        public List<Int32> InstanceIDs;

        public void RefreshData()
        {
            dgvConfig.Columns.Clear();
            dgvConfig.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Instance", HeaderText = "Instance"});

            using (var cn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.Configuration_Get", cn) { CommandType = CommandType.StoredProcedure })
                {
                    cn.Open();
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
                    string lastInstance = "";
                    List<DataGridViewRow> rows = new List<DataGridViewRow>();
                    DataGridViewRow row = null;
                    foreach (DataRow r in dt.Rows)
                    {
                        string instance = (string)r["ConnectionID"];
                        if (instance != lastInstance)
                        {
                            row = new DataGridViewRow();
                            row.CreateCells(dgvConfig);
                            row.Cells[0].Value = instance;
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
                        lastInstance = instance;
                    }
                    dgvConfig.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
                    dgvConfig.Rows.AddRange(rows.ToArray());
                }
            }
        }



        private void configuredOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void tsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void tsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgvConfig);
        }
    }
}
