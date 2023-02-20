using System.Collections.Generic;
using System.Data;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace DBADashGUI
{
    public partial class Corruption : UserControl, ISetContext, IRefreshData
    {
        public Corruption()
        {
            InitializeComponent();
        }

        private DBADashContext context;

        private static readonly DataGridViewColumn[] Cols =
{           new DataGridViewTextBoxColumn(){ Name="Instance", HeaderText="Instance", DataPropertyName="InstanceGroupName", SortMode = DataGridViewColumnSortMode.Automatic},
            new DataGridViewTextBoxColumn(){ Name="name", HeaderText="Database Name", DataPropertyName="name", SortMode = DataGridViewColumnSortMode.Automatic},
            new DataGridViewTextBoxColumn(){ Name="Source Table", HeaderText="Source Table", DataPropertyName="SourceTable", SortMode = DataGridViewColumnSortMode.Automatic},
            new DataGridViewTextBoxColumn(){ Name="LastGoodCheckDbTime", HeaderText="Last Good CheckDb Time", DataPropertyName="LastGoodCheckDbTime", SortMode = DataGridViewColumnSortMode.Automatic},
            new DataGridViewTextBoxColumn(){ Name="UpdateDateLocal", HeaderText="Update Date (local)", DataPropertyName="UpdateDateLocal", SortMode = DataGridViewColumnSortMode.Automatic},
            new DataGridViewTextBoxColumn(){ Name="UpdateDateServer", HeaderText="Update Date (server)", DataPropertyName="UpdateDate", SortMode = DataGridViewColumnSortMode.Automatic},
            new DataGridViewLinkColumn() { Name = "MoreInfo",HeaderText = "More Info",Text="More Info", UseColumnTextForLinkValue = true, LinkColor = DashColors.LinkColor}
        };

        public void SetContext(DBADashContext context)
        {
            this.context = context;
        }

        public void RefreshData()
        {
            var dt = GetCorruption();
            dgv.AutoGenerateColumns = false;
            if (dgv.Columns.Count == 0)
            {
                dgv.Columns.AddRange(Cols);
            }

            dgv.DataSource = dt;

            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private DataTable GetCorruption()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.Corruption_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceIDs", context.InstanceIDs.AsDataTable());
                DataTable dt = new();
                da.Fill(dt);
                DateHelper.ConvertUTCToAppTimeZone(ref dt, new List<string>() { "UpdateDateUtc", "LastGoodCheckDbTime" });
                dt.Columns["UpdateDateUtc"]!.ColumnName = "UpdateDateLocal";
                return dt;
            }
        }

        private void TsRefresh_Click(object sender, System.EventArgs e)
        {
            RefreshData();
        }

        private void Dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || dgv.Columns[e.ColumnIndex].Name != "MoreInfo") return;
            var instance = (string)dgv.Rows[e.RowIndex].Cells["Instance"].Value;
            var sql = SqlStrings.GetCorruptionInfo(instance);
            Common.ShowCodeViewer(sql, "Corruption Info");
        }

        private void tsCopy_Click(object sender, System.EventArgs e)
        {
            dgv.Columns["MoreInfo"]!.Visible = false;
            Common.CopyDataGridViewToClipboard(dgv);
            dgv.Columns["MoreInfo"]!.Visible = true;
        }

        private void tsExcel_Click(object sender, System.EventArgs e)
        {
            dgv.Columns["MoreInfo"]!.Visible = false;
            Common.PromptSaveDataGridView(dgv);
            dgv.Columns["MoreInfo"]!.Visible = true;
        }
    }
}