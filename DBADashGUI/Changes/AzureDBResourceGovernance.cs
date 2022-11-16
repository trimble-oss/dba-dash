using Humanizer;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Changes
{
    public partial class AzureDBResourceGovernance : UserControl, ISetContext
    {
        public AzureDBResourceGovernance()
        {
            InitializeComponent();
        }

        public List<Int32> InstanceIDs;

        public void SetContext(DBADashContext context)
        {
            InstanceIDs = context.AzureInstanceIDs.ToList();
            RefreshData();
        }

        public void RefreshData()
        {
            var dt = GetAzureDBResourceGovernance(InstanceIDs);
            dgv.DataSource = dt;
            dgv.Columns["InstanceID"].Visible = false;
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                col.HeaderText = col.HeaderText.Titleize();
            }
            if (InstanceIDs.Count == 1 & dgv.Rows.Count == 1)
            {
                Common.PivotDGV(ref dgv);
            }
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        public static DataTable GetAzureDBResourceGovernance(List<Int32> instanceIDs)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.AzureDBResourceGovernance_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                var dt = new DataTable();
                cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", instanceIDs));
                da.Fill(dt);
                return dt;
            }
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgv);
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgv);
        }

        private void TsCols_Click(object sender, EventArgs e)
        {
            using (var frm = new SelectColumns() { Columns = dgv.Columns })
            {
                frm.ShowDialog(this);
            }
        }
    }
}
