using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using DBAChecksGUI.Drives;

namespace DBAChecksGUI
{
    public partial class DriveControl : UserControl
    {

        public bool DisplayInstanceName { get; set; }

        public DriveControl()
        {
            InitializeComponent();
        }
        private Drive drive=new Drive();

        [Category("Drive"),
          DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Drive Drive { 
            get {
                return drive;
            } set { 
                drive = value;
                setDrive();
            }
        }

        private void setDrive()
        {
            if (DisplayInstanceName)
            {
                lblDriveLabel.Text = drive.InstanceName + " | " + drive.DriveLabel + " (" + drive.DriveLetter + ")";
            }
            else
            {
                lblDriveLabel.Text = drive.DriveLabel + " (" + drive.DriveLetter + ")";
            }

            var pct = drive.PercentFreeSpace;

            pbSpace.Value = (Int32)drive.PercentUsedSpace;

            lblFree.Text = String.Format("{0:0.0} GB free ({2:0.0}%) of {1:0.0} GB", drive.FreeSpaceGB, drive.DriveCapacityGB, drive.PercentFreeSpace);
            var pctGB = drive.DriveCheckType == Drive.DriveCheckTypeEnum.Percent ? "%" : "GB";
            var warning = drive.DriveCheckType == Drive.DriveCheckTypeEnum.Percent ? drive.WarningThreshold * 100 : drive.WarningThreshold;
            var critical = drive.DriveCheckType == Drive.DriveCheckTypeEnum.Percent ? drive.CriticalThreshold * 100 : drive.CriticalThreshold;
            lblThresholds.Text = string.Format("Warning: {0:0.0}{1}, Critical:{2:0.0}{1}", warning, pctGB, critical);
            if (drive.DriveCheckType == Drive.DriveCheckTypeEnum.None)
            {
                lblThresholds.Text = "Disabled";
            }
            lblThresholds.Font = drive.Inherited ? new Font(lblThresholds.Font, FontStyle.Italic) : new Font(lblThresholds.Font, FontStyle.Bold);
            if (drive.DriveStatus == Drive.StatusEnum.Critical)
            {
                picStatus.Image = imageList1.Images[0];

                pbSpace.ForeColor = Color.FromArgb(255, 192, 192);
                pbSpace.BackColor = Color.Red;
            }
            if (drive.DriveStatus == Drive.StatusEnum.Warning)
            {
                pbSpace.ForeColor = Color.White;
                pbSpace.BackColor = Color.Yellow;
                picStatus.Image = imageList1.Images[1];

            }
            if (drive.DriveStatus == Drive.StatusEnum.OK)
            {
                picStatus.Image = imageList1.Images[2];
                pbSpace.ForeColor = Color.LightGreen;
                pbSpace.BackColor = Color.Green;

            }
            if (drive.DriveStatus == Drive.StatusEnum.NA)
            {
                pbSpace.ForeColor = Color.LightBlue;
                pbSpace.BackColor = Color.RoyalBlue;
            }
            picStatus.Visible = (drive.DriveStatus != Drive.StatusEnum.NA);

            lblUpdated.Text = "Updated " + drive.SnapshotDate.ToString("yyyy-MM-dd HH:mm") + " (" + DateTime.Now.Subtract(drive.SnapshotDate).TotalMinutes.ToString("N0") + "min ago)";
            if (drive.SnapshotStatus == Drive.StatusEnum.Critical)
            {
                lblUpdated.ForeColor = Color.Red;
            }
            else if (drive.SnapshotStatus == Drive.StatusEnum.Warning)
            {
                lblUpdated.ForeColor = Color.Orange;
            }
            else if(drive.SnapshotStatus == Drive.StatusEnum.NA)
            {
                lblUpdated.ForeColor = Color.Black;
            }
            else
            {
                lblUpdated.ForeColor = Color.Green;
            }
        }


        private void DriveControl_Load(object sender, EventArgs e)
        {
            setDrive();
        }

        private void lnkThreshold_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var frm = new DriveThresholdConfig();
            frm.DriveThreshold = drive;
            frm.ShowDialog();
            if(frm.DialogResult== DialogResult.OK)
            {
                var thres = frm.DriveThreshold;
                Drive.WarningThreshold = thres.WarningThreshold;
                Drive.CriticalThreshold = thres.CriticalThreshold;
                drive.DriveCheckType = thres.DriveCheckType;
                drive.Inherited = thres.Inherited;
                drive.RefreshDriveStatus();
                setDrive();
            }

        }

        private void lnkHistory_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var frm = new DriveHistoryView();
            frm.DriveID = this.Drive.DriveID;
            frm.Text = this.Drive.InstanceName + " | " + this.Drive.DriveLetter + " " + this.Drive.DriveLabel;
            frm.Show();
        }
    }
}
