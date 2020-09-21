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

            dgvConfig.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Instance", HeaderText = "Instance", DataPropertyName = "Instance" });
            SqlConnection cn = new SqlConnection(ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.Configuration_Get", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
                cmd.Parameters.AddWithValue("@ConfiguredOnly", configuredOnlyToolStripMenuItem.Checked);
                SqlDataReader rdr = cmd.ExecuteReader();
                var dt = new DataTable();
                dt.Columns.Add("Instance");

                string instance = "";
                string previousInstance = "";
                DataRow r = null;
                while (rdr.Read())
                {
                    instance = (string)rdr["ConnectionID"];
                    if (instance != previousInstance)
                    {
                        r = dt.NewRow();
                        dt.Rows.Add(r);
                        r["Instance"] = instance;
                    }
                    var configid = (Int32)rdr["configuration_id"];
                    var configName = (string)rdr["name"];
                    var colName = configid.ToString();
                    if (!dt.Columns.Contains(colName))
                    {
                        DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn() { Name = colName, HeaderText = configName, DataPropertyName = colName };
                        col.Tag = colName + "_IsDefault";
                        dgvConfig.Columns.Add(col);
                        dt.Columns.Add(colName);

                        var c = new DataColumn(col.Name + "_IsDefault") { DataType = typeof(bool) };
                        dt.Columns.Add(c);

                    }
                    r[colName] = rdr["value"];
                    r[colName + "_IsDefault"] = (bool)rdr["IsDefault"];
                                    
                    previousInstance = instance;
                }
                dgvConfig.AutoGenerateColumns = false;
                dgvConfig.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
                dgvConfig.DataSource = dt;
            }
        }

        private void dgvConfig_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)dgvConfig.Rows[idx].DataBoundItem;
                foreach(DataGridViewTextBoxColumn col in dgvConfig.Columns)
                {
                    if(col.Tag != null)
                    {
                        var isDefaultCol = (string)col.Tag;
                        if (row[isDefaultCol] == DBNull.Value)
                        {
                            dgvConfig.Rows[idx].Cells[col.Index].Style.BackColor = Color.Gainsboro;
                            dgvConfig.Rows[idx].Cells[col.Index].Style.Font = new Font(dgvConfig.Font, FontStyle.Regular);
                        }
                        else if ((bool)row[isDefaultCol] == true)
                        {
                          
                            dgvConfig.Rows[idx].Cells[col.Index].Style.BackColor = Color.MintCream;
                            dgvConfig.Rows[idx].Cells[col.Index].Style.Font = new Font(dgvConfig.Font, FontStyle.Regular);
                        }
                        else
                        {
                            dgvConfig.Rows[idx].Cells[col.Index].Style.Font = new Font(dgvConfig.Font, FontStyle.Bold);
                            dgvConfig.Rows[idx].Cells[col.Index].Style.BackColor = Color.BlanchedAlmond;
                        }
                    }
                }
                
            }
        }

        private void configuredOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshData();
        }
    }
}
