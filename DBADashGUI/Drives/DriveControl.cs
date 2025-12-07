using DBADashGUI.Drives;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using DBADashGUI.DBFiles;
using DBADashGUI.Theme;
using static DBADashGUI.DBADashStatus;

namespace DBADashGUI
{
    public partial class DriveControl : UserControl, IThemedControl
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
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
            get => drive;
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

            pbSpace.Value = (int)drive.PercentUsedSpace;

            lblFree.Text = string.Format("{0:0.0} GB free ({2:0.0}%) of {1:0.0} GB", drive.FreeSpaceGB, drive.DriveCapacityGB, drive.PercentFreeSpace);
            var pctGB = drive.DriveCheckType == DriveThreshold.DriveCheckTypeEnum.Percent ? "%" : "GB";
            var warning = drive.DriveCheckType == DriveThreshold.DriveCheckTypeEnum.Percent ? drive.WarningThreshold * 100 : drive.WarningThreshold;
            var critical = drive.DriveCheckType == DriveThreshold.DriveCheckTypeEnum.Percent ? drive.CriticalThreshold * 100 : drive.CriticalThreshold;
            lblThresholds.Text = string.Format("Warning: {0:0.0}{1}, Critical:{2:0.0}{1}", warning, pctGB, critical);
            if (drive.DriveCheckType == DriveThreshold.DriveCheckTypeEnum.None)
            {
                lblThresholds.Text = "Disabled";
            }
            lblThresholds.Font = drive.Inherited ? new Font(lblThresholds.Font, FontStyle.Italic) : new Font(lblThresholds.Font, FontStyle.Bold);
            pbSpace.BackColor = drive.DriveStatus.GetBackColor();
            pbSpace.ForeColor = Color.White;
            if (drive.DriveStatus == DBADashStatusEnum.Critical)
            {
                picStatus.Image = Properties.Resources.StatusCriticalError_16x;
            }
            else if (drive.DriveStatus == DBADashStatusEnum.Warning)
            {
                picStatus.Image = Properties.Resources.StatusAnnotations_Warning_16xLG_color;
            }
            else if (drive.DriveStatus == DBADashStatusEnum.OK)
            {
                picStatus.Image = Properties.Resources.StatusOK_16x;
            }
            else if (drive.DriveStatus == DBADashStatusEnum.NA)
            {
                pbSpace.ForeColor = Color.White;
                pbSpace.BackColor = DashColors.TrimbleBlue;
            }
            picStatus.Visible = (drive.DriveStatus != DBADashStatusEnum.NA);

            lblUpdated.Text = "Updated " + drive.SnapshotDate.ToString("yyyy-MM-dd HH:mm") + " (" + DateHelper.AppNow.Subtract(drive.SnapshotDate).TotalMinutes.ToString("N0") + "min ago)";

            UpdateSnapshotStatus();
        }

        private void UpdateSnapshotStatus()
        {
            lblUpdated.ForeColor = drive.SnapshotStatus.GetBackColor();
            if (drive.SnapshotStatus == DBADashStatusEnum.NA)
            {
                lblUpdated.ForeColor = DBADashUser.SelectedTheme.ForegroundColor;
            }
        }

        private void DriveControl_Load(object sender, EventArgs e)
        {
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
            DriveHistoryView driveHistoryViewForm = new()
            {
                DriveID = Drive.DriveID,
                Text = Drive.InstanceName + " | " + Drive.DriveLetter + " " + Drive.DriveLabel
            };
            driveHistoryViewForm.ShowSingleInstance();
        }

        private void Files_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ShowFilesForDrive(Drive.DriveLetter, Drive.DriveLabel, Drive.InstanceID, Drive.InstanceName, this);
        }

        public static void ShowFilesForDrive(string driveLetter, string driveLabel, int instanceID, string instanceName, Control ctrl)
        {
            var frm = new Form()
            {
                Width = ctrl.Parent?.Width ?? 500,
                Height = ctrl.Parent?.Height ?? 500,
            };
            var context = CommonData.GetDBADashContext(instanceID);
            var files = new DBFilesControl()
            {
                Dock = DockStyle.Fill,
                FileLevel = true,
                IncludeCritical = true,
                IncludeWarning = true,
                IncludeOK = true,
                IncludeNA = true,
            };
            frm.Controls.Add(files);
            frm.ApplyTheme();
            frm.Load += (s, args) =>
            {
                files.SetContext(context, false, true);
                files.GridFilter = $"[physical_name] LIKE '{driveLetter}%'";
            };
            frm.Text = $@"DB Files on drive {driveLetter} ({driveLabel}) on {instanceName}";
            frm.ShowSingleInstance();
        }

        void IThemedControl.ApplyTheme(BaseTheme theme)
        {
            BackColor = theme.BackgroundColor;
            ForeColor = theme.ForegroundColor;
            lnkHistory.ApplyTheme(theme);
            lnkThreshold.ApplyTheme(theme);
            lnkFiles.ApplyTheme(theme);
            UpdateSnapshotStatus();
        }
    }
}