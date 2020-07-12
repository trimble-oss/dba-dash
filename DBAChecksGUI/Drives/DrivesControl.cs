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

        private DriveThreshold rootDrive;
        private DriveThreshold instanceDrive;

        private void GetInheritedThresholds(string connectionString, Int32 InstanceID)
        {
            instanceDrive = null;
            rootDrive = null;

            rootDrive = getDriveThreshold(-1, -1, connectionString);
            instanceDrive = getDriveThreshold(InstanceID, -1, connectionString);

            setLinkText(rootDrive, lblRootThreshold);
            setLinkText(instanceDrive, lblInstanceThreshold);
  
        }

        private DriveThreshold getDriveThreshold(Int32 InstanceID,Int32 DriveID,string connectionString)
        {
            DriveThreshold drv = new DriveThreshold();
            drv.InstanceID = InstanceID;
            drv.DriveID = DriveID;
            drv.ConnectionString = connectionString;
            SqlConnection cn = new SqlConnection(connectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand(@"DriveThreshold_Get", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("DriveID", -1);
                var rdr = cmd.ExecuteReader();
                if (rdr.Read())
                {
                    drv.WarningThreshold = rdr["DriveWarningThreshold"] == DBNull.Value ? 0 : (decimal)rdr["DriveWarningThreshold"];
                    drv.CriticalThreshold = rdr["DriveCriticalThreshold"] == DBNull.Value ? 0 : (decimal)rdr["DriveCriticalThreshold"];
                    drv.Inherited = (bool)rdr["Inherited"];
                    drv.DriveCheckTypeChar = char.Parse((string)rdr["DriveCheckType"]);
                }
                else
                {
                    drv.Inherited = DriveID != -1;
                }
                return drv;
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


        public void LoadDrives(string connectionString,Int32 InstanceID)
        {
            pnlDrives.Controls.Clear();

            SqlConnection cn = new SqlConnection(connectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.Drives_Get", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    var drv = new DriveControl();

                    drv.Drive.WarningThreshold = rdr["DriveWarningThreshold"] == DBNull.Value ? 0 : (decimal)rdr["DriveWarningThreshold"];
                    drv.Drive.CriticalThreshold = rdr["DriveCriticalThreshold"] == DBNull.Value ? 0 : (decimal)rdr["DriveCriticalThreshold"];
                    drv.Drive.DriveLabel = rdr["Label"] == DBNull.Value ? "" : (string)rdr["Label"];
                    drv.Drive.DriveLetter = (string)rdr["Name"];
                    drv.Drive.DriveCapacityGB = (decimal)rdr["TotalGB"];
                    drv.Drive.FreeSpaceGB = (decimal)rdr["FreeGB"];
                    drv.Drive.DriveStatus = (Drive.DriveStatusEnum)rdr["Status"];
                    drv.Drive.Inherited = (bool)rdr["IsInheritedThreshold"];
                    drv.Drive.InstanceID = (Int32)rdr["InstanceID"];
                    drv.Drive.DriveID = (Int32)rdr["DriveID"];
                    drv.Drive.DriveCheckTypeChar = char.Parse((string)rdr["DriveCheckType"]);
                    drv.Drive.ConnectionString = connectionString;
                    pnlDrives.Controls.Add(drv);
                    drv.Dock = DockStyle.Top;
                }
                GetInheritedThresholds(connectionString, InstanceID);
            }

        }

        private void linkConfigureRoot_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var frm = new DriveThresholdConfig();
            frm.DriveThreshold = rootDrive;
            frm.ShowDialog();
            if(frm.DialogResult == DialogResult.OK)
            {
                LoadDrives(instanceDrive.ConnectionString, instanceDrive.InstanceID);
            }
        }

        private void lnkConfigureInstance_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var frm = new DriveThresholdConfig();
            frm.DriveThreshold = instanceDrive;
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                LoadDrives(instanceDrive.ConnectionString, instanceDrive.InstanceID);
            }
        }
    }
}
