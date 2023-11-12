using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Windows.Forms;
using DBADashGUI.Theme;

namespace DBADashGUI
{
    public partial class DataRetention : Form
    {
        public DataRetention()
        {
            InitializeComponent();
            this.ApplyTheme();
        }

        private void DataRetention_Load(object sender, EventArgs e)
        {
            try
            {
                RefreshData();
            }
            catch (SqlException ex) when (ex.Number == 262)
            {
                MessageBox.Show("Insufficient permissions", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                this.Close();
            }
        }

        private void RefreshData()
        {
            var dt = GetDataRetention(showAllTablesToolStripMenuItem.Checked);
            dgv.DataSource = dt;
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private static DataTable GetDataRetention(bool allTables)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.DataRetention_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cn.Open();
            cmd.Parameters.AddWithValue("AllTables", allTables);
            DataTable dt = new();
            da.Fill(dt);
            return dt;
        }

        private void ShowAllTablesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void Dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || dgv.Columns[e.ColumnIndex].Name != "colRetentionDays") return;
            var days = (int)dgv[e.ColumnIndex, e.RowIndex].Value;
            var sDays = days.ToString();
            var tableName = (string)dgv["colTableName", e.RowIndex].Value;
            var schema = (string)dgv["colSchema", e.RowIndex].Value;
            if (CommonShared.ShowInputDialog(ref sDays, "Enter number of days") != DialogResult.OK) return;
            if (int.TryParse(sDays, out var newRetention))
            {
                if (newRetention == days) return;
                try
                {
                    UpdateRetention(tableName, schema, newRetention);
                    dgv[e.ColumnIndex, e.RowIndex].Value = newRetention;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Please enter an integer number of days for retention", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void UpdateRetention(string table, string schema, int days)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using SqlCommand cmd = new("dbo.DataRetention_Upd", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
            cmd.Parameters.AddWithValue("TableName", table);
            cmd.Parameters.AddWithValue("SchemaName", schema);
            cmd.Parameters.AddWithValue("RetentionDays", days);
            cmd.ExecuteNonQuery();
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgv);
        }

        private static void Purge()
        {
            using SqlConnection cn = new(Common.ConnectionString);
            cn.Open();
            SqlCommand cmd = new("dbo.PurgeData", cn);
            cmd.ExecuteNonQuery();
        }

        private void TsPurge_Click(object sender, EventArgs e)
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;
                Purge();
                RefreshData();
                this.Cursor = Cursors.Default;
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}