using Humanizer;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    public partial class CompletedRPCBatchEvent : Form
    {
        public CompletedRPCBatchEvent()
        {
            InitializeComponent();
        }

        public int SessionID;
        public DateTime SnapshotDateUTC;
        public DateTime StartTimeUTC;
        public bool IsSleeping;
        public int InstanceID;

        public DataTable GetCompletedRPCBatch()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("SlowQueriesDetail_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceIDs", InstanceID.ToString());
                cmd.Parameters.AddWithValue("SessionID", SessionID);
                cmd.Parameters.AddWithValue("Sort", "timestamp");
                cmd.Parameters.AddWithValue("Top", 1);

                if (IsSleeping)
                {
                    cmd.Parameters.AddWithValue("FromDate", StartTimeUTC);
                    cmd.Parameters.AddWithValue("ToDate", SnapshotDateUTC);
                    cmd.Parameters.AddWithValue("SortDesc", true);
                }
                else
                {
                    cmd.Parameters.Add("FromDate", SqlDbType.DateTime2).Value = SnapshotDateUTC;
                    cmd.Parameters.Add("ToDate", SqlDbType.DateTime2).Value = SnapshotDateUTC.AddDays(2); //For Performance
                    cmd.Parameters.AddWithValue("SortDesc", false);
                }

                DataTable dt = new();
                da.Fill(dt);
                if (dt.Rows.Count == 1)
                {
                    var row = dt.Rows[0];
                    if ((DateTime)row["start_time"] > SnapshotDateUTC && !IsSleeping)
                    {
                        throw new Exception("Unable to find completed event that was running at this time.");
                    }
                    else
                    {
                        Common.ConvertUTCToLocal(ref dt);
                        return dt;
                    }
                }
                else
                {
                    throw new Exception("Unable to find completed event that was running at this time.");
                }

            }
        }

        private void CompletedRPCBatchEvent_Load(object sender, EventArgs e)
        {
            try
            {
                var dt = GetCompletedRPCBatch();
                codeEditor1.Text = Convert.ToString(dt.Rows[0]["text"]);
                dt.Columns.Remove("text");
                dt.Columns.Remove("InstanceID");
                dt.Columns.Remove("DatabaseID");
                dgv.DataSource = dt;
                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    col.HeaderText = col.HeaderText.Titleize();
                }
                Common.PivotDGV(ref dgv);
                dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);

            }
            catch (Exception ex) when (ex.Message == "Unable to find completed event that was running at this time.")
            {
                MessageBox.Show(string.Format("No RPC or Batch completed was found for session {0} running at {1}. This could occur for a number of reasons:{2}* The session is still running or hasn't been processed yet. (Try again later){2}* Slow query capture isn't configured{2}* It did not meet the threshold for collection.{2}* The event was lost for some reason{2}", SessionID, SnapshotDateUTC.ToLocalTime(), Environment.NewLine), "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(codeEditor1.Text);
        }
    }
}
