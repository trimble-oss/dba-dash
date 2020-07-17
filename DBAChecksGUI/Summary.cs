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

namespace DBAChecksGUI
{
    public partial class Summary : UserControl
    {

        public List<Int32> InstanceIDs;
        public string ConnectionString;

        public Summary()
        {
            InitializeComponent();
        }

        public void RefreshData()
        {
            SqlConnection cn = new SqlConnection(ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.Summary_Get", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",",InstanceIDs));
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                dgvSummary.AutoGenerateColumns = false;
                dgvSummary.DataSource = dt;
            }
        }

        private void dgvSummary_RowAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)dgvSummary.Rows[idx].DataBoundItem;
                string[] StatusColumns = new string[] { "FullBackupStatus", "LogShippingStatus", "DiffBackupStatus", "LogBackupStatus","DriveStatus","JobStatus","CollectionErrorStatus" ,"AGStatus","LastGoodCheckDBStatus","SnapshotAgeStatus","MemoryDumpStatus","UptimeStatus","CorruptionStatus","AlertStatus","FileFreeSpaceStatus"};
                foreach(string col in StatusColumns)
                {
                    dgvSummary.Rows[idx].Cells[col].Style.BackColor = DBAChecksStatus.GetStatusColour((DBAChecksStatus.DBAChecksStatusEnum)row[col]);
                }
                string uptimeString;
                Int32 uptime = (Int32)row["sqlserver_uptime"];
                Int32 addUptime = (Int32)row["AdditionalUptime"];
                if (uptime < 120)
                {
                    uptimeString = uptime.ToString() + " Mins (+" + addUptime.ToString() + "mins)";
                }
                else if (uptime < 1440)
                {
                    uptimeString = (uptime / 60).ToString("0") + " Hours  (+" + addUptime.ToString() + "mins)";
                }
                else
                {
                    uptimeString = (uptime / 1440).ToString("0") + " Days";
                }
                dgvSummary.Rows[idx].Cells["UptimeStatus"].Value = uptimeString;
                Int32 snapshotAgeMin = (Int32)row["SnapshotAgeMin"];
                Int32 snapshotAgeMax= (Int32)row["SnapshotAgeMax"];
                if (snapshotAgeMax == snapshotAgeMin)
                {
                    dgvSummary.Rows[idx].Cells["SnapshotAgeStatus"].Value = snapshotAgeMax + "Mins";
                }
                else
                {
                    dgvSummary.Rows[idx].Cells["SnapshotAgeStatus"].Value =snapshotAgeMin.ToString() + " to " + snapshotAgeMax.ToString() + "Mins";
                }
                if (row["DaysSinceLastGoodCheckDB"] != DBNull.Value)
                {
                    dgvSummary.Rows[idx].Cells["LastGoodCheckDBStatus"].Value = ((Int32)row["DaysSinceLastGoodCheckDB"]).ToString() + " Days";
                }
                if(row["OldestLastGoodCheckDBTime"] !=DBNull.Value && (DateTime)row["OldestLastGoodCheckDBTime"]  == DateTime.Parse("1900-01-01"))
                {
                    dgvSummary.Rows[idx].Cells["LastGoodCheckDBStatus"].Value = "Never";
                }
               



            }
        }

    }
}
