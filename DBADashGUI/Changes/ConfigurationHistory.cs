using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI
{
    public partial class ConfigurationHistory : UserControl, ISetContext
    {
        public ConfigurationHistory()
        {
            InitializeComponent();
            dgv.RegisterClearFilter(tsClearFilter);
        }

        private List<int> InstanceIDs;

        public void SetContext(DBADashContext _context)
        {
            InstanceIDs = _context.RegularInstanceIDs.ToList();
            RefreshData();
        }

        public void RefreshData()
        {
            configuration1.InstanceIDs = InstanceIDs;
            configuration1.RefreshData();
            dgv.AutoGenerateColumns = false;
            dgv.DataSource = new DataView(GetConfigHistory());
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            dgv.Columns[0].Frozen = Common.FreezeKeyColumn;
        }

        private DataTable GetConfigHistory()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.SysConfigHistory_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cn.Open();
            cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
            cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);

            DataTable dt = new();
            da.Fill(dt);
            DateHelper.ConvertUTCToAppTimeZone(ref dt);
            return dt;
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            dgv.CopyGrid();
        }

        private void TsHistoryExcel_Click(object sender, EventArgs e)
        {
            dgv.ExportToExcel();
        }
    }
}