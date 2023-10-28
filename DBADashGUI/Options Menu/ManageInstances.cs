using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Text;
using System.Windows.Forms;
using DBADashGUI.Theme;

namespace DBADashGUI
{
    public partial class ManageInstances : Form
    {
        public ManageInstances()
        {
            InitializeComponent();
            this.ApplyTheme();
            tsFilterError.ForeColor = DashColors.Fail;
        }

        public string Tags { get; set; } = null;
        private bool activeFlagChanged = false;
        private bool summaryVisibleChanged = false;
        public bool InstanceActiveFlagChanged { get => activeFlagChanged; }

        public bool InstanceSummaryVisibleChanged { get => summaryVisibleChanged; }

        private void ManageInstances_Load(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void RefreshData()
        {
            var dt = CommonData.GetInstances(Tags, null);
            dgv.AutoGenerateColumns = false;
            dgv.DataSource = new DataView(dt);
        }

        private void Dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (int idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var r = dgv.Rows[idx];
                var row = (DataRowView)r.DataBoundItem;
                if (row != null)
                {
                    bool isActive = (bool)row["IsActive"];
                    dgv.Rows[idx].Cells[colDeleteRestore.Index].Value = isActive ? "Mark Deleted" : "Restore";
                }
            }
        }

        private static void MarkInstanceDeleted(int InstanceID, bool IsActive)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.Instance_Del", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("IsActive", IsActive);
                cmd.ExecuteNonQuery();
            }
        }

        private static void UpdateShowInSummary(int InstanceID, bool ShowInSummary)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.Instance_ShowInSummary_Upd", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("ShowInSummary", ShowInSummary);
                cmd.ExecuteNonQuery();
            }
        }

        private void Dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == colDeleteRestore.Index)
            {
                var row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
                var InstanceID = (int)row["InstanceID"];
                var isActive = (bool)row["IsActive"];
                isActive = !isActive;
                try
                {
                    MarkInstanceDeleted(InstanceID, isActive);
                    dgv.Rows[e.RowIndex].Cells[colDeleteRestore.Index].Value = isActive ? "Mark Deleted" : "Restore";
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    RefreshData();
                }
                activeFlagChanged = true;
            }
            else if (e.RowIndex >= 0 && e.ColumnIndex == colHidden.Index)
            {
                dgv.CommitEdit(DataGridViewDataErrorContexts.Commit); // Trigger cell value to change so we can process the update
            }
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            timer1.Stop();
            timer1.Start();
        }

        private void SetFilter()
        {
            try
            {
                StringBuilder sbFilter = new();
                if (txtSearch.Text.Trim().Length > 0)
                {
                    sbFilter.AppendFormat(" AND (ConnectionID LIKE '*{0}*' OR InstanceDisplayName LIKE '*{0}*')", txtSearch.Text.Replace("'", "''"));
                }
                if (showActiveToolStripMenuItem.Checked != showDeletedToolStripMenuItem.Checked)
                {
                    sbFilter.AppendFormat(" AND IsActive = {0}", showActiveToolStripMenuItem.Checked ? 1 : 0);
                }
                else if (!showActiveToolStripMenuItem.Checked && !showDeletedToolStripMenuItem.Checked)
                {
                    sbFilter.AppendFormat(" AND 1=0"); // Not showing active or deleted items.  Return no rows
                }
                if (!showAzureToolStripMenuItem.Checked)
                {
                    sbFilter.AppendFormat(" AND IsAzure=0");
                }

                if (sbFilter.Length > 0)
                {
                    sbFilter.Remove(0, 5); // Remove AND
                }

                ((DataView)dgv.DataSource).RowFilter = sbFilter.ToString();
                tsFilterError.Visible = false;
            }
            catch (Exception ex)
            {
                tsFilterError.Text = "Filter error:" + ex.Message;
                tsFilterError.Visible = true;
            }
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();
            SetFilter();
        }

        private void Dgv_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == colHidden.Index)
            {
                try
                {
                    var row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
                    var InstanceID = (int)row["InstanceID"];
                    var showInSummary = !(bool)row["IsHidden"];

                    UpdateShowInSummary(InstanceID, showInSummary);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    RefreshData();
                }
                summaryVisibleChanged = true;
            }
        }

        private void Filter_Click(object sender, EventArgs e)
        {
            SetFilter();
        }
    }
}