using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using static DBADashGUI.DiffControl;
using System.IO;
using System.Globalization;

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


        private DataSet ddlSnapshotDiff(DateTime snapshotDate, int databaseID)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.DDLSnapshotDiff_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
                cmd.Parameters.AddWithValue("DatabaseID", databaseID);
                var p = cmd.Parameters.AddWithValue("SnapshotDate", snapshotDate);
                p.DbType = DbType.DateTime2;     
                DataSet ds = new DataSet();
                da.Fill(ds);

                return ds;
            }           
        }

        private void gvSnapshots_SelectionChanged(object sender, EventArgs e)
        {
            if (gvSnapshots.SelectedRows.Count == 1)
            {
                var row = (DataRowView)gvSnapshots.SelectedRows[0].DataBoundItem;
                DateTime snapshotDate = (DateTime)row["SnapshotDate"];
                Int32 databaseID = (Int32)row["DatabaseID"];
          
                DataSet ds = ddlSnapshotDiff(snapshotDate,databaseID);                     
                gvSnapshotsDetail.AutoGenerateColumns = false;
                gvSnapshotsDetail.DataSource = ds.Tables[0];
                gvSnapshotsDetail.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);                    
                
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

        private DataTable ddlSnapshotInstanceSummary()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.DDLSnapshotInstanceSummary_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
                cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                
                var dt = new DataTable();
                da.Fill(dt);
                Common.ConvertUTCToLocal(ref dt);
                return dt;
            }         
        }

        private void loadInstanceSummary()
        {
            splitSnapshotSummary.Visible = false;
            dgvInstanceSummary.Visible = true;
            var dt = ddlSnapshotInstanceSummary();
            dgvInstanceSummary.DataSource = dt;
            dgvInstanceSummary.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);       
        }

        private DataTable getDDLSnapshots(int pageNum)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.DDLSnapshots_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
                cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
                cmd.Parameters.AddWithValue("InstanceDisplayName", InstanceName);
                cmd.Parameters.AddWithValue("PageSize", currentSummaryPage);
                cmd.Parameters.AddWithValue("PageNumber", pageNum);
                
                var dt = new DataTable();
                da.Fill(dt);
                Common.ConvertUTCToLocal(ref dt);
                return dt;
            }           
        }

        private void loadSnapshots(int pageNum = 1)
        {
            splitSnapshotSummary.Visible = true;
            dgvInstanceSummary.Visible = false;
            gvSnapshotsDetail.DataSource = null;
            currentSummaryPage = Int32.Parse(tsSummaryPageSize.Text);    
            var dt = getDDLSnapshots(pageNum);         
            gvSnapshots.AutoGenerateColumns = false;
            gvSnapshots.DataSource = dt;
            gvSnapshots.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            tsSummaryPageNum.Text = "Page " + pageNum;
            tsSummaryBack.Enabled = (pageNum > 1);
            tsSummaryNext.Enabled = dt.Rows.Count == currentSummaryPage;
            currentSummaryPage = pageNum;
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
            Common.StyleGrid(ref gvSnapshots);
            Common.StyleGrid(ref gvSnapshotsDetail);
            Common.StyleGrid(ref dgvInstanceSummary);
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
            else if(e.RowIndex>=0 && e.ColumnIndex == colExport.Index)
            {
                var row = (DataRowView)gvSnapshots.Rows[e.RowIndex].DataBoundItem;
                export(row);
            }
        }

        private void export(DataRowView row)
        {
            var dbid = (Int32)row["DatabaseID"];
            var db = (string)row["DB"];
            var snapshotDate = (DateTime)row["SnapshotDate"];
            using (var ofd = new FolderBrowserDialog() { Description = "Select a folder" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string folder = System.IO.Path.Combine(ofd.SelectedPath, InstanceName + "_" + db + "_" + snapshotDate.ToString("yyyyMMdd_HHmmss"));
                    Directory.CreateDirectory(folder);
                    try
                    {
                        ExportSchema(folder, dbid, snapshotDate);
                        MessageBox.Show("Export Completed", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                        {
                            FileName = folder,
                            UseShellExecute = true,
                            Verb = "open"
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Export", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                }
            }
        }

        private void ExportSchema(string folder,int DBID, DateTime SnapshotDate)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using(var cmd = new SqlCommand("dbo.DBSchemaAtDate_Get",cn) {  CommandType = CommandType.StoredProcedure, CommandTimeout = 300})
            {
                cn.Open();
                cmd.Parameters.AddWithValue("DBID", DBID);
                var pSnapshotDate = cmd.Parameters.AddWithValue("SnapshotDate", SnapshotDate);
                pSnapshotDate.SqlDbType = SqlDbType.DateTime2;
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        string schema =(string)rdr["SchemaName"];
                        string name = (string)rdr["ObjectName"];
                        string objType = (string)rdr["TypeDescription"];
                        string subFolder = Path.Combine(Path.Combine(folder, Common.StripInvalidFileNameChars(schema)), objType);
                        string filePath = Path.Combine(subFolder, Common.StripInvalidFileNameChars(name) + ".sql");
                        if (!Directory.Exists(subFolder))
                        {
                            Directory.CreateDirectory(subFolder);
                        }
                        var bDDL = (byte[])rdr["DDL"];
                        string sql = DBADash.SchemaSnapshotDB.Unzip(bDDL);
                        File.WriteAllText(filePath, sql);                       
                    }
                }

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
