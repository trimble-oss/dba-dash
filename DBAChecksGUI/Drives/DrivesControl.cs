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

namespace DBAChecksGUI.Properties
{
    public partial class DrivesControl : UserControl
    {
        public DrivesControl()
        {
            InitializeComponent();
        }

        public List<Int32> InstanceIDs;
        public string ConnectionString;

        public bool IncludeCritical
        {
            get
            {
                return criticalToolStripMenuItem.Checked;
            }
            set
            {
                criticalToolStripMenuItem.Checked = value;
            }
        }

        public bool IncludeWarning
        {
            get
            {
                return warningToolStripMenuItem.Checked;
            }
            set
            {
                warningToolStripMenuItem.Checked = value;
            }
        }
        public bool IncludeNA
        {
            get
            {
                return undefinedToolStripMenuItem.Checked;
            }
            set
            {
                undefinedToolStripMenuItem.Checked = value;
            }
        }
        public bool IncludeOK
        {
            get
            {
                return OKToolStripMenuItem.Checked;
            }
            set
            {
                OKToolStripMenuItem.Checked = value;
            }
        }


        private void setLinkText(DriveThreshold drive,Label lbl)
        {
            if (drive == null)
            {
                lbl.Text = "{Not Configured}";
                return;
            }
            var pctGB = drive.DriveCheckType == Drive.DriveCheckTypeEnum.Percent ? "%" : "GB";
            var warning = drive.DriveCheckType == Drive.DriveCheckTypeEnum.Percent ? drive.WarningThreshold * 100 : drive.WarningThreshold;
            var critical = drive.DriveCheckType == Drive.DriveCheckTypeEnum.Percent ? drive.CriticalThreshold * 100 : drive.CriticalThreshold;
            if (drive.Inherited)
            {
                lbl.Text = "Inherited";
            }
            else if(drive.DriveCheckType == DriveThreshold.DriveCheckTypeEnum.None)
            {
                lbl.Text = "Disabled";
            }
            else
            {
                lbl.Text = string.Format("Warning: {0:0.0}{1}, Critical:{2:0.0}{1}", warning, pctGB, critical);
            }
        }


        public void RefreshData()
        {
            pnlDrives.Controls.Clear();

            SqlConnection cn = new SqlConnection(ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.Drives_Get", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("InstanceIDs", String.Join(",",InstanceIDs));
                cmd.Parameters.AddWithValue("IncludeCritical", IncludeCritical);
                cmd.Parameters.AddWithValue("IncludeWarning", IncludeWarning);
                cmd.Parameters.AddWithValue("IncludeNA", IncludeNA);
                cmd.Parameters.AddWithValue("IncludeOK", IncludeOK);
                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    var drv = new DriveControl();

                    drv.Drive.WarningThreshold = rdr["DriveWarningThreshold"] == DBNull.Value ? 0 : (decimal)rdr["DriveWarningThreshold"];
                    drv.Drive.CriticalThreshold = rdr["DriveCriticalThreshold"] == DBNull.Value ? 0 : (decimal)rdr["DriveCriticalThreshold"];

                    drv.Drive.DriveLabel = rdr["Label"] == DBNull.Value ? "" : (string)rdr["Label"];
                    drv.Drive.InstanceName = (string)rdr["Instance"];                   
                    drv.Drive.DriveLetter = (string)rdr["Name"];
                    drv.Drive.DriveCapacityGB = (decimal)rdr["TotalGB"];
                    drv.Drive.FreeSpaceGB = (decimal)rdr["FreeGB"];
                    drv.Drive.DriveStatus = (Drive.DriveStatusEnum)rdr["Status"];
                    drv.Drive.Inherited = (bool)rdr["IsInheritedThreshold"];
                    drv.Drive.InstanceID = (Int32)rdr["InstanceID"];
                    drv.Drive.DriveID = (Int32)rdr["DriveID"];
                    drv.Drive.DriveCheckTypeChar = char.Parse((string)rdr["DriveCheckType"]);
                    drv.DisplayInstanceName = InstanceIDs.Count > 1;
                    drv.Drive.ConnectionString = ConnectionString;
                    pnlDrives.Controls.Add(drv);
                    drv.Dock = DockStyle.Top;
                }
            }
            configureInstanceThresholdsToolStripMenuItem.Enabled = InstanceIDs.Count == 1;

        }

        public void Configure(Int32 InstanceID,Int32 DriveID)
        {
            var drv = DriveThreshold.GetDriveThreshold(InstanceID, DriveID,ConnectionString);
            var frm = new DriveThresholdConfig();
            frm.DriveThreshold = drv;
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                RefreshData();
            }
        }


        
        private void configureInstanceThresholdsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (InstanceIDs.Count == 1)
            {
                Configure(InstanceIDs[0], -1);
            }
        }

        private void configureRootThresholdsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Configure(-1, -1);
        }

        private void criticalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void warningToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void undefinedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void OKToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshData();
        }
    }
}
