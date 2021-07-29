using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    public partial class BlockingViewer : Form
    {

        public Int32 BlockingSnapshotID { get; set; } = int.MinValue;
        public Int16 BlockingSessionID { get; set; } = 0;
        public int InstanceID { get; set; } = int.MinValue;
        public DateTime SnapshotDate { get; set; } = DateTime.MinValue;

        readonly List<Int16> BlockingNavigation = new List<Int16>();

        public BlockingViewer()
        {
            InitializeComponent();
        }

        private void BlockingViewer_Load(object sender, EventArgs e)
        {
            getSummary();
            loadData();
        }

        private void getSummary()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))         
            using (SqlCommand cmd = new SqlCommand("dbo.BlockingSummary_Get", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
                if (InstanceID > int.MinValue && SnapshotDate> DateTime.MinValue)
                {
                    cmd.Parameters.AddWithValue("@InstanceID", InstanceID);
                    var pSnapshotDate= cmd.Parameters.AddWithValue("@SnapshotDate", SnapshotDate) ;
                    pSnapshotDate.SqlDbType = SqlDbType.DateTime2;
                }
                else
                {
                    cmd.Parameters.AddWithValue("@BlockingsnapshotID", BlockingSnapshotID);
                }
                var rdr = cmd.ExecuteReader();
                if (rdr.Read())
                {
                    lblInstance.Text = "Instance:" + (string)rdr["ConnectionID"];
                    lblSnapshotDate.Text = "Snapshot Date:" + ((DateTime)rdr["SnapshotDateUTC"]).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                    lblBlockedSessions.Text = "Blocked Sessions: " + (Int32)rdr["BlockedSessionCount"];
                    lblBlockedWaitTime.Text = "Blocked Wait Time: " + MillisecondsToReadableDuration((Int64)rdr["BlockedWaitTime"]);
                }
            }
            
        }

        string MillisecondsToReadableDuration(Int64 ms)
        {
            TimeSpan ts= TimeSpan.FromMilliseconds(ms);
            return string.Format("{0:D2}h:{1:D2}m:{2:D2}s:{3:D3}ms",
                                   (Int32)Math.Floor(ts.TotalHours),
                                    ts.Minutes,
                                    ts.Seconds,
                                    ts.Milliseconds);
        }

        private void loadData()
        {

            using (var cn = new SqlConnection(Common.ConnectionString))          
            using (SqlCommand cmd = new SqlCommand("dbo.Blocking_Get", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
                cmd.Parameters.AddWithValue("blocking_session_id", BlockingSessionID);
                if (InstanceID > int.MinValue && SnapshotDate > DateTime.MinValue)
                {
                    cmd.Parameters.AddWithValue("@InstanceID", InstanceID);
                    var pSnapshotDate = cmd.Parameters.AddWithValue("@SnapshotDate", SnapshotDate);
                    pSnapshotDate.SqlDbType = SqlDbType.DateTime2;
                }
                else
                {
                    cmd.Parameters.AddWithValue("@BlockingsnapshotID", BlockingSnapshotID);
                }
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                gvBlocking.AutoGenerateColumns = false;
                gvBlocking.DataSource = new DataView(dt);
                lblBlockers.Text = BlockingSessionID == 0 ? "Root Blockers" : "Blocked By Session " + BlockingSessionID;
                bttnRootBlockers.Enabled = BlockingSessionID != 0;
                if (BlockingSessionID == 0)
                {
                    BlockingNavigation.Clear();
                }
                BlockingNavigation.Add(BlockingSessionID);
                bttnBack.Enabled = BlockingNavigation.Count > 1;
                updatePath();
            }
            
        }

        private void gvBlocking_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var row = (DataRowView)gvBlocking.Rows[e.RowIndex].DataBoundItem;
                if (e.ColumnIndex == BlockedSessions.Index)
                {
                    BlockingSessionID = (Int16)row["session_id"];
                    int blockedCount = Convert.ToInt32(row["BlockCountRecursive"]);
                    if (blockedCount > 0)
                    {
                        loadData();
                    }
                    
                }
                if (e.ColumnIndex == Txt.Index && e.RowIndex >= 0)
                {
                    string txt = (string)row["Txt"];
                    var frm = new CodeViewer
                    {
                        SQL = txt
                    };
                    frm.Show();
                }
            }
    
        }

        void updatePath()
        {
            StringBuilder sb = new StringBuilder();
            foreach(Int32 session in BlockingNavigation)
            {
                if(sb.Length>0) { sb.Append(" \\ ");  }
                string s = session == 0 ? "{root}" : session.ToString();
                sb.Append(s);
            }
            lblPath.Text = sb.ToString();
        }

        private void bttnRootBlockers_Click(object sender, EventArgs e)
        {
            lblPath.Text = "{Root}";
            BlockingSessionID = 0;
            loadData();
        }

        private void gvBlocking_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)gvBlocking.Rows[idx].DataBoundItem;
                var blocked = (Int32)row["BlockCount"];
                var blockedRecursive = (Int32)row["BlockCountRecursive"];
                if (blocked != blockedRecursive)
                {
                    gvBlocking.Rows[idx].Cells["BlockedSessions"].ToolTipText = blocked + " (" + blockedRecursive + " recursive)";
                }
                var txt = (string)gvBlocking.Rows[idx].Cells["Txt"].Value;
                gvBlocking.Rows[idx].Cells["Txt"].Value = txt.Trim();
                gvBlocking.Rows[idx].Cells["BlockedWaitTimeRecursive"].Value = MillisecondsToReadableDuration((Int32)row["WaitTimeRecursive"]);
                if(row["wait_time"]!=DBNull.Value){ gvBlocking.Rows[idx].Cells["WaitTime"].Value = MillisecondsToReadableDuration((Int32)row["Wait_Time"]); }
            }
        }


        private void gvBlocking_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            var dv = (DataView)gvBlocking.DataSource;
            BlockedWaitTimeRecursive.HeaderCell.SortGlyphDirection = System.Windows.Forms.SortOrder.None;
            WaitTime.HeaderCell.SortGlyphDirection = System.Windows.Forms.SortOrder.None;
           if (e.ColumnIndex == BlockedWaitTimeRecursive.Index)
            {         
               dv.Sort = dv.Sort == "WaitTimeRecursive" ? "WaitTimeRecursive DESC" : "WaitTimeRecursive";
               BlockedWaitTimeRecursive.HeaderCell.SortGlyphDirection = dv.Sort == "WaitTimeRecursive" ? System.Windows.Forms.SortOrder.Ascending : System.Windows.Forms.SortOrder.Descending;
            }
           if(e.ColumnIndex == WaitTime.Index)
            {
                dv.Sort = dv.Sort == "Wait_Time" ? "Wait_Time DESC" : "Wait_Time";
                BlockedWaitTimeRecursive.HeaderCell.SortGlyphDirection = dv.Sort == "Wait_Time" ? System.Windows.Forms.SortOrder.Ascending : System.Windows.Forms.SortOrder.Descending;
            }
        }

        private void bttnBack_Click(object sender, EventArgs e)
        {
            if (BlockingNavigation.Count > 1)
            {
                BlockingNavigation.RemoveAt(BlockingNavigation.Count - 1);
                BlockingSessionID = BlockingNavigation[BlockingNavigation.Count - 1];
                BlockingNavigation.RemoveAt(BlockingNavigation.Count - 1);
                loadData();
            }

        }
    }
}
