using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using Color = System.Drawing.Color;

namespace DBADashGUI
{
    public partial class Corruption : UserControl, ISetContext, IRefreshData
    {
        public Corruption()
        {
            InitializeComponent();
            dgv.RegisterClearFilter(tsClearFilter);
        }

        private DBADashContext context;

        private readonly DataGridViewColumn[] Cols =
{           new DataGridViewTextBoxColumn(){ Name="Instance", HeaderText="Instance", DataPropertyName="InstanceGroupName", SortMode = DataGridViewColumnSortMode.Automatic},
            new DataGridViewTextBoxColumn(){ Name="name", HeaderText="Database Name", DataPropertyName="name", SortMode = DataGridViewColumnSortMode.Automatic},
            new DataGridViewTextBoxColumn(){ Name="Source Table", HeaderText="Source Table", DataPropertyName="SourceTable", SortMode = DataGridViewColumnSortMode.Automatic, ToolTipText = "Table where corruption was detected."},
            new DataGridViewTextBoxColumn(){ Name="LastGoodCheckDbTime", HeaderText="Last Good CheckDb Time", DataPropertyName="LastGoodCheckDbTime", SortMode = DataGridViewColumnSortMode.Automatic, ToolTipText = "Time of last successful DBCC CHECKDB."},
            new DataGridViewTextBoxColumn(){ Name="UpdateDateLocal", HeaderText="Update Date (local)", DataPropertyName="UpdateDateLocal", SortMode = DataGridViewColumnSortMode.Automatic, ToolTipText = "Last time corruption was detected. (App timezone)"},
            new DataGridViewTextBoxColumn(){ Name="UpdateDateServer", HeaderText="Update Date (server)", DataPropertyName="UpdateDate", SortMode = DataGridViewColumnSortMode.Automatic, ToolTipText = "Last time corruption was detected. (Server timezone)"},
            new DataGridViewTextBoxColumn(){ Name="Ack Date", HeaderText="Ack Date (local)", DataPropertyName="AckDate", SortMode = DataGridViewColumnSortMode.Automatic, ToolTipText = "Acknowledge date (App timezone). Corruption detected after this date will be highlighted again."},
            new DataGridViewTextBoxColumn(){ Name="CountOfRows", HeaderText="Rows", DataPropertyName="CountOfRows", SortMode = DataGridViewColumnSortMode.Automatic, ToolTipText = "msdb.dbo.suspect_pages table has a limit of 1000 rows. \nOnce the limit is reached new errors won't be logged. \nThe DBA is responsible for clearing out old data. \nStatus is critical at 1000 rows and warning at 800.\n\nNote: sys.dm_db_mirroring_auto_page_repair & sys.dm_hadr_auto_page_repair have limits of 100 rows.  Old entries are automatically replaced."},
            new DataGridViewLinkColumn() { Name = "MoreInfo",HeaderText = "More Info",Text="More Info", UseColumnTextForLinkValue = true, LinkColor = DashColors.LinkColor, DefaultCellStyle = new DataGridViewCellStyle() { BackColor = Color.White, ForeColor = Color.Black}, ToolTipText = "Script to run on monitored SQL instance to get more information about detected corruption."},
            new DataGridViewLinkColumn() { Name = "Acknowledge",HeaderText = "Acknowledge",Text="Acknowledge", LinkColor = DashColors.LinkColor, DefaultCellStyle = new DataGridViewCellStyle() { BackColor = Color.White, ForeColor = Color.Black}, ToolTipText = "Click link to acknowledge issue after taking steps to identify the root cause of the corruption, repair if necessary and validate that the database is free of corruption."}
        };

        public void SetContext(DBADashContext _context)
        {
            this.context = _context;
        }

        public void RefreshData()
        {
            var dt = GetCorruption();
            dgv.AutoGenerateColumns = false;
            if (dgv.Columns.Count == 0)
            {
                dgv.Columns.AddRange(Cols);
            }

            dgv.DataSource = new DataView(dt);

            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private DataTable GetCorruption()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.Corruption_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cmd.Parameters.AddWithValue("InstanceIDs", context.InstanceIDs.AsDataTable());
            DataTable dt = new();
            da.Fill(dt);
            DateHelper.ConvertUTCToAppTimeZone(ref dt, new List<string>() { "UpdateDateUtc", "LastGoodCheckDbTime", "AckDate" });
            dt.Columns["UpdateDateUtc"]!.ColumnName = "UpdateDateLocal";
            return dt;
        }

        private static void AcknowledgeCorruption(int DatabaseID, bool Clear = false)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.Corruption_Ack", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cn.Open();
            cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
            cmd.Parameters.AddWithValue("Clear", Clear);
            cmd.ExecuteNonQuery();
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void Dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            switch (dgv.Columns[e.ColumnIndex].Name)
            {
                case "MoreInfo":
                    {
                        var instance = (string)dgv.Rows[e.RowIndex].Cells["Instance"].Value;
                        var sql = SqlStrings.GetCorruptionInfo(instance);
                        Common.ShowCodeViewer(sql, "Corruption Info");
                        break;
                    }
                case "Acknowledge":
                    {
                        var row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
                        var databaseID = (int)row["DatabaseID"];
                        var status = (DBADashStatus.DBADashStatusEnum)row["CorruptionStatus"];
                        if (status != DBADashStatus.DBADashStatusEnum.Acknowledged && MessageBox.Show(
                                "Before acknowledging corruption you should take steps to repair the corruption if needed and investigate the root cause to prevent the issue from re-occurring.  Run a DBCC CHECKDB to validate that your database is free of corruption.  Do you want to acknowledge?",
                                "Acknowledge", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                        {
                            AcknowledgeCorruption(databaseID);
                            RefreshData();
                        }
                        else if (status == DBADashStatus.DBADashStatusEnum.Acknowledged &&
                                 MessageBox.Show("Clear Acknowledgement?", "Acknowledge", MessageBoxButtons.YesNo,
                                     MessageBoxIcon.Question) ==
                                 DialogResult.Yes)
                        {
                            AcknowledgeCorruption(databaseID, true);
                            RefreshData();
                        }

                        break;
                    }
            }
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            dgv.Columns["MoreInfo"]!.Visible = false;
            dgv.CopyGrid();
            dgv.Columns["MoreInfo"]!.Visible = true;
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            dgv.Columns["MoreInfo"]!.Visible = false;
            dgv.ExportToExcel();
            dgv.Columns["MoreInfo"]!.Visible = true;
        }

        private void Dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (var idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)dgv.Rows[idx].DataBoundItem;
                var status = (DBADashStatus.DBADashStatusEnum)row["CorruptionStatus"];
                var lastGoodStatus = (DBADashStatus.DBADashStatusEnum)row["LastGoodCheckDBStatus"];
                var rowCountStatus = (DBADashStatus.DBADashStatusEnum)row["CountOfRowsStatus"];
                foreach (var col in Cols)
                {
                    dgv.Rows[idx].Cells[col.Index].SetStatusColor(col.Name switch { "LastGoodCheckDbTime" => lastGoodStatus, "CountOfRows" => rowCountStatus, _ => status });
                }

                dgv.Rows[idx].Cells["Acknowledge"].Value =
                    status == DBADashStatus.DBADashStatusEnum.Acknowledged ? "Clear" : "Acknowledge";
            }
        }
    }
}