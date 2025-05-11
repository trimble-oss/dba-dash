using DBADashGUI.AgentJobs;
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

        public static DataTable GetDDLHistory(int instanceId, Guid jobId)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.JobDDLHistory_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cmd.Parameters.AddWithValue("InstanceID", instanceId);
            cmd.Parameters.AddWithValue("JobID", jobId);
            var dt = new DataTable();
            da.Fill(dt);
            DateHelper.ConvertUTCToAppTimeZone(ref dt);
            return dt;
        }

        public void SetContext(DBADashContext _context)
        {
            InstanceID = _context.InstanceID;
            JobID = _context.JobID;
            RefreshData();
        }

        public void RefreshData()
        {
            diffControl1.OldText = "";
            diffControl1.NewText = "";
            dgv.AutoGenerateColumns = false;
            dgv.DataSource = GetDDLHistory(InstanceID, JobID);
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private void Dgv_SelectionChanged(object sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count != 1) return;
            var row = (DataRowView)dgv.SelectedRows[0].DataBoundItem;
            var DDLID = (long)row["DDLID"];
            var DDLIDold = row["PreviousDDLID"] == DBNull.Value ? -1 : (long)row["PreviousDDLID"];

            var newText = Common.DDL(DDLID);
            var oldText = DDLIDold > 0 ? Common.DDL(DDLIDold) : "";

            diffControl1.OldText = oldText;
            diffControl1.NewText = newText;
            diffControl1.Mode = string.IsNullOrEmpty(oldText) ? DiffControl.ViewMode.Code : DiffControl.ViewMode.Diff;
        }

        private void CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 & e.ColumnIndex >= 0 && dgv.Columns[e.ColumnIndex].Name == "colJobInfoLink")
            {
                var row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
                var DDLID = (long)row["DDLID"];
                var snapshotDate = (DateTime)row["SnapshotDate"];
                var jobInfo = SqlJobInfo.GetJobInfo(DDLID);
                var title = jobInfo.JobName + " " + snapshotDate.ToString("g");
                jobInfo.ShowForm(title);
            }
            else if (e.RowIndex >= 0 & e.ColumnIndex >= 0 && dgv.Columns[e.ColumnIndex].Name == "colCompareInfo")
            {
                var row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
                if (row["PreviousDDLID"] == DBNull.Value)
                {
                    MessageBox.Show("Nothing to compare", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                var DDLID = (long)row["DDLID"];
                var previousDDLID = (long)row["PreviousDDLID"];
                var snapshotDate = (DateTime)row["SnapshotDate"];
                var jobInfo = SqlJobInfo.GetJobInfo(DDLID);
                var jobInfoPrevious = SqlJobInfo.GetJobInfo(previousDDLID);
                SqlJobInfo.ShowCompare(jobInfoPrevious, jobInfo, jobInfoPrevious.JobName += " Previous", jobInfo.JobName, jobInfo.JobName + " " + snapshotDate.ToString("g"));
            }
        }
    }
}