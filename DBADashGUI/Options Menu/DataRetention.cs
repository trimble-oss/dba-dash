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
            // The table a linked table's retention follows - see dbo.DataRetentionLink.  Shown after the retention it explains.
            dgv.Columns.Insert(dgv.Columns["colRetentionDays"].Index + 1, new DataGridViewTextBoxColumn
            {
                Name = "colRetentionLinkedTo",
                DataPropertyName = "RetentionLinkedTo",
                HeaderText = "Same As",
                ReadOnly = true
            });
            dgv.DataBindingComplete += Dgv_DataBindingComplete;
            this.ApplyTheme();
        }

        private static bool IsLinked(DataGridViewRow row) => row.Cells["colRetentionLinkedTo"].Value is string;

        /// <summary>Retention is set on tables with a row in dbo.DataRetention.  A linked table follows another table's
        /// retention, and a table without a row (shown with Show All Tables) isn't meant to have one.</summary>
        private static bool IsConfigurable(DataGridViewRow row) => !IsLinked(row) && row.Cells["colRetentionDays"].Value is int;

        /// <summary>Rows whose retention can't be set here aren't shown as a link to edit, and are greyed out</summary>
        private void Dgv_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (IsConfigurable(row)) continue;
                var linkedTo = row.Cells["colRetentionLinkedTo"].Value as string;
                row.Cells["colRetentionDays"] = new DataGridViewTextBoxCell
                {
                    Value = row.Cells["colRetentionDays"].Value,
                    ToolTipText = linkedTo != null
                        ? $"Same as {linkedTo}.  Set the retention for {linkedTo} instead."
                        : "Data retention isn't managed for this table."
                };
                row.Cells["colRetentionDays"].ReadOnly = true;
                // Muted text rather than the theme's Disabled colours, which are a solid fill for status cells.  Gray5 reads as muted on the light and dark grid backgrounds.
                row.DefaultCellStyle.ForeColor = DashColors.Gray5;
            }
        }

        private void DataRetention_Load(object sender, EventArgs e)
        {
            try
            {
                RefreshData();
            }
            catch (SqlException ex) when (ex.Number == 262)
            {
                CommonShared.ShowExceptionDialog(ex, "Insufficient permissions", default,
                    TaskDialogIcon.ShieldWarningYellowBar);
                Close();
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
            if (e.RowIndex < 0 || dgv.Columns[e.ColumnIndex].Name != "colRetentionDays" || !IsConfigurable(dgv.Rows[e.RowIndex])) return;
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
                    CommonShared.ShowExceptionDialog(ex);
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
            using SqlCommand cmd = new("dbo.PurgeData", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = Config.DefaultCommandTimeout };
            cmd.Parameters.AddWithValue("Force", true);
            cmd.ExecuteNonQuery();
        }

        private void TsPurge_Click(object sender, EventArgs e)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                Purge();
                RefreshData();
                Cursor = Cursors.Default;
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                CommonShared.ShowExceptionDialog(ex);
            }
        }
    }
}