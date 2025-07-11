﻿using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DBADashGUI.Theme;

namespace DBADashGUI.Checks
{
    public partial class OSLoadedModuleStatus : Form
    {
        public OSLoadedModuleStatus()
        {
            InitializeComponent();
            this.ApplyTheme();
        }

        private DataTable dt;

        private static DataTable GetOSLoadedModulesStatus()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.OSLoadedModulesStatus_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
                DataTable dt = new();
                da.Fill(dt);
                UniqueConstraint uc = new(new[] { dt.Columns["Name"], dt.Columns["Description"], dt.Columns["Company"] });
                dt.Constraints.Add(uc);
                return dt;
            }
        }

        private void OSLoadedModuleStatus_Load(object sender, EventArgs e)
        {
            AddCols();
            RefreshData();
        }

        private void AddCols()
        {
            dgv.AutoGenerateColumns = false;
            dgv.Columns.Clear();
            dgv.Columns.AddRange(
                new DataGridViewTextBoxColumn() { Name = "colName", HeaderText = "Name", DataPropertyName = "Name" },
                new DataGridViewTextBoxColumn() { Name = "colCompany", HeaderText = "Company", DataPropertyName = "Company" },
                new DataGridViewTextBoxColumn() { Name = "colDescription", HeaderText = "Description", DataPropertyName = "Description" },
                new DataGridViewTextBoxColumn() { Name = "colStatus", HeaderText = "Status", DataPropertyName = "Status", ToolTipText = "1=Critical, 2= Warning, 3 = N/A, 4 = OK" },
                new DataGridViewTextBoxColumn() { Name = "colNotes", HeaderText = "Notes", DataPropertyName = "Notes" },
                new DataGridViewCheckBoxColumn() { Name = "colIsSystem", HeaderText = "Is System", DataPropertyName = "IsSystem", ReadOnly = true }
                );
        }

        private void RefreshData()
        {
            dt = GetOSLoadedModulesStatus();
            dgv.DataSource = new DataView(dt, chkSystem.Checked ? "" : "IsSystem=0", "", DataViewRowState.CurrentRows);
        }

        private static void DeleteOSLoadedModulesStatus(int ID)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.OSLoadedModulesStatus_Del", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
                cmd.Parameters.AddWithValue("ID", ID);
                cmd.ExecuteNonQuery();
            }
        }

        private static int AddOSLoadedModulesStatus(string Name, string Company, string Description, short Status, string Notes)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.OSLoadedModulesStatus_Add", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
                cmd.Parameters.AddWithValue("Name", Name);
                cmd.Parameters.AddWithValue("Company", Company);
                cmd.Parameters.AddWithValue("Description", Description);
                cmd.Parameters.AddWithValue("Status", Status);
                cmd.Parameters.AddWithValue("Notes", Notes);
                var pID = new SqlParameter() { ParameterName = "ID", Direction = ParameterDirection.Output, DbType = DbType.Int32 };
                cmd.Parameters.Add(pID);
                cmd.ExecuteNonQuery();
                return Convert.ToInt32(pID.Value);
            }
        }

        private static void UpdateOSLoadedModulesStatus(int ID, string Name, string Company, string Description, short Status, string Notes)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.OSLoadedModulesStatus_Upd", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
                cmd.Parameters.AddWithValue("ID", ID);
                cmd.Parameters.AddWithValue("Name", Name);
                cmd.Parameters.AddWithValue("Company", Company);
                cmd.Parameters.AddWithValue("Description", Description);
                cmd.Parameters.AddWithValue("Status", Status);
                cmd.Parameters.AddWithValue("Notes", Notes);
                cmd.ExecuteNonQuery();
            }
        }

        private static void OSLoadedModulesRefreshStatus()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.OSLoadedModules_RefreshStatus", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private void FormatRow(DataGridViewRow row)
        {
            if (dgv.Columns.Contains("colStatus"))
            {
                var isSystem = Convert.ToBoolean(row.Cells["colIsSystem"].Value);
                var status = (DBADashStatus.DBADashStatusEnum)Convert.ToInt32(row.Cells["colStatus"].Value == DBNull.Value ? 3 : row.Cells["colStatus"].Value);
                row.DefaultCellStyle.BackColor = isSystem ? Color.Gray : Color.White;
                row.Cells["colStatus"].SetStatusColor(status);
                row.ReadOnly = isSystem;
            }
        }

        /// <summary>
        /// Add default values when user is adding a new row.  % will match everything.
        /// </summary>
        private void Dgv_DefaultValuesNeeded(object sender, DataGridViewRowEventArgs e)
        {
            e.Row.Cells["colStatus"].Value = 1;
            e.Row.Cells["colName"].Value = "%";
            e.Row.Cells["colDescription"].Value = "%";
            e.Row.Cells["colCompany"].Value = "%";
            e.Row.Cells["colNotes"].Value = "User Custom";
            e.Row.Cells["colIsSystem"].Value = false;
            e.Row.Cells["colStatus"].SetStatusColor(DBADashStatus.DBADashStatusEnum.Critical);
        }

        /// <summary>
        /// Check that status is between 1 and 4
        /// </summary>
        private void Dgv_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (e.ColumnIndex != dgv.Columns["colStatus"].Index) return;
            try
            {
                var status = Convert.ToInt32(e.FormattedValue);
                if (status is >= 1 and <= 4) return;
                e.Cancel = true;
                ShowStatusValidationError();
            }
            catch
            {
                e.Cancel = true;
                ShowStatusValidationError();
            }
        }

        private static void ShowStatusValidationError()
        {
            MessageBox.Show("Please input values 1..4.\n1=Critical\n2=Warning\n3=N/A\n4=Good", "Status", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        private void Dgv_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            if (e.ColumnIndex == dgv.Columns["colStatus"].Index)
            {
                e.Cancel = true;
                ShowStatusValidationError();
            }
        }

        private void Dgv_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == dgv.Columns["colStatus"].Index)
            {
                FormatRow(dgv.Rows[e.RowIndex]);
            }
        }

        private void BttnSave_Click(object sender, EventArgs e)
        {
            Save();
            DialogResult = DialogResult.OK;
        }

        /// <summary>
        /// Return true if user has made any edits to the grid/table.
        /// </summary>
        private bool HasChanges()
        {
            var dtAll = dt.GetChanges();
            return dtAll is { Rows.Count: > 0 };
        }

        /// <summary>
        /// Save changes and refresh the os loaded module status
        /// </summary>
        private void Save()
        {
            if (HasChanges())
            {
                var dtDeleted = dt.GetChanges(DataRowState.Deleted);
                var dtAdded = dt.GetChanges(DataRowState.Added);
                var dtModified = dt.GetChanges(DataRowState.Modified);
                try
                {
                    if (dtDeleted != null)
                    {
                        foreach (var id in dtDeleted.Rows.Cast<DataRow>().Select(row => Convert.ToInt32(row["ID", DataRowVersion.Original])))
                        {
                            DeleteOSLoadedModulesStatus(id);
                        }
                    }
                    if (dtAdded != null)
                    {
                        foreach (DataRow row in dtAdded.Rows)
                        {
                            var id = AddOSLoadedModulesStatus((string)row["Name"], (string)row["Company"], (string)row["Description"], Convert.ToInt16(row["Status"]), Convert.ToString(row["Notes"]));
                            row["ID"] = id;
                        }
                    }
                    if (dtModified != null)
                    {
                        foreach (DataRow row in dtModified.Rows)
                        {
                            UpdateOSLoadedModulesStatus((int)row["ID"], (string)row["Name"], (string)row["Company"], (string)row["Description"], Convert.ToInt16(row["Status"]), Convert.ToString(row["Notes"]));
                        }
                    }
                }
                catch (Exception ex)
                {
                    CommonShared.ShowExceptionDialog(ex, "Error saving changes");
                }
                try
                {
                    OSLoadedModulesRefreshStatus();
                }
                catch (Exception ex)
                {
                    CommonShared.ShowExceptionDialog(ex, "Error refreshing statuses");
                }
                RefreshData();
            }
            else
            {
                MessageBox.Show("Nothing to update", "Save", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Apply formatting
        /// </summary>
        private void Dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (var idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = dgv.Rows[idx];
                var isSystem = Convert.ToBoolean(row.Cells["colIsSystem"].Value);
                row.ReadOnly = isSystem;
                row.DefaultCellStyle.BackColor = isSystem ? Color.LightGray : Color.White;
                FormatRow(row);
            }
        }

        private void ChkSystem_CheckedChanged(object sender, EventArgs e)
        {
            dgv.DataSource = new DataView(dt, chkSystem.Checked ? "" : "IsSystem=0", "", DataViewRowState.CurrentRows);
        }

        private void BttnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void OSLoadedModuleStatus_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (HasChanges() && e.CloseReason == CloseReason.UserClosing)
            {
                if (MessageBox.Show("You have unsaved changes.  Are you sure you want to exit?", "Exit", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                {
                    e.Cancel = true;
                }
            }
        }

        private void Dgv_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            if (Convert.ToBoolean(e.Row.Cells["colIsSystem"].Value))
            {
                MessageBox.Show("Can't delete system row", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}