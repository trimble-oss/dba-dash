﻿using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Changes
{
    public partial class DBConfiguration : UserControl, ISetContext
    {
        public DBConfiguration()
        {
            InitializeComponent();
            dgvConfigHistory.RegisterClearFilter(tsClearFilterHistory);
        }

        private List<int> InstanceIDs;
        private int DatabaseID = -1;

        public void SetContext(DBADashContext _context)
        {
            InstanceIDs = _context.InstanceIDs.ToList();
            DatabaseID = _context.DatabaseID;

            RefreshConfig();
            RefreshHistory();
        }

        private DataTable GetDBConfiguration()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.DBConfiguration_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cn.Open();
            cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
            cmd.Parameters.AddWithValue("@ConfiguredOnly", configuredOnlyToolStripMenuItem.Checked);
            cmd.Parameters.AddIfGreaterThanZero("DatabaseID", DatabaseID);
            cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);
            DataTable dt = new();
            da.Fill(dt);
            return dt;
        }

        private void RefreshConfig()
        {
            dgvConfig.Columns.Clear();

            dgvConfig.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Instance", HeaderText = "Instance", Frozen = Common.FreezeKeyColumn });
            dgvConfig.Columns.Add(new DataGridViewTextBoxColumn() { Name = "DB", HeaderText = "Database", Frozen = Common.FreezeKeyColumn });

            var dt = GetDBConfiguration();

            foreach (DataRow r in dt.DefaultView.ToTable(true, "name").Rows)
            {
                DataGridViewTextBoxColumn col = new() { HeaderText = (string)r["name"], Name = (string)r["name"] };
                dgvConfig.Columns.Add(col);
            }
            var lastDB = -1;
            List<DataGridViewRow> rows = new();
            DataGridViewRow row = null;
            foreach (DataRow r in dt.Rows)
            {
                var dbid = (int)r["DatabaseID"];
                if (dbid != lastDB)
                {
                    row = new DataGridViewRow();
                    row.CreateCells(dgvConfig);
                    row.Cells[0].Value = (string)r["InstanceGroupName"];
                    row.Cells[1].Value = (string)r["DB"];
                    rows.Add(row);
                }

                var configName = (string)r["name"];
                var idx = dgvConfig.Columns[configName].Index;
                row.Cells[idx].Value = r["value"];
                row.Cells[idx].SetStatusColor((bool)r["IsDefault"] ? DBADashStatus.DBADashStatusEnum.OK : DBADashStatus.DBADashStatusEnum.WarningLow);
                if (!(bool)r["IsDefault"])
                {
                    row.Cells[idx].Style.Font = new Font(dgvConfig.Font, FontStyle.Bold);
                }
                lastDB = dbid;
            }
            dgvConfig.Rows.AddRange(rows.ToArray());
            dgvConfig.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private DataTable GetDBConfigurationHistory()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.DBConfigurationHistory_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cn.Open();
            cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
            if (DatabaseID > 0)
            {
                cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
            }
            cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);
            DataTable dt = new();
            da.Fill(dt);
            return dt;
        }

        private void RefreshHistory()
        {
            var dt = GetDBConfigurationHistory();
            dgvConfigHistory.AutoGenerateColumns = false;
            DateHelper.ConvertUTCToAppTimeZone(ref dt);
            dgvConfigHistory.DataSource = new DataView(dt);
            dgvConfigHistory.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private void ConfiguredOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshConfig();
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshConfig();
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            dgvConfig.CopyGrid();
        }

        private void TsRefreshHistory_Click(object sender, EventArgs e)
        {
            RefreshHistory();
        }

        private void TsCopyHistory_Click(object sender, EventArgs e)
        {
            dgvConfigHistory.CopyGrid();
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            dgvConfig.ExportToExcel();
        }

        private void TsExcelHistory_Click(object sender, EventArgs e)
        {
            dgvConfigHistory.ExportToExcel();
        }

        private void TsCols_Click(object sender, EventArgs e)
        {
            dgvConfig.PromptColumnSelection();
        }

        private void TsColumnsHistory_Click(object sender, EventArgs e)
        {
            dgvConfigHistory.PromptColumnSelection();
        }
    }
}