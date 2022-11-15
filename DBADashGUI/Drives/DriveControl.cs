using DBADashGUI.Drives;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using static DBADashGUI.DBADashStatus;

namespace DBADashGUI
{
    public partial class DriveControl : UserControl
    {

        public bool DisplayInstanceName { get; set; }

        public DriveControl()
        {
            InitializeComponent();
        }
        private Drive drive = new();

        [Category("Drive"),
          DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Drive Drive
        {
            get
            {
                return drive;
            }
            set
            {
                drive = value;
                SetDrive();
            }
        }

        private void SetDrive()
        {
            if (DisplayInstanceName)
            {
                lblDriveLabel.Text = drive.InstanceName + " | " + drive.DriveLabel + " (" + drive.DriveLetter + ")";
            }
            else
            {
                lblDriveLabel.Text = drive.DriveLabel + " (" + drive.DriveLetter + ")";
            }

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
            pbSpace.BackColor = drive.DriveStatus.GetColor();
            pbSpace.ForeColor = Color.White;
            if (drive.DriveStatus == DBADashStatusEnum.Critical)
            {
                picStatus.Image = imageList1.Images[0];
            }
            else if (drive.DriveStatus == DBADashStatusEnum.Warning)
            {
                picStatus.Image = imageList1.Images[1];
            }
            else if (drive.DriveStatus == DBADashStatusEnum.OK)
            {
                picStatus.Image = imageList1.Images[2];

            }
            else if (drive.DriveStatus == DBADashStatusEnum.NA)
            {
                pbSpace.ForeColor = Color.White;
                pbSpace.BackColor = DashColors.TrimbleBlue;
            }
            picStatus.Visible = (drive.DriveStatus != DBADashStatusEnum.NA);

            lblUpdated.Text = "Updated " + drive.SnapshotDate.ToString("yyyy-MM-dd HH:mm") + " (" + DateTime.Now.Subtract(drive.SnapshotDate).TotalMinutes.ToString("N0") + "min ago)";

            lblUpdated.ForeColor = drive.SnapshotStatus.GetColor();
            if (drive.SnapshotStatus == DBADashStatusEnum.NA)
            {
                lblUpdated.ForeColor = Color.Black;
            }
        }


        private void DriveControl_Load(object sender, EventArgs e)
        {
            lnkThreshold.LinkColor = DashColors.LinkColor;
            lnkHistory.LinkColor = DashColors.LinkColor;
            SetDrive();
        }

        private void LnkThreshold_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var frm = new DriveThresholdConfig
            {
                DriveThreshold = drive
            };
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                var thres = frm.DriveThreshold;
                Drive.WarningThreshold = thres.WarningThreshold;
                Drive.CriticalThreshold = thres.CriticalThreshold;
                drive.DriveCheckType = thres.DriveCheckType;
                drive.Inherited = thres.Inherited;
                drive.RefreshDriveStatus();
                SetDrive();
            }

        }

        private void LnkHistory_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var frm = new DriveHistoryView
            {
                DriveID = this.Drive.DriveID,
                Text = this.Drive.InstanceName + " | " + this.Drive.DriveLetter + " " + this.Drive.DriveLabel
            };
            frm.Show();
        }
    }
}
