﻿using Humanizer;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Changes
{
    public partial class DBOptions : UserControl, ISetContext, INavigation
    {
        private List<int> InstanceIDs;
        private int DatabaseID;
        private string InstanceGroupName;
        private string RowFilter;

        public DBOptions()
        {
            InitializeComponent();
        }

        private static readonly DataGridViewColumn[] SummaryCols =
        {           new DataGridViewLinkColumn(){ Name="Instance", HeaderText="Instance", DataPropertyName="Instance", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor},
                    new DataGridViewLinkColumn(){ Name="Page Verify Not Optimal", HeaderText="Page Verify Not Optimal", DataPropertyName="Page Verify Not Optimal", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor},
                    new DataGridViewLinkColumn(){ Name="Auto Close", HeaderText="Auto Close", DataPropertyName="Auto Close", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor},
                    new DataGridViewLinkColumn(){ Name="Auto Shrink", HeaderText="Auto Shrink", DataPropertyName="Auto Shrink", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor},
                    new DataGridViewLinkColumn(){ Name="Auto Create Stats Disabled", HeaderText="Auto Create Stats Disabled", DataPropertyName="Auto Create Stats Disabled", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor},
                    new DataGridViewLinkColumn(){ Name="Auto Update Stats Disabled", HeaderText="Auto Update Stats Disabled", DataPropertyName="Auto Update Stats Disabled", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor},
                    new DataGridViewLinkColumn(){ Name="Old Compat Level", HeaderText="Old Compat Level", DataPropertyName="Old Compat Level", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor},
                    new DataGridViewLinkColumn(){ Name="Trustworthy", HeaderText="Trustworthy", DataPropertyName="Trustworthy", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor},
                    new DataGridViewLinkColumn(){ Name="In Recovery", HeaderText="In Recovery", DataPropertyName="In Recovery", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor},
                    new DataGridViewLinkColumn(){ Name="Suspect", HeaderText="Suspect", DataPropertyName="Suspect", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor},
                    new DataGridViewLinkColumn(){ Name="Emergency", HeaderText="Emergency", DataPropertyName="Emergency", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor},
                    new DataGridViewLinkColumn(){ Name="Offline", HeaderText="Offline", DataPropertyName="Offline", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor},
                    new DataGridViewLinkColumn(){ Name="Max VLF Count", HeaderText="Max VLF Count", DataPropertyName="Max VLF Count", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor},
                    new DataGridViewLinkColumn(){ Name="Not Using Indirect Checkpoints", HeaderText="Not Using Indirect Checkpoints", DataPropertyName="Not Using Indirect Checkpoints", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor},
                    new DataGridViewLinkColumn(){ Name="None-Default Target Recovery Time", HeaderText="None-Default Target Recovery Time", DataPropertyName="None-Default Target Recovery Time", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor},
                    new DataGridViewLinkColumn(){ Name="User Database Count", HeaderText="User Database Count", DataPropertyName="User Database Count", SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor},
        };

        public bool SummaryMode
        {
            get => DatabaseID <= 0 && tsDetail.Visible;
            set
            {
                tsDetail.Visible = value;
                tsSummary.Visible = !value;
            }
        }

        public void SetContext(DBADashContext context)
        {
            InstanceIDs = context.InstanceIDs.ToList();
            DatabaseID = context.DatabaseID;
            RowFilter = string.Empty;
            InstanceGroupName = string.Empty;
            RefreshData();
        }

        private void RefreshData()
        {
            if (InstanceIDs == null) return;
            RefreshHistory();
            if (SummaryMode)
            {
                RefreshDBSummary();
            }
            else
            {
                RefreshDBInfo();
            }
        }

        private void Pivot(ref DataTable dt)
        {
            var pivotDT = new DataTable();
            pivotDT.Columns.Add("Setting");
            pivotDT.Columns.Add("Value");
            foreach (DataColumn col in dt.Columns)
            {
                if (col.ColumnName is not "InstanceID" and not "DatabaseID")
                {
                    var r = pivotDT.NewRow();
                    r[0] = col.ColumnName;
                    r[1] = dt.Rows[0][col];
                    pivotDT.Rows.Add(r);
                }
            }
            dgv.DataSource = pivotDT;
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private void RefreshDBSummary()
        {
            tsClearFilter.Visible = false;
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.DBSummary_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);
                DataTable dt = new();
                da.Fill(dt);
                dgv.DataSource = null;
                dgv.AutoGenerateColumns = false;
                dgv.Columns.Clear();
                dgv.Columns.AddRange(SummaryCols);
                dgv.DataSource = dt;
                dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            }
            dgv.Columns[0].Frozen = Common.FreezeKeyColumn;
        }

        public DataTable GetDBInfo()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.DatabasesAllInfo_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                cmd.Parameters.AddIfGreaterThanZero("DatabaseID", DatabaseID);
                cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);
                cmd.Parameters.AddStringIfNotNullOrEmpty("InstanceGroupName", InstanceGroupName);
                DataTable dt = new();
                da.Fill(dt);
                return dt;
            }
        }

        private void RefreshDBInfo()
        {
            DataTable dt = GetDBInfo();
            tsClearFilter.Visible = true;
            tsClearFilter.Enabled = RowFilter != string.Empty;
            if (dt.Rows.Count == 1)
            {
                Pivot(ref dt);
            }
            else
            {
                dgv.Columns.Clear();
                dgv.AutoGenerateColumns = true;
                var dv = new DataView(dt, RowFilter, string.Empty, DataViewRowState.CurrentRows);
                dgv.DataSource = dv;
                dgv.Columns["InstanceID"].Visible = false;
                dgv.Columns["DatabaseID"].Visible = false;
                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    col.HeaderText = col.HeaderText.Titleize();
                    col.SortMode = DataGridViewColumnSortMode.Automatic;
                }
                dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                dgv.Columns[0].Frozen = Common.FreezeKeyColumn;
                dgv.Columns[1].Frozen = Common.FreezeKeyColumn; //hidden
                dgv.Columns[2].Frozen = Common.FreezeKeyColumn; //hidden
                dgv.Columns[3].Frozen = Common.FreezeKeyColumn;
            }
        }

        private void RefreshHistory()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.DBOptionsHistory_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                cmd.Parameters.AddIfGreaterThanZero("DatabaseID", DatabaseID);
                cmd.Parameters.AddWithValue("ExcludeStateChanges", excludeStateChangesToolStripMenuItem.Checked);
                cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);
                DataTable dt = new();
                da.Fill(dt);
                DateHelper.ConvertUTCToAppTimeZone(ref dt);
                foreach (DataRow r in dt.Rows)
                {
                    if (r["OldValue"].GetType() == typeof(byte[]))
                    {
                        r["OldValue"] = Common.ByteArrayToString((byte[])r["OldValue"]);
                    }
                    if (r["NewValue"].GetType() == typeof(byte[]))
                    {
                        r["NewValue"] = Common.ByteArrayToString((byte[])r["NewValue"]);
                    }
                }
                dgvHistory.AutoGenerateColumns = false;
                dgvHistory.DataSource = dt;
                dgvHistory.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            }
        }

        private void TsCopyHistory_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgvHistory);
        }

        private void TsRefreshHistory_Click(object sender, EventArgs e)
        {
            RefreshHistory();
        }

        private void TsRefreshInfo_Click(object sender, EventArgs e)
        {
            if (SummaryMode)
            {
                RefreshDBSummary();
            }
            else
            {
                RefreshDBInfo();
            }
        }

        private void TsCopyInfo_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgv);
        }

        private void ExcludeStateChangesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshHistory();
        }

        private void TsSummary_Click(object sender, EventArgs e)
        {
            ShowSummary();
        }

        private void ShowSummary()
        {
            dgv.DataSource = null;
            SummaryMode = true;
            RefreshDBSummary();
        }

        private void TsDetail_Click(object sender, EventArgs e)
        {
            RowFilter = string.Empty;
            SummaryMode = false;
            RefreshDBInfo();
        }

        private void DBOptions_Load(object sender, EventArgs e)
        {
            if (DatabaseID > 0)
            {
                SummaryMode = false;
            }
        }

        private void Dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            if (!SummaryMode) return;
            var warningCols = new string[] { "Auto Create Stats Disabled", "Auto Update Stats Disabled", "Old Compat Level", "In Recovery", "Offline", "Trustworthy", "Not Using Indirect Checkpoints", "None-Default Target Recovery Time" };
            var criticalCols = new string[] { "Page Verify Not Optimal", "Auto Close", "Auto Shrink", "Suspect", "Emergency" };
            for (int idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var r = dgv.Rows[idx];

                foreach (var col in warningCols)
                {
                    if (r.Cells[col].Value == DBNull.Value)
                    {
                        r.Cells[col].SetStatusColor(DashColors.NotApplicable);
                    }
                    else
                    {
                        r.Cells[col].SetStatusColor((int)r.Cells[col].Value > 0 ? DashColors.Warning : DashColors.Success);
                    }
                }
                foreach (var col in criticalCols)
                {
                    if (r.Cells[col].Value == DBNull.Value)
                    {
                        r.Cells[col].SetStatusColor(DashColors.NotApplicable);
                    }
                    else
                    {
                        r.Cells[col].SetStatusColor((int)r.Cells[col].Value > 0 ? DashColors.Fail : DashColors.Success);
                    }
                }
                Color vlfStatusColor = DashColors.NotApplicable;
                if (r.Cells["Max VLF Count"].Value != DBNull.Value)
                {
                    vlfStatusColor = (int)r.Cells["Max VLF Count"].Value > 10000 ? DashColors.Fail : ((int)r.Cells["Max VLF Count"].Value > 1000 ? DashColors.Warning : DashColors.Success);
                }

                r.Cells["Max VLF Count"].SetStatusColor(vlfStatusColor);
            }
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgv);
        }

        private void TsExcelHistory_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgvHistory);
        }

        private void TsCols_Click(object sender, EventArgs e)
        {
            using var frm = new SelectColumns() { Columns = dgv.Columns };
            frm.ShowDialog(this);
        }

        private void Dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (!SummaryMode || e.RowIndex < 0) return;
            var row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
            InstanceGroupName = (string)row["Instance"];
            SummaryMode = false;
            if (e.ColumnIndex == dgv.Columns["Instance"]?.Index)
            {
                RowFilter = string.Empty;
            }
            else if (e.ColumnIndex == dgv.Columns["Page Verify Not Optimal"]?.Index)
            {
                RowFilter = "page_verify_option <> 2";
            }
            else if (e.ColumnIndex == dgv.Columns["Auto Close"]?.Index)
            {
                RowFilter = "is_auto_close_on = 1";
            }
            else if (e.ColumnIndex == dgv.Columns["Auto Shrink"]?.Index)
            {
                RowFilter = "is_auto_shrink_on = 1";
            }
            else if (e.ColumnIndex == dgv.Columns["Auto Create Stats Disabled"]?.Index)
            {
                RowFilter = "is_auto_create_stats_on = 0";
            }
            else if (e.ColumnIndex == dgv.Columns["Auto Update Stats Disabled"]?.Index)
            {
                RowFilter = "is_auto_update_stats_on = 0";
            }
            else if (e.ColumnIndex == dgv.Columns["Trustworthy"]?.Index)
            {
                RowFilter = "is_trustworthy_on = 1 AND DatabaseName<> 'msdb'";
            }
            else if (e.ColumnIndex == dgv.Columns["In Recovery"]?.Index)
            {
                RowFilter = "state IN(2,3)";
            }
            else if (e.ColumnIndex == dgv.Columns["Suspect"]?.Index)
            {
                RowFilter = "state = 4";
            }
            else if (e.ColumnIndex == dgv.Columns["Emergency"]?.Index)
            {
                RowFilter = "state = 5";
            }
            else if (e.ColumnIndex == dgv.Columns["Offline"]?.Index)
            {
                RowFilter = "state IN(6, 10)";
            }
            else if (e.ColumnIndex == dgv.Columns["Not Using Indirect Checkpoints"]?.Index)
            {
                RowFilter = "target_recovery_time_in_seconds=0 and database_id>4";
            }
            else if (e.ColumnIndex == dgv.Columns["None-Default Target Recovery Time"]?.Index)
            {
                RowFilter = "target_recovery_time_in_seconds NOT IN(0,60)";
            }
            else if (e.ColumnIndex == dgv.Columns["Old Compat Level"]?.Index)
            {
                RowFilter = "compatibility_level < " + Convert.ToInt32(row["Max Supported Compatibility Level"]);
            }
            else if (e.ColumnIndex == dgv.Columns["Max VLF Count"]?.Index)
            {
                RowFilter = "VLFCount >= " + Math.Min(1001, Convert.ToInt32(row["Max VLF Count"]));
            }
            else if (e.ColumnIndex == dgv.Columns["User Database Count"]?.Index)
            {
                RowFilter = "database_id > 4";
            }
            else
            {
                RowFilter = "";
            }
            RefreshData();
        }

        public bool CanNavigateBack => InstanceGroupName != string.Empty;

        public bool NavigateBack()
        {
            if (!CanNavigateBack) return false;

            ShowSummary();
            return true;
        }

        private void tsClearFilter_Click(object sender, EventArgs e)
        {
            RowFilter = string.Empty;
            ((DataView)dgv.DataSource).RowFilter = RowFilter;
            tsClearFilter.Enabled = false;
        }
    }
}