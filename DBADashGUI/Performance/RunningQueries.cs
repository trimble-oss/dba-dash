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
        private DataTable snapshotDT;
        public void RefreshData()
        {
            snapshotDT = null;
            tsBlocking.Visible = false;
            dgv.DataSource = null;
            tsGroupBy.Enabled = false;
            lblSnapshotDate.Visible = false;
            if (InstanceIDs!=null && InstanceIDs.Count == 1)
            {
                InstanceID = InstanceIDs[0];
            }
            dgv.Columns.Clear();
            dgv.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "Snapshot Date", DataPropertyName = "SnapshotDate", Name = "colSnapshotDate", SortMode = DataGridViewColumnSortMode.Automatic });
            dgv.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "InstanceID", DataPropertyName = "InstanceID", Name = "colInstanceID", Visible = false });
            DataTable dt;

            if (InstanceID > 0)
            {
                dt = runningQueriesSummary();
                tsGetLatest.Visible = true;
                tsBack.Visible = InstanceIDs != null && InstanceIDs.Count>1;
                dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Instance", DataPropertyName = "Instance", Name = "colInstance" });
            }
            else
            {
                dt = runningQueriesServerSummary();
                tsGetLatest.Visible = false;
                tsBack.Visible = false;
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
            snapshotDT = runningQueriesSnapshot(ref snapshotDate);
            lblSnapshotDate.Text = "Snapshot Date: " + snapshotDate.ToString("yyyy-MM-dd HH:mm:ss");
            loadSnapshot(new DataView(snapshotDT));
            lblSnapshotDate.Visible = true;
            tsGetLatest.Visible = true;
            int blockedCount = snapshotDT.AsEnumerable().Where(r => Convert.ToInt16(r["blocking_session_id"]) != 0).Count();
            tsBlocking.Visible = true;
            tsBlocking.Enabled = blockedCount > 0;
            tsBlocking.Text = string.Format("Show Blocking ({0})", blockedCount);
            currentSnapshotDate = snapshotDate;
            tsBack.Visible = true;
        }

        private void loadSnapshot(object source)
        {

            dgv.DataSource = null;
            dgv.Columns.Clear();
            dgv.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "Batch Text", DataPropertyName = "batch_text", Name = "colBatchText", AutoSizeMode = DataGridViewAutoSizeColumnMode.None, Width = 50, SortMode = DataGridViewColumnSortMode.Automatic });
            dgv.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "Text", DataPropertyName = "text", Name = "colText", AutoSizeMode = DataGridViewAutoSizeColumnMode.None, Width = 50, SortMode = DataGridViewColumnSortMode.Automatic });
            dgv.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "Plan", DataPropertyName = "query_plan", Name = "colQueryPlan", AutoSizeMode = DataGridViewAutoSizeColumnMode.None, Width = 50, SortMode = DataGridViewColumnSortMode.NotSortable });
            dgv.DataSource = source;
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                col.HeaderText = col.HeaderText.Titleize();
                if (snapshotDT.Columns[col.DataPropertyName].DataType.IsNumeric() && !col.DataPropertyName.EndsWith("id"))
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
                    var plan =  (string)row["query_plan"];
                    string path = System.IO.Path.GetTempFileName() + ".sqlplan";
                    System.IO.File.WriteAllText(path, plan);
                    Process.Start(path);
                }
                else if(dgv.Columns[e.ColumnIndex].Name == "colGroup")
                {
                    string filter = dgv.Columns[e.ColumnIndex].DataPropertyName + "='" + Convert.ToString(row[dgv.Columns[e.ColumnIndex].DataPropertyName]).Replace("'","''") + "'";
                    DataView dv;
                    try
                    {
                        dv = new DataView(snapshotDT, filter, "", DataViewRowState.CurrentRows);
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show("Filter error: " + filter + Environment.NewLine + ex.Message);
                        return;
                    }
                    loadSnapshot(dv);
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

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            groupSnapshot("database_name");
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
                dgv.Columns.Add(new DataGridViewLinkColumn() { DataPropertyName = group, HeaderText = group,Name = "colGroup" });
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
            }
            
        }
    }
}
