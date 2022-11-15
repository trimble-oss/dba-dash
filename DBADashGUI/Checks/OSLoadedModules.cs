using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace DBADashGUI.Checks
{
    public partial class OSLoadedModules : UserControl, INavigation
    {
        public OSLoadedModules()
        {
            InitializeComponent();
        }

        public List<int> InstanceIDs { get; set; }
        private int selectedInstanceID = -1;
        private bool HasSelectedInstance { get => selectedInstanceID > 0; }

        public bool CanNavigateBack { get => selectedInstanceID != -1; }

        public void RefreshData()
        {
            selectedInstanceID = InstanceIDs.Count == 1 ? InstanceIDs[0] : -1;
            RefreshDataLocal();
        }

        private void RefreshDataLocal()
        {
            tsBack.Enabled = HasSelectedInstance;
            if (HasSelectedInstance)
            {
                var dt = GetOSLoadedModules();
                LoadDetailCols();
                dgv.DataSource = dt;
                dgv.AutoResizeColumns();
            }
            else
            {
                var dt = GetOSLoadedModuleSummary();
                LoadSummaryCols();
                dgv.DataSource = dt;
                dgv.AutoResizeColumns();
            }
        }

        private void LoadDetailCols()
        {
            dgv.AutoGenerateColumns = false;
            dgv.Columns.Clear();
            dgv.Columns.AddRange(
                new DataGridViewTextBoxColumn() { HeaderText = "Instance", DataPropertyName = "InstanceDisplayName" },
                new DataGridViewTextBoxColumn() { Name = "colStatus", HeaderText = "Status", DataPropertyName = "StatusDescription" },
                new DataGridViewTextBoxColumn() { HeaderText = "Notes", DataPropertyName = "Notes" },
                new DataGridViewTextBoxColumn() { Name = "colBaseAddress", HeaderText = "Base Address", DataPropertyName = "base_address" },
                new DataGridViewTextBoxColumn() { HeaderText = "File Version", DataPropertyName = "file_version" },
                new DataGridViewTextBoxColumn() { HeaderText = "Product Version", DataPropertyName = "product_version" },
                new DataGridViewCheckBoxColumn() { HeaderText = "Debug", DataPropertyName = "debug" },
                new DataGridViewCheckBoxColumn() { HeaderText = "Patched", DataPropertyName = "patched" },
                new DataGridViewCheckBoxColumn() { HeaderText = "Pre Release", DataPropertyName = "prerelease" },
                new DataGridViewCheckBoxColumn() { HeaderText = "Private Build", DataPropertyName = "private_build" },
                new DataGridViewCheckBoxColumn() { HeaderText = "Special Build", DataPropertyName = "special_build" },
                new DataGridViewTextBoxColumn() { HeaderText = "Language", DataPropertyName = "language" },
                new DataGridViewTextBoxColumn() { HeaderText = "Company", DataPropertyName = "company" },
                new DataGridViewTextBoxColumn() { HeaderText = "Description", DataPropertyName = "description" },
                new DataGridViewTextBoxColumn() { HeaderText = "Name", DataPropertyName = "name" }
                );
        }

        private void LoadSummaryCols()
        {
            dgv.AutoGenerateColumns = false;
            dgv.Columns.Clear();
            dgv.Columns.AddRange(
                new DataGridViewLinkColumn() { HeaderText = "Instance", DataPropertyName = "InstanceDisplayName", LinkColor = DashColors.LinkColor },
                new DataGridViewTextBoxColumn() { Name = "colStatus", HeaderText = "Status", DataPropertyName = "StatusDescription" },
                new DataGridViewTextBoxColumn() { HeaderText = "Notes", DataPropertyName = "Notes" }
                );
        }

        private DataTable GetOSLoadedModuleSummary()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.OSLoadedModuleSummary_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
                cmd.Parameters.AddWithValue("InstanceIDs", InstanceIDs.AsDataTable());
                DataTable dt = new();
                da.Fill(dt);
                return dt;
            }
        }

        private DataTable GetOSLoadedModules()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.OSLoadedModules_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
                cmd.Parameters.AddWithValue("InstanceID", selectedInstanceID);
                DataTable dt = new();
                da.Fill(dt);
                return dt;
            }
        }

        private void Dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)dgv.Rows[idx].DataBoundItem;
                var status = (DBADashStatus.DBADashStatusEnum)Convert.ToInt32(row["Status"] == DBNull.Value ? 3 : row["Status"]);
                dgv.Rows[idx].Cells["colStatus"].SetStatusColor(status);
            }
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void Dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

            if (e.RowIndex >= 0 && !HasSelectedInstance)
            {
                DataRowView row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
                selectedInstanceID = (int)row["InstanceID"];
                RefreshDataLocal();
            }
        }

        private void TsBack_Click(object sender, EventArgs e)
        {
            NavigateBack();
        }

        public bool NavigateBack()
        {
            if (CanNavigateBack)
            {
                selectedInstanceID = -1;
                RefreshDataLocal();
                return true;
            }
            else
            {
                return false;
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



        private void TsEdit_Click(object sender, EventArgs e)
        {
            using (var frm = new OSLoadedModuleStatus())
            {
                frm.ShowDialog();
                if (frm.DialogResult == DialogResult.OK)
                {
                    RefreshDataLocal();
                }
            }
        }

        private void Dgv_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgv.Columns.Contains("colBaseAddress") && e.ColumnIndex == dgv.Columns["colBaseAddress"].Index)
            {
                if (e.Value != null && e.Value != DBNull.Value)
                {
                    byte[] array = (byte[])e.Value;
                    e.Value = "0x" + Convert.ToHexString(array);
                    e.FormattingApplied = true;
                }
                else
                    e.FormattingApplied = false;
            }
        }
    }
}
