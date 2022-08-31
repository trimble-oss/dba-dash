using Microsoft.Data.SqlClient;
using Octokit;
using SpreadsheetLight.Drawing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.Checks
{
    public partial class IdentityColumns : UserControl
    {
        public IdentityColumns()
        {
            InitializeComponent();
        }

        public List<int> InstanceIDs { get; set; }
        public int DatabaseID { get; set; }

        public bool IncludeOK { get => oKToolStripMenuItem.Checked; set => oKToolStripMenuItem.Checked = value; }
        public bool IncludeNA { get => undefinedToolStripMenuItem.Checked; set => undefinedToolStripMenuItem.Checked = value; }
        public bool IncludeWarning { get => warningToolStripMenuItem.Checked; set => warningToolStripMenuItem.Checked = value; }
        public bool IncludeCritical { get => criticalToolStripMenuItem.Checked; set => criticalToolStripMenuItem.Checked = value; }

        public void RefreshData()
        {
            if (dgv.Columns.Count == 0)
            {
                AddColsToDGV();
            }
            var dt = GetIdentityColumns();
            dgv.DataSource = dt;
            dgv.Sort(dgv.Columns["colPctUsed"], ListSortDirection.Descending);
            dgv.AutoResizeColumns();
        }

        private void AddColsToDGV()
        {
            dgv.AutoGenerateColumns = false;
            dgv.Columns.Clear();
            dgv.Columns.AddRange(
                new DataGridViewTextBoxColumn() { HeaderText = "Instance", DataPropertyName = "InstanceDisplayName" },
                new DataGridViewTextBoxColumn() { HeaderText = "DB", DataPropertyName = "DB" },
                new DataGridViewTextBoxColumn() { HeaderText = "Table", DataPropertyName = "object_name" },
                new DataGridViewTextBoxColumn() { HeaderText = "Column", DataPropertyName = "column_name" },
                new DataGridViewTextBoxColumn() { HeaderText = "Type", DataPropertyName = "type", ToolTipText="Data type for identity value" },
                new DataGridViewTextBoxColumn() { HeaderText = "Min Identity", DataPropertyName = "min_ident", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, Visible=false, ToolTipText="Minimum identity value supported by the column type." },
                new DataGridViewTextBoxColumn() { HeaderText = "Max Identity", DataPropertyName = "max_ident", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits,Visible=false, ToolTipText = "Maximum identity value supported by the column type." },
                new DataGridViewTextBoxColumn() { HeaderText = "Max Rows", DataPropertyName = "max_rows", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits,Visible=false, ToolTipText="Maximum rows possible with unique identity values." },
                new DataGridViewTextBoxColumn() { HeaderText = "Increment", DataPropertyName = "increment_value", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits , Visible = false,ToolTipText="Identity increment value" },
                new DataGridViewTextBoxColumn() { HeaderText = "Seed", DataPropertyName = "seed_value", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, Visible = false, ToolTipText= "Identity seed value" },
                new DataGridViewTextBoxColumn() { HeaderText = "Rows", DataPropertyName = "row_count", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, ToolTipText="Number of rows in table" },
                new DataGridViewTextBoxColumn() { HeaderText = "Last Identity", DataPropertyName = "last_value", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, ToolTipText="Last identity value used" },
                new DataGridViewTextBoxColumn() { Name = "colPctUsed", HeaderText = "% Used", DataPropertyName = "pct_used", DefaultCellStyle = Common.DataGridViewPercentCellStyle, ToolTipText= "Percent used. The greatest value of % Ident Used and % Rows Used." },
                new DataGridViewTextBoxColumn() { Name = "colPctFree", HeaderText = "% Free", DataPropertyName = "pct_free", DefaultCellStyle = Common.DataGridViewPercentCellStyle, ToolTipText= "Percent free.  Calculated from % used." },
                new DataGridViewTextBoxColumn() { HeaderText = "% Ident Used", DataPropertyName = "pct_ident_used", DefaultCellStyle = Common.DataGridViewPercentCellStyle, ToolTipText= "Percent of identity values used. Calculated as last identity value as a % of max identity value.  If last identity value is negative it's a % of max rows" },
                new DataGridViewTextBoxColumn() { HeaderText = "% Rows Used", DataPropertyName = "pct_rows_used", DefaultCellStyle = Common.DataGridViewPercentCellStyle, ToolTipText="Percentage of rows used.  Calculated as row count as a % of max rows" },
                new DataGridViewTextBoxColumn() { HeaderText = "Ident Remaining", DataPropertyName = "remaining_ident_count", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, ToolTipText= "Number of identity values remaining based on max identity value and last identity value." },
                new DataGridViewTextBoxColumn() { HeaderText = "Rows Remaining", DataPropertyName = "remaining_row_count", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, ToolTipText="Number of rows remaining based on Max Rows and Rows." },
                new DataGridViewTextBoxColumn() { HeaderText = "Avg Ident/day", DataPropertyName = "avg_ident_per_day", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, ToolTipText="Avg identity values used per day over the last ~month (Calc Days)" },
                new DataGridViewTextBoxColumn() { HeaderText = "Avg Rows/day", DataPropertyName = "avg_rows_per_day", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, ToolTipText= "Avg rows added to the table per day of the last ~month (Calc Days)" },
                new DataGridViewTextBoxColumn() { HeaderText = "Avg Calc Days", DataPropertyName = "avg_calc_days", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, Visible= false, ToolTipText= "Number of days Avg Ident/day and Avg Rows/day have been calculated over" },
                new DataGridViewTextBoxColumn() { HeaderText = "Estimated Days", DataPropertyName = "estimated_days", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, ToolTipText="Estimated days remaining until table runs out of identity values" },
                new DataGridViewTextBoxColumn() { HeaderText = "Estimated Date", DataPropertyName = "estimated_date", DefaultCellStyle = new DataGridViewCellStyle() { Format = "d" }, ToolTipText = "Estimated date table will run out of identity values"  },
                new DataGridViewTextBoxColumn() { HeaderText = "Ident Estimated Days", DataPropertyName = "ident_estimated_days", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits,Visible=false, ToolTipText = "Estimated days remaining until table runs out of identity values.  Based on Ident Remaining and Avg Ident/day" },
                new DataGridViewTextBoxColumn() { HeaderText = "Row Estimated Days", DataPropertyName = "row_estimated_days", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, Visible = false, ToolTipText = "Estimated days remaining until table runs out of identity values.  Based on Rows Remaining and Avg Rows/day" },
                new DataGridViewTextBoxColumn() { Name="colSnapshotDate", HeaderText = "Snapshot Date", DataPropertyName = "SnapshotDate", ToolTipText = "Date identity data was collected from the SQL instance"}
                );
        }

        private DataTable GetIdentityColumns()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("IdentityColumns_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceIDs", InstanceIDs.AsDataTable());
                if (DatabaseID > 0)
                {
                    cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
                }
                cmd.Parameters.AddWithValue("IncludeWarning", warningToolStripMenuItem.Checked);
                cmd.Parameters.AddWithValue("IncludeCritical", criticalToolStripMenuItem.Checked);
                cmd.Parameters.AddWithValue("IncludeOK", oKToolStripMenuItem.Checked);
                cmd.Parameters.AddWithValue("IncludeNA",undefinedToolStripMenuItem.Checked);
                DataTable dt = new();
                da.Fill(dt);
                Common.ConvertUTCToLocal(ref dt);
                return dt;
            }
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void TsColumns_Click(object sender, EventArgs e)
        {
            using (var frm = new SelectColumns() { Columns = dgv.Columns })
            {
                frm.ShowDialog(this);
            }
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgv);
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgv);
        }

        private void Dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)dgv.Rows[idx].DataBoundItem;
                var SnapshotStatus = (DBADashStatus.DBADashStatusEnum)row["SnapshotStatus"];
                var IdentityStatus = (DBADashStatus.DBADashStatusEnum)row["IdentityStatus"];
                dgv.Rows[idx].Cells["colSnapshotDate"].SetStatusColor(SnapshotStatus);
                dgv.Rows[idx].Cells["colPctUsed"].SetStatusColor(IdentityStatus);
                dgv.Rows[idx].Cells["colPctFree"].SetStatusColor(IdentityStatus);

            }
        }

        private void Filter_Click(object sender, EventArgs e)
        {
            RefreshData();
        }
    }
}
