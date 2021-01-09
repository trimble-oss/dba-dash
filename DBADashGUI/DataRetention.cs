using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
namespace DBADashGUI
{
    public partial class DataRetention : Form
    {
        public DataRetention()
        {
            InitializeComponent();
        }

        public string ConnectionString;

        private void DataRetention_Load(object sender, EventArgs e)
        {
            refreshData();
        }

        private void refreshData()
        {
            var dt = getDataRetention(showAllTablesToolStripMenuItem.Checked);
            dgv.DataSource = dt;
        }

        private DataTable getDataRetention(bool allTables)
        {
            var cn = new SqlConnection(ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.DataRetention_Get", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("AllTables", allTables);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
           

        }

        private void showAllTablesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            refreshData();
        }

        private void dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if(e.RowIndex>=0 && dgv.Columns[e.ColumnIndex].Name == "colRetentionDays")
            {
                Int32 days = (Int32)dgv[e.ColumnIndex, e.RowIndex].Value;
                string sDays = days.ToString();
                string tableName = (string)dgv["colTableName", e.RowIndex].Value;
                if (Common.ShowInputDialog(ref sDays, "Enter number of days") == DialogResult.OK) {
                    
                    if (Int32.TryParse(sDays, out int newRetention))
                    {
                        if (newRetention != days)
                        {
                            try
                            {
                                updateRetention(tableName, newRetention);
                                dgv[e.ColumnIndex, e.RowIndex].Value = newRetention;
                            }
                            catch(Exception ex)
                            {
                                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Please enter an integer number of days for retention", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    

                }              
            }
        }

        private void updateRetention(string table, int days)
        {
            SqlConnection cn = new SqlConnection(ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.DataRetention_Upd", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("TableName", table);
                cmd.Parameters.AddWithValue("RetentionDays", days);
                cmd.ExecuteNonQuery();
            }
        }

        private void tsRefresh_Click(object sender, EventArgs e)
        {
            refreshData();
        }

        private void tsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgv);
        }

        private void purge()
        {
            SqlConnection cn = new SqlConnection(ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.PurgeData", cn);
                cmd.ExecuteNonQuery();
            }
        }

        private void tsPurge_Click(object sender, EventArgs e)
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;
                purge();
                refreshData();
                this.Cursor = Cursors.Default;
            }
            catch(Exception ex)
            {
                this.Cursor = Cursors.Default;
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
