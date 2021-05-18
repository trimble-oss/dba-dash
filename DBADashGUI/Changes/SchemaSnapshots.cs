using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using static DBADashGUI.DiffControl;

namespace DBADashGUI.Changes
{
    public partial class SchemaSnapshots : UserControl
    {
        public SchemaSnapshots()
        {
            InitializeComponent();
        }
        readonly Int32 currentSummaryPageSize = 100;
        int currentSummaryPage = 1;
        public Int32 InstanceID;
        public string InstanceName;
        public Int32 DatabaseID;
        public List<Int32> InstanceIDs;


        private void gvSnapshots_SelectionChanged(object sender, EventArgs e)
        {
            if (gvSnapshots.SelectedRows.Count == 1)
            {
                var row = (DataRowView)gvSnapshots.SelectedRows[0].DataBoundItem;
                DateTime SnapshotDate = (DateTime)row["SnapshotDate"];
                Int32 DatabaseID = (Int32)row["DatabaseID"];
                SqlConnection cn = new SqlConnection(Common.ConnectionString);
                using (cn)
                {
                    using (SqlCommand cmd = new SqlCommand("dbo.DDLSnapshotDiff_Get", cn) { CommandType = CommandType.StoredProcedure })
                    {
                        cn.Open();
                        cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
                        var p = cmd.Parameters.AddWithValue("SnapshotDate", SnapshotDate.ToUniversalTime());
                        p.DbType = DbType.DateTime2;

                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        DataSet ds = new DataSet();
                        da.Fill(ds);

                        gvSnapshotsDetail.AutoGenerateColumns = false;
                        gvSnapshotsDetail.DataSource = ds.Tables[0];
                        gvSnapshotsDetail.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                    }
                }
            }
        }

        private void gvSnapshotsDetail_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex>=0 && (e.ColumnIndex ==  colView.Index|| e.ColumnIndex == colDiff.Index))
            {
                var row = (DataRowView)gvSnapshotsDetail.Rows[e.RowIndex].DataBoundItem;
                string ddl = "";
                if (row["NewDDLID"] != DBNull.Value)
                {
                    ddl = Common.DDL((Int64)row["NewDDLID"]);
                }
                string ddlOld = "";
                if (row["OldDDLID"] != DBNull.Value)
                {
                    ddlOld = Common.DDL((Int64)row["OldDDLID"]);
                }
                ViewMode mode = e.ColumnIndex == colDiff.Index ? ViewMode.Diff : ViewMode.Code;
                var frm = new Diff();
                frm.setText(ddlOld, ddl, mode);
                frm.Show();
            }
        }
 
        public void RefreshData()
        {
            if (InstanceID >0 || (InstanceName !=null && InstanceName.Length> 0))
            {
                loadSnapshots();
                tsBack.Enabled = true;
            }
            else {
                tsBack.Enabled = false;
                loadInstanceSummary();
            }
        }

        private void loadInstanceSummary()
        {
            splitSnapshotSummary.Visible = false;
            dgvInstanceSummary.Visible = true;
            SqlConnection cn = new SqlConnection(Common.ConnectionString);
            using (cn)
            {
                using (SqlCommand cmd = new SqlCommand("dbo.DDLSnapshotInstanceSummary_Get", cn) { CommandType = CommandType.StoredProcedure })
                {
                    cn.Open();
                    cmd.Parameters.AddWithValue("InstanceIDs",string.Join(",",InstanceIDs));
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    var dt = new DataTable();
                    da.Fill(dt);
                    Common.ConvertUTCToLocal(ref dt);
                    dgvInstanceSummary.DataSource = dt;
                    dgvInstanceSummary.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                }
            }
        }

        private void loadSnapshots(Int32 pageNum = 1)
        {
            splitSnapshotSummary.Visible = true;
            dgvInstanceSummary.Visible = false;
            gvSnapshotsDetail.DataSource = null;
            currentSummaryPage = Int32.Parse(tsSummaryPageSize.Text);
            SqlConnection cn = new SqlConnection(Common.ConnectionString);
            using (cn)
            {
                using (SqlCommand cmd = new SqlCommand("dbo.DDLSnapshots_Get", cn) { CommandType = CommandType.StoredProcedure })
                {
                    cn.Open();
                    cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
                    cmd.Parameters.AddWithValue("Instance", InstanceName);
                    cmd.Parameters.AddWithValue("PageSize", currentSummaryPage);
                    cmd.Parameters.AddWithValue("PageNumber", pageNum);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    var dt = new DataTable();
                    da.Fill(dt);
                    Common.ConvertUTCToLocal(ref dt);
                    gvSnapshots.AutoGenerateColumns = false;
                    gvSnapshots.DataSource = dt;
                    gvSnapshots.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);

                    tsSummaryPageNum.Text = "Page " + pageNum;
                    tsSummaryBack.Enabled = (pageNum > 1);
                    tsSummaryNext.Enabled = dt.Rows.Count == currentSummaryPage;
                    currentSummaryPage = pageNum;
                }
            }

        }
        private void tsSummaryBack_Click(object sender, EventArgs e)
        {
            loadSnapshots(currentSummaryPage - 1);
        }

        private void tsSummaryNext_Click(object sender, EventArgs e)
        {
            loadSnapshots(currentSummaryPage + 1);
        }

        private void tsSummaryPageSize_Validated(object sender, EventArgs e)
        {
            if (Int32.Parse(tsSummaryPageSize.Text) != currentSummaryPage)
            {
                loadSnapshots(1);
            }
        }

        private void tsSummaryPageSize_Validating(object sender, CancelEventArgs e)
        {
            int.TryParse(tsSummaryPageSize.Text, out int i);
            if (i <= 0)
            {
                tsSummaryPageSize.Text = currentSummaryPageSize.ToString();
            }
        }

        private void dgvInstanceSummary_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if(e.RowIndex>=0 && e.ColumnIndex == colInstance.Index)
            {
                InstanceName = (string)dgvInstanceSummary.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
                RefreshData();
            }
        }

        private void SchemaSnapshots_Load(object sender, EventArgs e)
        {
            splitSnapshotSummary.Dock = DockStyle.Fill;
        }

        private void gvSnapshots_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex>=0 && e.ColumnIndex== colDB.Index)
            {
                var row = (DataRowView)gvSnapshots.Rows[e.RowIndex].DataBoundItem;
                DatabaseID = (Int32)row["DatabaseID"];
                RefreshData();
            }
        }

        private void tsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void tsBack_Click(object sender, EventArgs e)
        {
            if (DatabaseID > 0)
            {
                DatabaseID = -1;
            }
            else if( InstanceName.Length>0 || InstanceID>0)
            {
                InstanceName = "";
                InstanceID = -1;
            }
            RefreshData();
        }
    }
}
