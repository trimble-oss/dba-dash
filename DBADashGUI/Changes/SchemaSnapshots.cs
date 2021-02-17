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
                        var p = cmd.Parameters.AddWithValue("SnapshotDate", SnapshotDate);
                        p.DbType = DbType.DateTime2;

                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        DataSet ds = new DataSet();
                        da.Fill(ds);

                        gvSnapshotsDetail.AutoGenerateColumns = false;
                        gvSnapshotsDetail.DataSource = ds.Tables[0];

                    }
                }
            }
        }

        private void gvSnapshotsDetail_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            var colCount = gvSnapshotsDetail.Columns.Count;

            if (e.ColumnIndex == colCount - 1 || e.ColumnIndex == colCount - 2)
            {
                var row = (DataRowView)gvSnapshotsDetail.Rows[e.RowIndex].DataBoundItem;
                string ddl = "";
                if (row["NewDDLID"] != DBNull.Value)
                {
                    ddl = Common.DDL((Int64)row["NewDDLID"], Common.ConnectionString);
                }
                string ddlOld = "";
                if (row["OldDDLID"] != DBNull.Value)
                {
                    ddlOld = Common.DDL((Int64)row["OldDDLID"], Common.ConnectionString);
                }
                ViewMode mode = ViewMode.Diff;
                if (e.ColumnIndex == colCount - 2)
                {
                    mode = ViewMode.Code;
                }
                var frm = new Diff();
                frm.setText(ddlOld, ddl, mode);
                frm.Show();
            }
        }

        public Int32 InstanceID;
        public string InstanceName;
        public Int32 DatabaseID;
 
        public void RefreshData()
        {
            loadSnapshots();
        }

        private void loadSnapshots(Int32 pageNum = 1)
        {
            gvSnapshotsDetail.DataSource = null;
            currentSummaryPage = Int32.Parse(tsSummaryPageSize.Text);
            //if (n.Type == SQLTreeItem.TreeType.Database || n.Type == SQLTreeItem.TreeType.Instance || n.Type == SQLTreeItem.TreeType.AzureInstance || n.Type == SQLTreeItem.TreeType.AzureDatabase)
           // {
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
                        DataSet ds = new DataSet();
                        da.Fill(ds);
                        gvSnapshots.AutoGenerateColumns = false;
                        gvSnapshots.DataSource = ds.Tables[0];

                        tsSummaryPageNum.Text = "Page " + pageNum;
                        tsSummaryBack.Enabled = (pageNum > 1);
                        tsSummaryNext.Enabled = ds.Tables[0].Rows.Count == currentSummaryPage;
                        currentSummaryPage = pageNum;
                    }
                }
            //}
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
    }
}
