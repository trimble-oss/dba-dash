using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Changes
{
    public partial class AzureServiceObjectivesHistory : UserControl, ISetContext
    {
        public AzureServiceObjectivesHistory()
        {
            InitializeComponent();
            dgv.RegisterClearFilter(tsClearFilterDB);
            dgvPool.RegisterClearFilter(tsClearFilterPool);
        }

        private List<int> InstanceIDs;

        public void SetContext(DBADashContext _context)
        {
            InstanceIDs = _context.AzureInstanceIDs.ToList();
            RefreshDB();
            RefreshPool();
        }

        private void RefreshDB()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using SqlCommand cmd = new("dbo.AzureServiceObjectivesHistory_Get", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
            cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);
            SqlDataAdapter da = new(cmd);
            DataTable dt = new();
            da.Fill(dt);
            DateHelper.ConvertUTCToAppTimeZone(ref dt);
            dgv.AutoGenerateColumns = false;
            dgv.DataSource = new DataView(dt);
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private void RefreshPool()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using SqlCommand cmd = new("dbo.AzureDBElasticPoolHistory_Get", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
            cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);
            SqlDataAdapter da = new(cmd);
            DataTable dt = new();
            da.Fill(dt);
            DateHelper.ConvertUTCToAppTimeZone(ref dt);
            dgvPool.AutoGenerateColumns = false;
            dgvPool.DataSource = new DataView(dt);
            dgvPool.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshDB();
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            dgv.CopyGrid();
        }

        private void TsRefreshPool_Click(object sender, EventArgs e)
        {
            RefreshPool();
        }

        private void TsCopyPool_Click(object sender, EventArgs e)
        {
            dgvPool.CopyGrid();
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
             dgv.ExportToExcel();
        }

        private void TsPoolExcel_Click(object sender, EventArgs e)
        {
            dgvPool.ExportToExcel();
        }
    }
}