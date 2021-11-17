using Humanizer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using Microsoft.Data.SqlClient;
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
        private DataTable snapshotDT;
        public DateTime SnapshotDateFrom;
        public DateTime SnapshotDateTo;
        public int SessionID=0;
   
        public void RefreshData()
        {
            splitContainer1.Panel2Collapsed = true;
            dgvSessionWaits.DataSource = null;
            lblJobCount.Visible = false;
            snapshotDT = null;
            tsBack.Enabled = false;
            tsBlocking.Visible = false;
            dgv.DataSource = null;
            tsGroupBy.Enabled = false;
            lblSnapshotDate.Visible = false;
            tsPrevious.Visible = false;
            tsNext.Visible = false;
            tsGetLatest.Visible = true;
            if (InstanceIDs!=null && InstanceIDs.Count == 1)
            {
                InstanceID = InstanceIDs[0];
            }
            dgv.Columns.Clear();
            dgv.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "Snapshot Date", DataPropertyName = "SnapshotDate", Name = "colSnapshotDate", SortMode = DataGridViewColumnSortMode.Automatic });
            dgv.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "InstanceID", DataPropertyName = "InstanceID", Name = "colInstanceID", Visible = false });
            DataTable dt;

            if (SessionID != 0)
            {
                tsGetLatest.Visible = false;
                snapshotDT = runningQueriesForSession(SessionID, SnapshotDateFrom, SnapshotDateTo, InstanceID);
                loadSnapshot(new DataView(snapshotDT));
                return;
            }
            else if (InstanceID > 0)
            {
                
                dt = runningQueriesSummary();
                tsGetLatest.Visible = true;
                tsBack.Enabled = InstanceIDs != null && InstanceIDs.Count>1;
                dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Instance", DataPropertyName = "Instance", Name = "colInstance" });
            }
            else
            {
                dt = runningQueriesServerSummary();
                tsGetLatest.Visible = false;
                dgv.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "Instance", DataPropertyName = "Instance", Name = "colInstance", SortMode= DataGridViewColumnSortMode.Automatic });
            }
                
           
            dgv.DataSource = new DataView(dt);
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

        private DataTable runningQueriesForSession(int sessionID, DateTime fromDate, DateTime toDate, int instanceID)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("RunningQueriesForSession_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("SessionID", sessionID);
                cmd.Parameters.Add(new SqlParameter("SnapshotDateFrom", fromDate) { DbType = DbType.DateTime2 });
                cmd.Parameters.Add(new SqlParameter("SnapshotDateTo", toDate) { DbType = DbType.DateTime2 });
                cmd.Parameters.AddWithValue("InstanceID", instanceID);
                DataTable dt = new DataTable();               
                da.Fill(dt);
                Common.ConvertUTCToLocal(ref dt);
                dt.Columns["SnapshotDateUTC"].ColumnName = "SnapshotDate";
                dt.Columns["start_time_utc"].ColumnName = "start_time";
                dt.Columns["last_request_start_time_utc"].ColumnName = "last_request_start_time";
                dt.Columns["login_time_utc"].ColumnName = "login_time";
                return dt;
            }
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

        private DataTable runningQueriesSnapshot(ref DateTime snapshotDate,int skip=0)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.RunningQueries_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                var dt = new DataTable();
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                var pSnapshotDate = cmd.Parameters.AddWithValue("SnapshotDate", snapshotDate);
                cmd.Parameters.AddWithValue("Skip", skip);
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
                dt.Columns["login_time_utc"].ColumnName = "login_time";
                return dt;
            }
        }

        private void loadSnapshot(DateTime snapshotDate,int skip=0)
        {           
            snapshotDT = runningQueriesSnapshot(ref snapshotDate,skip);
            int runningJobCount = snapshotDT.AsEnumerable().Where(r => r["job_id"] != DBNull.Value).Count();
            if (runningJobCount == 0)
            {
                snapshotDT.Columns.Remove("job_id");
                snapshotDT.Columns.Remove("job_name");
                lblJobCount.Visible = false;
            }
            else {
                lblJobCount.Visible = true;
                lblJobCount.Text = string.Format("Running Jobs: {0}", runningJobCount);
            }

            lblSnapshotDate.Text = "Snapshot Date: " + snapshotDate.ToLocalTime().ToString();
            loadSnapshot(new DataView(snapshotDT));
            lblSnapshotDate.Visible = true;
            tsGetLatest.Visible = true;
            tsPrevious.Visible = true;
            tsNext.Visible = true;
            int blockedCount = snapshotDT.AsEnumerable().Where(r => Convert.ToInt16(r["blocking_session_id"]) != 0).Count();
            tsBlocking.Visible = true;
            tsBlocking.Enabled = blockedCount > 0;
            tsBlocking.Text = string.Format("Show Blocking ({0})", blockedCount);
            currentSnapshotDate = snapshotDate;
            tsBack.Enabled = true;
        }

        private void loadSnapshot(DataView source)
        {

            dgv.DataSource = null;
            dgv.Columns.Clear();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "InstanceID", DataPropertyName = "InstanceID", Name = "colInstanceID", Visible=false});
            dgv.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "Batch Text", DataPropertyName = "batch_text", Name = "colBatchText", AutoSizeMode = DataGridViewAutoSizeColumnMode.None, Width = 50, SortMode = DataGridViewColumnSortMode.Automatic });
            dgv.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "Text", DataPropertyName = "text", Name = "colText", AutoSizeMode = DataGridViewAutoSizeColumnMode.None, Width = 50, SortMode = DataGridViewColumnSortMode.Automatic });
            dgv.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "Plan", DataPropertyName = "query_plan", Name = "colQueryPlan", AutoSizeMode = DataGridViewAutoSizeColumnMode.None, Width = 50, SortMode = DataGridViewColumnSortMode.NotSortable });
            dgv.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "Session ID", DataPropertyName = "session_id", Name = "colSessionID", SortMode = DataGridViewColumnSortMode.Automatic });
            dgv.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "Top Session Waits", DataPropertyName = "TopSessionWaits", Name = "colTopSessionWaits", SortMode = DataGridViewColumnSortMode.Automatic });
            dgv.DataSource = source;
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                col.HeaderText = col.HeaderText.Titleize();
                if (source.Table.Columns[col.DataPropertyName].DataType.IsNumeric() && !col.DataPropertyName.EndsWith("id"))
                {
                    col.DefaultCellStyle.Format = "#,##0.###";
                }
            }
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            dgv.Columns["colBatchText"].Width = 200;
            dgv.Columns["colText"].Width = 200;
            dgv.Columns["colQueryPlan"].Width = 90;
            tsGroupBy.Enabled = dgv.Rows.Count>1;
            
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
                    tsBack.Enabled = true;
                }
                else if (dgv.Columns[e.ColumnIndex].Name == "colInstance")
                {
                    InstanceID = (int)row["InstanceID"];
                    RefreshData();
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
                    var plan = (string)row["query_plan"];
                    string path = System.IO.Path.GetTempFileName() + ".sqlplan";
                    System.IO.File.WriteAllText(path, plan);
                    Process.Start(path);
                }
                else if (dgv.Columns[e.ColumnIndex].Name == "colGroup")
                {
                    string filter = dgv.Columns[e.ColumnIndex].DataPropertyName + "='" + Convert.ToString(row[dgv.Columns[e.ColumnIndex].DataPropertyName]).Replace("'", "''") + "'";
                    DataView dv;
                    try
                    {
                        dv = new DataView(snapshotDT, filter, "", DataViewRowState.CurrentRows);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Filter error: " + filter + Environment.NewLine + ex.Message);
                        return;
                    }
                    loadSnapshot(dv);
                    tsBack.Enabled = true;
                }
                else if (dgv.Columns[e.ColumnIndex].Name == "colSessionID")
                {
                    var frm = new CompletedRPCBatchEvent()
                    {
                        SessionID = Convert.ToInt32(row["session_id"]),
                        InstanceID = Convert.ToInt32(row["InstanceID"]),
                        SnapshotDateUTC = Convert.ToDateTime(row["SnapshotDate"]).ToUniversalTime(),
                        StartTimeUTC = row["start_time"] == DBNull.Value ? Convert.ToDateTime(row["last_request_start_time"]).ToUniversalTime() : Convert.ToDateTime(row["start_time"]).ToUniversalTime(),
                        IsSleeping = Convert.ToString(row["status"]) == "sleeping"
                    };
                    frm.ShowDialog();
                }
                else if(dgv.Columns[e.ColumnIndex].Name == "colTopSessionWaits")
                {
                    splitContainer1.Panel2Collapsed = false;
                    if (dgvSessionWaits.Columns.Count == 0)
                    {
                        dgvSessionWaits.AutoGenerateColumns = false;
                        dgvSessionWaits.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Session ID", DataPropertyName = "session_id", Name="colSessionID" });
                        dgvSessionWaits.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Wait Type", DataPropertyName = "WaitType", Name = "colWaitType" });
                        dgvSessionWaits.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Waiting Tasks Count", DataPropertyName = "waiting_tasks_count" });
                        dgvSessionWaits.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Wait Time (ms)", DataPropertyName = "wait_time_ms",DefaultCellStyle = new DataGridViewCellStyle() { Format = "N0" } });
                        dgvSessionWaits.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Wait Time %", DataPropertyName = "wait_pct", DefaultCellStyle = new DataGridViewCellStyle() { Format = "P1" } });
                        dgvSessionWaits.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Max Wait Time (ms)", DataPropertyName = "max_wait_time_ms", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N0" } });
                        dgvSessionWaits.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Signal Wait Time (ms)", DataPropertyName = "signal_wait_time_ms", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N0" } });
                        dgvSessionWaits.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Signal Wait %", DataPropertyName = "signal_wait_pct", DefaultCellStyle = new DataGridViewCellStyle() { Format = "P1" } });
                        dgvSessionWaits.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "Help", Text="help", UseColumnTextForLinkValue=true, Name="colHelp" });
                    }
                    dgvSessionWaits.Columns["colSessionID"].Visible = false;
                    var sessionid = (short)row["session_id"];
                    dgvSessionWaits.DataSource= GetSessionWaits(InstanceID,sessionid , Convert.ToDateTime(row["SnapshotDate"]).ToUniversalTime(), Convert.ToDateTime(row["login_time"]).ToUniversalTime());
                    dgvSessionWaits.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                    sessionToolStripMenuItem.Tag = sessionid;
                    lblWaitsForSession.Text = "Waits For Session ID: " + sessionid.ToString();
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
            string rowFilter = String.Empty;
            if(dgv.DataSource.GetType() == typeof(DataView))
            {
               rowFilter= ((DataView)dgv.DataSource).RowFilter;
            }
            if (dgv.Columns.Contains("colGroup") ||  !string.IsNullOrEmpty(rowFilter) )
            {
                loadSnapshot(new DataView(snapshotDT));
                tsBack.Enabled = false;
            }
            else if (dgv.Columns.Contains("colSnapshotDate"))
            {
                InstanceID = -1;
                RefreshData();
            }
            else
            {
                RefreshData();
            }         
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
       
            if (dgv.Columns[e.ColumnIndex].Name =="colQueryPlan")
            {
                e.Value = e.Value == DBNull.Value ? "" : "View Plan";
            }
            else if (Convert.ToString(e.Value).Length > 1000)
            {
                e.Value = Convert.ToString(e.Value).Truncate(997) + "...";
            }
      

        }
        private void tsBlocking_Click(object sender, EventArgs e)
        {
            using (var frm = new BlockingViewer() {InstanceID = InstanceID, SnapshotDate = currentSnapshotDate })
            {
                frm.ShowDialog();
            }
        }

        private void groupSnapshot(string group)
        {
            if (snapshotDT != null && snapshotDT.Rows.Count>0)
            {
                DataTable groupedDT = new DataTable();
                groupedDT.Columns.Add(group);
                groupedDT.Columns.Add("execution_count", typeof(long));
                groupedDT.Columns.Add("sum_cpu_time", typeof(long));
                groupedDT.Columns.Add("sum_reads", typeof(long));
                groupedDT.Columns.Add("sum_logical_reads", typeof(long));
                groupedDT.Columns.Add("sum_writes", typeof(long));
                groupedDT.Columns.Add("sum_granted_query_memory_kb", typeof(long));
                groupedDT = snapshotDT.AsEnumerable()
                      .GroupBy(r => r.Field<string>(group))
                      .Select(g =>
                      {
                          var row = groupedDT.NewRow();
                          row[group] = g.Key;
                          row["execution_count"] = g.Count();
                          row["sum_reads"] = g.Sum(r => r["reads"] == DBNull.Value ? 0 : r.Field<long>("reads"));
                          row["sum_logical_reads"] = g.Sum(r => r["logical_reads"] == DBNull.Value ? 0 : r.Field<long>("logical_reads"));
                          row["sum_writes"] = g.Sum(r => r["writes"] == DBNull.Value ? 0 : r.Field<long>("writes"));
                          row["sum_cpu_time"] = g.Sum(r => r["cpu_time"] == DBNull.Value ? 0 : r.Field<int>("cpu_time"));
                          row["sum_granted_query_memory_kb"] = g.Sum(r => r["granted_query_memory_kb"]==DBNull.Value? 0 : Convert.ToInt64(r["granted_query_memory_kb"]));
                          return row;
                      }).CopyToDataTable();

                dgv.Columns.Clear();
                dgv.Columns.Add(new DataGridViewLinkColumn() { DataPropertyName = group, HeaderText = group,Name = "colGroup", SortMode = DataGridViewColumnSortMode.Automatic });
                dgv.DataSource = new DataView(groupedDT);
                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    col.HeaderText = col.HeaderText.Titleize();
                    if (groupedDT.Columns[col.DataPropertyName].DataType.IsNumeric() && !col.DataPropertyName.EndsWith("id"))
                    {
                        col.DefaultCellStyle.Format = "#,##0.###";
                    }
                }
                dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            }
        }

        private void tsGroupBy_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            if(ts.Tag== null)
            {
                loadSnapshot(new DataView(snapshotDT));
            }
            else
            {
                groupSnapshot((string)ts.Tag);
                tsBack.Enabled = true;
            }
            
        }

        private void tsPrevious_Click(object sender, EventArgs e)
        {
            loadSnapshot(currentSnapshotDate, -1);
        }

        private void tsNext_Click(object sender, EventArgs e)
        {
            loadSnapshot(currentSnapshotDate, 1);
        }

        private DataTable GetSessionWaits(int InstanceID,short? SessionID, DateTime? SnapshotDateUTC,DateTime? LoginTimeUTC)
        {
            using(var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.SessionWaits_Get", cn) { CommandType = CommandType.StoredProcedure })
            using(var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("SessionID", SessionID);         
                cmd.Parameters.Add(new SqlParameter("SnapshotDateUTC", SnapshotDateUTC) { DbType = DbType.DateTime2 });
                cmd.Parameters.AddWithValue("LoginTimeUTC", LoginTimeUTC);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        private DataTable GetSessionWaitSummary(int InstanceID, DateTime? SnapshotDateUTC)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.SessionWaits_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.Add(new SqlParameter("SnapshotDateUTC", SnapshotDateUTC) { DbType = DbType.DateTime2 });
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        private void tsSessionWaitCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgvSessionWaits);
        }

        private void tsSessionWaitExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgvSessionWaits);
        }

        private void allSessionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dgvSessionWaits.Columns["colSessionID"].Visible = true;
            dgvSessionWaits.DataSource = GetSessionWaits(InstanceID, null, currentSnapshotDate, null);
            lblWaitsForSession.Text = "All Sessions";
        }

        private void summaryViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dgvSessionWaits.Columns["colSessionID"].Visible = false;
            dgvSessionWaits.DataSource = GetSessionWaitSummary(InstanceID,currentSnapshotDate);
            lblWaitsForSession.Text = "Session Wait Summary";
        }

        private void sessionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dgvSessionWaits.Columns["colSessionID"].Visible = false;
            var sessionid = (short)sessionToolStripMenuItem.Tag;
            dgvSessionWaits.DataSource = GetSessionWaits(InstanceID, sessionid, currentSnapshotDate, null);
            lblWaitsForSession.Text = "Waits For Session ID: " + sessionid.ToString();
        }

        private void dgvSessionWaits_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgvSessionWaits.Columns[e.ColumnIndex].Name == "colHelp")
            {
                string wait = (string)dgvSessionWaits.Rows[e.RowIndex].Cells["colWaitType"].Value;
                System.Diagnostics.Process.Start("https://www.sqlskills.com/help/waits/" + wait.ToLower() + "/");
            }
        }
    }
}
