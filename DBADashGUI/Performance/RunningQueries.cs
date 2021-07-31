using Humanizer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DBADashGUI;
namespace DBADashGUI.Performance
{
    public partial class RunningQueries : UserControl
    {
        public RunningQueries()
        {
            InitializeComponent();
        }

        public int InstanceID;
        public List<Int32> InstanceIDs;
        private DateTime currentSnapshotDate;

        public void RefreshData()
        {
            tsBlocking.Visible = false;
            dgv.DataSource = null;
            lblSnapshotDate.Visible = false;
            if (InstanceIDs!=null && InstanceIDs.Count == 1)
            {
                InstanceID = InstanceIDs[0];
            }
            DataTable dt;
            if (InstanceID > 0)
            {
                dt = runningQueriesSummary();
                tsGetLatest.Visible = true;
                tsBack.Visible = InstanceIDs != null && InstanceIDs.Count>1;
            }
            else
            {
                dt = runningQueriesServerSummary();
                tsGetLatest.Visible = false;
                tsBack.Visible = false;
            }
            
            dgv.Columns.Clear();
            dgv.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "Snapshot Date", DataPropertyName = "SnapshotDate", Name = "colSnapshotDate" });
            dgv.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "InstanceID", DataPropertyName = "InstanceID", Name = "colInstanceID", Visible = false });
            dgv.DataSource = dt;
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                col.HeaderText = col.HeaderText.Titleize();
                if(dt.Columns[col.DataPropertyName].DataType.IsNumeric())
                {
                    col.DefaultCellStyle.Format = "#,##0.###";
                }               
            }
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
           
        }

        private DataTable runningQueriesServerSummary()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.RunningQueriesServerSummary_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                var dt = new DataTable();
                cmd.Parameters.AddWithValue("InstanceIDs",string.Join(",",InstanceIDs));
                da.Fill(dt);
                Common.ConvertUTCToLocal(ref dt);
                dt.Columns["SnapshotDateUTC"].ColumnName = "SnapshotDate";
                return dt;
            }
        }

        private DataTable runningQueriesSummary()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using(var cmd = new SqlCommand("dbo.RunningQueriesSummary_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                var dt = new DataTable();
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("FromDate", DateRange.FromUTC);
                cmd.Parameters.AddWithValue("ToDate", DateRange.ToUTC);
                da.Fill(dt);
                Common.ConvertUTCToLocal(ref dt);
                dt.Columns["SnapshotDateUTC"].ColumnName = "SnapshotDate";
                return dt;
            }
        }

        private DataTable runningQueriesSnapshot(ref DateTime snapshotDate)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.RunningQueries_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                var dt = new DataTable();
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                var pSnapshotDate = cmd.Parameters.AddWithValue("SnapshotDate", snapshotDate);
                pSnapshotDate.SqlDbType = SqlDbType.DateTime2;
                pSnapshotDate.Direction = ParameterDirection.InputOutput;
                if (snapshotDate ==  DateTime.MaxValue)
                {
                    pSnapshotDate.Value = DBNull.Value;
                }
                da.Fill(dt);
                snapshotDate = Convert.ToDateTime(pSnapshotDate.Value);
                Common.ConvertUTCToLocal(ref dt);
                dt.Columns["SnapshotDateUTC"].ColumnName = "SnapshotDate";
                dt.Columns["start_time_utc"].ColumnName = "start_time";
                dt.Columns["last_request_start_time_utc"].ColumnName = "last_request_start_time";
                return dt;
            }
        }

        private void loadSnapshot(DateTime snapshotDate)
        {           
            var dt = runningQueriesSnapshot(ref snapshotDate);
            lblSnapshotDate.Visible = true;
            lblSnapshotDate.Text = "Snapshot Date: " + snapshotDate.ToString("yyyy-MM-dd HH:mm:ss");
            tsGetLatest.Visible = true;
            dgv.DataSource = null;
            dgv.Columns.Clear();
            dgv.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "Batch Text", DataPropertyName = "batch_text", Name = "colBatchText",  AutoSizeMode = DataGridViewAutoSizeColumnMode.None, Width=50, SortMode = DataGridViewColumnSortMode.Automatic });
            dgv.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "Text", DataPropertyName = "text", Name = "colText", AutoSizeMode = DataGridViewAutoSizeColumnMode.None, Width=50, SortMode = DataGridViewColumnSortMode.Automatic });
            dgv.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "Plan", DataPropertyName = "query_plan", Name = "colQueryPlan", AutoSizeMode = DataGridViewAutoSizeColumnMode.None, Width = 50, SortMode = DataGridViewColumnSortMode.NotSortable });           
            dgv.DataSource = new DataView(dt);
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                col.HeaderText = col.HeaderText.Titleize();
                if (dt.Columns[col.DataPropertyName].DataType.IsNumeric() && !col.DataPropertyName.EndsWith("id"))
                {
                    col.DefaultCellStyle.Format = "#,##0.###";
                }
            }
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            dgv.Columns["colBatchText"].Width = 200;
            dgv.Columns["colText"].Width = 200;
            dgv.Columns["colQueryPlan"].Width = 90;
            tsBack.Visible = true;

            int blockedCount = dt.AsEnumerable().Where(r => Convert.ToInt16(r["blocking_session_id"]) != 0).Count();
            tsBlocking.Visible = true;
            tsBlocking.Enabled = blockedCount > 0;
            tsBlocking.Text = string.Format("Show Blocking ({0})", blockedCount);
            currentSnapshotDate = snapshotDate;
        }

        private void dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
                if (dgv.Columns[e.ColumnIndex].Name == "colSnapshotDate")
                {
                    var snapshotDate = ((DateTime)row["SnapshotDate"]).ToUniversalTime();
                    InstanceID = (int)row["InstanceID"];
                    loadSnapshot(snapshotDate);
                }
                else if (dgv.Columns[e.ColumnIndex].Name == "colBatchText")
                {
                    var frm = new CodeViewer() { SQL = (string)row["batch_text"] };
                    frm.Show();
                }
                else if (dgv.Columns[e.ColumnIndex].Name == "colText")
                {
                    var frm = new CodeViewer() { SQL = (string)row["text"] };                   
                    frm.Show();
                }
                else if (dgv.Columns[e.ColumnIndex].Name == "colQueryPlan")
                {
                    var plan =  (string)row["query_plan"];
                    string path = System.IO.Path.GetTempFileName() + ".sqlplan";
                    System.IO.File.WriteAllText(path, plan);
                    Process.Start(path);
                }
            }
        }

        private void tsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgv);
        }

        private void tsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgv);
        }

        private void tsBack_Click(object sender, EventArgs e)
        {
            if (dgv.Columns.Contains("colSnapshotDate"))
            {
                InstanceID = -1;
            }
            RefreshData();
        }

        private void tsGetLatest_Click(object sender, EventArgs e)
        {
            loadSnapshot(DateTime.MaxValue);
        }

        private void tsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void dgv_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if(e.ColumnIndex==1 || e.ColumnIndex==2 || e.ColumnIndex == 3)
            {
                if (e.ColumnIndex == 3 && dgv.Columns[e.ColumnIndex].Name =="colQueryPlan")
                {
                    e.Value = e.Value == DBNull.Value ? "" : "View Plan";
                }
                else if (Convert.ToString(e.Value).Length > 1000)
                {
                    e.Value = Convert.ToString(e.Value).Truncate(997) + "...";
                }
            }
        }

        private void tsBlocking_Click(object sender, EventArgs e)
        {
            using (var frm = new BlockingViewer() {InstanceID = InstanceID, SnapshotDate = currentSnapshotDate })
            {
                frm.ShowDialog();
            }
        }
    }
}
