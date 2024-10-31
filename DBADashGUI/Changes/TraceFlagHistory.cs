using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Changes
{
    public partial class TraceFlagHistory : UserControl, ISetContext
    {
        public TraceFlagHistory()
        {
            InitializeComponent();
            dgvHistory.RegisterClearFilter(tsClearFilterHistory);
            dgvFlags.RegisterClearFilter(tsClearFilter);
        }

        private List<int> InstanceIDs;

        // Pivot data returned by TraceFlags_Get by trace flag
        private DataTable GetTraceFlags()
        {
            var dt = new DataTable();
            dt.Columns.Add("Instance");
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.TraceFlags_Get", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
            cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
            cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);

            using (var rdr = cmd.ExecuteReader())
            {
                var previousInstance = "";
                DataRow r = null;
                while (rdr.Read())
                {
                    var instance = (string)rdr["InstanceDisplayName"];
                    if (instance != previousInstance)
                    {
                        r = dt.NewRow();
                        dt.Rows.Add(r);
                        r["Instance"] = instance;
                    }
                    if (rdr["TraceFlag"] != DBNull.Value)
                    {
                        var flag = (short)rdr["TraceFlag"];
                        var colName = "T" + flag;
                        var validFrom = (DateTime)rdr["ValidFrom"];
                        if (!dt.Columns.Contains(colName))
                        {
                            dt.Columns.Add(colName);
                        }
                        if (validFrom > DateTime.Parse("1900-01-01"))
                        {
                            r[colName] = "Y (" + validFrom.ToAppTimeZone().ToString("yyyy-MM-dd") + ")";
                        }
                        else
                        {
                            r[colName] = "Y";
                        }
                    }
                    previousInstance = instance;
                }
            }

            return dt;
        }

        private void RefreshFlags()
        {
            var dt = GetTraceFlags();
            dgvFlags.DataSource = new DataView(dt);
            dgvFlags.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private void RefreshHistory()
        {
            var dt = GetTraceFlagHistory();
            dgvHistory.AutoGenerateColumns = false;
            dgvHistory.DataSource = new DataView(dt);
            dgvHistory.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private DataTable GetTraceFlagHistory()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.TraceFlagHistory_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cn.Open();
            cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
            cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);

            DataTable dt = new();
            da.Fill(dt);
            DateHelper.ConvertUTCToAppTimeZone(ref dt);
            return dt;
        }

        public void SetContext(DBADashContext _context)
        {
            InstanceIDs = _context.RegularInstanceIDs.ToList();
            RefreshFlags();
            RefreshHistory();
        }

        private void TsRefreshHistory_Click(object sender, EventArgs e)
        {
            RefreshHistory();
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshFlags();
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            dgvFlags.CopyGrid();
        }

        private void TsCopyHistory_Click(object sender, EventArgs e)
        {
            dgvHistory.CopyGrid();
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            dgvFlags.ExportToExcel();
        }

        private void TsExcelHistory_Click(object sender, EventArgs e)
        {
            dgvHistory.ExportToExcel();
        }
    }
}