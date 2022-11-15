using System;
using System.Windows.Forms;

namespace DBADashGUI
{
    public partial class DriveThresholdConfig : Form
    {
        public DriveThresholdConfig()
        {
            InitializeComponent();
        }

        private DriveThreshold driveThres;
        public DriveThreshold DriveThreshold
        {
            get
            {
                driveThres.Inherited = optInherit.Checked;
                if (optPercent.Checked)
                {
                    driveThres.WarningThreshold = numDriveWarning.Value / 100;
                    driveThres.CriticalThreshold = numDriveCritical.Value / 100;
                    driveThres.DriveCheckType = Drive.DriveCheckTypeEnum.Percent;
                }
                if (optGB.Checked)
                {
                    driveThres.WarningThreshold = numDriveWarning.Value;
                    driveThres.CriticalThreshold = numDriveCritical.Value;
                    driveThres.DriveCheckType = Drive.DriveCheckTypeEnum.GB;
                }
                if (OptDisabled.Checked)
                {
                    driveThres.DriveCheckType = Drive.DriveCheckTypeEnum.None;
                }

                return driveThres;
            }
            set
            {
                driveThres = value;
                if (driveThres.InstanceID == -1)
                {
                    optInherit.Enabled = false;
                    driveThres.Inherited = false;
                }
                if (!driveThres.Inherited)
                {
                    optGB.Checked = driveThres.DriveCheckType == Drive.DriveCheckTypeEnum.GB;
                    optPercent.Checked = driveThres.DriveCheckType == Drive.DriveCheckTypeEnum.Percent;
                    OptDisabled.Checked = driveThres.DriveCheckType == Drive.DriveCheckTypeEnum.None;
                }
                else
                {
                    optInherit.Checked = true;
                }
                numDriveWarning.Value = driveThres.WarningThreshold;
                numDriveCritical.Value = driveThres.CriticalThreshold;
                if (driveThres.DriveCheckType == Drive.DriveCheckTypeEnum.Percent)
                {
                    numDriveWarning.Value *= 100;
                    numDriveCritical.Value *= 100;
                }
            }
        }

        private void BttnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void OptInherit_CheckedChanged(object sender, EventArgs e)
        {
            pnlThresholds.Enabled = false;
        }

        private void OptDisabled_CheckedChanged(object sender, EventArgs e)
        {
            pnlThresholds.Enabled = false;
        }

        private void OptGB_CheckedChanged(object sender, EventArgs e)
        {
            pnlThresholds.Enabled = true;
            lblDriveCritical.Text = "GB";
            lblDriveWarning.Text = "GB";
            numDriveCritical.Maximum = Int32.MaxValue;
            numDriveWarning.Maximum = Int32.MaxValue;
        }

        private void OptPercent_CheckedChanged(object sender, EventArgs e)
        {
            pnlThresholds.Enabled = true;
            lblDriveCritical.Text = "%";
            lblDriveWarning.Text = "%";
            numDriveCritical.Maximum = 100;
            numDriveWarning.Maximum = 100;
        }

        private void BttnUpdate_Click(object sender, EventArgs e)
        {
            DriveThreshold.UpdateThresholds();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void DriveThresholdConfig_Load(object sender, EventArgs e)
        {

        }
    }
}
