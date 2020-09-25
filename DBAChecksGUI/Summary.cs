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

        DataView dv;

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
                dv = new DataView(dt);
                dgvSummary.DataSource = dv;
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
                    uptimeString = (uptime / 1440).ToString("0") + " days";
                }
                dgvSummary.Rows[idx].Cells["UptimeStatus"].Value = uptimeString;
                Int32 snapshotAgeMin = (Int32)row["SnapshotAgeMin"];
                Int32 snapshotAgeMax= (Int32)row["SnapshotAgeMax"];
                if (snapshotAgeMax == snapshotAgeMin)
                {
                    dgvSummary.Rows[idx].Cells["SnapshotAgeStatus"].Value = snapshotAgeMax + "mins";
                }
                else
                {
                    dgvSummary.Rows[idx].Cells["SnapshotAgeStatus"].Value =snapshotAgeMin.ToString() + " to " + snapshotAgeMax.ToString() + "mins";
                }
                if (row["DaysSinceLastGoodCheckDB"] != DBNull.Value)
                {
                    dgvSummary.Rows[idx].Cells["LastGoodCheckDBStatus"].Value = ((Int32)row["DaysSinceLastGoodCheckDB"]).ToString() + " days";
                }
                string oldestLastGoodCheckDB="Unknown";
                if(row["OldestLastGoodCheckDBTime"] !=DBNull.Value)
                {
                    if((DateTime)row["OldestLastGoodCheckDBTime"] == DateTime.Parse("1900-01-01"))
                    {
                        oldestLastGoodCheckDB = "Never";
                        dgvSummary.Rows[idx].Cells["LastGoodCheckDBStatus"].Value = "Never";
                    }
                    else
                    {
                        oldestLastGoodCheckDB = ((DateTime)row["OldestLastGoodCheckDBTime"]).ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                    }

                }
                
                dgvSummary.Rows[idx].Cells["LastGoodCheckDBStatus"].ToolTipText = "Last Good CheckDB Critical:" + (Int32)row["LastGoodCheckDBCriticalCount"] + Environment.NewLine +
                                                                               "Last Good CheckDB Warning:" + (Int32)row["LastGoodCheckDBWarningCount"] + Environment.NewLine +
                                                                               "Last Good CheckDB Good:" + (Int32)row["LastGoodCheckDBHealthyCount"] + Environment.NewLine +
                                                                               "Last Good CheckDB NA:" + (Int32)row["LastGoodCheckDBNACount"] + Environment.NewLine +
                                                                               "Oldest Last Good CheckDB:" + oldestLastGoodCheckDB;
                                                                               ;
                if (row["LastMemoryDump"]!= DBNull.Value)
                {
                    DateTime lastMemoryDump = (DateTime)row["LastMemoryDump"];
                    Int32 memoryDumpCount = (Int32)row["MemoryDumpCount"];
                    var lastMemoryDumpHrs = DateTime.UtcNow.Subtract(lastMemoryDump).TotalHours;
                    var lastMemoryDumpDays = DateTime.UtcNow.Subtract(lastMemoryDump).TotalDays;
                    if (DateTime.UtcNow.Subtract(lastMemoryDump).TotalHours< 24)
                    {
                        dgvSummary.Rows[idx].Cells["MemoryDumpStatus"].Value = lastMemoryDumpHrs.ToString("0") + "hrs";
                    }
                    else
                    {
                        dgvSummary.Rows[idx].Cells["MemoryDumpStatus"].Value = lastMemoryDumpDays.ToString("0") + " days";
                    }
                    dgvSummary.Rows[idx].Cells["MemoryDumpStatus"].ToolTipText = "Last Memory Dump:" + lastMemoryDump.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine + "Total Memory Dumps:" + memoryDumpCount;


                }
                string lastAlert = "Never";
                string lastAlertDays = "Never";
                string lastCriticalAlert = "Never";
                if (row["LastAlert"] != DBNull.Value)
                {
                    DateTime lastAlertD = (DateTime)row["LastAlert"];
                    lastAlert = lastAlertD.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                    if(DateTime.UtcNow.Subtract(lastAlertD).TotalHours < 24)
                    {
                        lastAlertDays = DateTime.UtcNow.Subtract(lastAlertD).TotalHours.ToString("0") + "hrs";
                    }
                    else
                    {
                        lastAlertDays = DateTime.UtcNow.Subtract(lastAlertD).TotalDays.ToString("0") + " days";
                    }
                }
               if(row["LastCritical"] != DBNull.Value)
                {
                    lastCriticalAlert = (((DateTime)row["LastCritical"]).ToLocalTime()).ToString("yyyy-MM-dd HH:mm");
                }
                Int32 totalAlerts = row["TotalAlerts"] == DBNull.Value ? 0 : (Int32)row["TotalAlerts"];

                dgvSummary.Rows[idx].Cells["AlertStatus"].Value = lastAlertDays;
                dgvSummary.Rows[idx].Cells["AlertStatus"].ToolTipText = "Last Alert:" + lastAlert + Environment.NewLine + 
                                                                        "Last Critical Alert:" + lastCriticalAlert + Environment.NewLine + 
                                                                        "Total Alerts:" + totalAlerts;


            }
        }

        private void dgvSummary_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            var sortCol = "";
            if(dgvSummary.Columns[e.ColumnIndex] == FullBackupStatus)
            {
                sortCol = "FullBackupStatus";
            }
            if(dgvSummary.Columns[e.ColumnIndex] == AlertStatus)
            {
                sortCol = "AlertStatus";
            }
            if (dgvSummary.Columns[e.ColumnIndex] == SnapshotAgeStatus)
            {
                sortCol = "SnapshotAgeMax";
            }
            if (dgvSummary.Columns[e.ColumnIndex] == JobStatus)
            {
                sortCol = "JobStatus";
            }
            if (dgvSummary.Columns[e.ColumnIndex] == AGStatus)
            {
                sortCol = "AGStatus";
            }
            if (dgvSummary.Columns[e.ColumnIndex] == CorruptionStatus)
            {
                sortCol = "CorruptionStatus";
            }
            if (dgvSummary.Columns[e.ColumnIndex] == UptimeStatus)
            {
                sortCol = "sqlserver_uptime";
            }
            if (dgvSummary.Columns[e.ColumnIndex] == DriveStatus)
            {
                sortCol = "DriveStatus";
            }
            if (dgvSummary.Columns[e.ColumnIndex] == MemoryDumpStatus)
            {
                sortCol = "LastMemoryDump";
            }
            if (dgvSummary.Columns[e.ColumnIndex] == LastGoodCheckDBStatus )
            {
                sortCol = "LastGoodCheckDBStatus";
            }
            if (dgvSummary.Columns[e.ColumnIndex] == DiffBackupStatus)
            {
                sortCol = "DiffBackupStatus";
            }
            if (dgvSummary.Columns[e.ColumnIndex] == LogBackupStatus)
            {
                sortCol = "LogBackupStatus";
            }
            if (dgvSummary.Columns[e.ColumnIndex] == LogShippingStatus)
            {
                sortCol = "LogShippingStatus";
            }
            if (dgvSummary.Columns[e.ColumnIndex] == FileFreeSpaceStatus)
            {
                sortCol = "FileFreeSpaceStatus";
            }
            if (dgvSummary.Columns[e.ColumnIndex] == CollectionErrorStatus)
            {
                sortCol = "CollectionErrorStatus";
            }
            if (sortCol != "")
            {
                if (dv.Sort == sortCol) {
                    dv.Sort = sortCol += " DESC";
                }
                else
                {
                    dv.Sort = sortCol;
                }
            }
        }

        private void tsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void tsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgvSummary);
        }
    }
}
