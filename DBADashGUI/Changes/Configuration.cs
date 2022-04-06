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
    public partial class Configuration : UserControl
    {
        public Configuration()
        {
            InitializeComponent();
        }

        public List<Int32> InstanceIDs;

        private DataTable getConfiguration()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.Configuration_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
                cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
                cmd.Parameters.AddWithValue("@ConfiguredOnly", configuredOnlyToolStripMenuItem.Checked);
               
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        public void RefreshData()
        {
            dgvConfig.Columns.Clear();
            dgvConfig.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Instance", HeaderText = "Instance"});
    
            DataTable dt = getConfiguration();
                    
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
                string displayName = (string)r["InstanceDisplayName"];
                if (instance != lastInstance)
                {
                    row = new DataGridViewRow();
                    row.CreateCells(dgvConfig);
                    row.Cells[0].Value = displayName;
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
                lastInstance = instance;
            }                   
            dgvConfig.Rows.AddRange(rows.ToArray());
            dgvConfig.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                
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

        private void tsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgvConfig);
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
