using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Windows.Forms;

namespace DBADashGUI.Changes
{
    public partial class JobDDLHistory : UserControl, ISetContext
    {
        public JobDDLHistory()
        {
            InitializeComponent();
        }

        private int InstanceID { get; set; }
        private Guid JobID { get; set; }

        public DataTable GetDDLHistory()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.JobDDLHistory_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("JobID", JobID);
                var dt = new DataTable();
                da.Fill(dt);
                DateHelper.ConvertUTCToAppTimeZone(ref dt);
                return dt;
            }
        }

        public void SetContext(DBADashContext context)
        {
            InstanceID = context.InstanceID;
            JobID = context.JobID;
            RefreshData();
        }

        public void RefreshData()
        {
            diffControl1.OldText = "";
            diffControl1.NewText = "";
            dgv.AutoGenerateColumns = false;
            dgv.DataSource = GetDDLHistory();
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private void Dgv_SelectionChanged(object sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count != 1) return;
            var row = (DataRowView)dgv.SelectedRows[0].DataBoundItem;
            long DDLID = (long)row["DDLID"];
            long DDLIDold = row["PreviousDDLID"] == DBNull.Value ? -1 : (long)row["PreviousDDLID"];

            string newText = Common.DDL(DDLID);
            string oldText = DDLIDold > 0 ? Common.DDL(DDLIDold) : "";

            diffControl1.OldText = oldText;
            diffControl1.NewText = newText;
            diffControl1.Mode = string.IsNullOrEmpty(oldText) ? DiffControl.ViewMode.Code : DiffControl.ViewMode.Diff;
        }
    }
}