using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.DBFiles
{
    public partial class FileThresholdConfig : Form
    {
        public FileThresholdConfig()
        {
            InitializeComponent();
        }
        private FileThreshold fileThres;
        public FileThreshold FileThreshold
        {
            get
            {
                fileThres.Inherited = optInherit.Checked;
                if (optPercent.Checked)
                {
                    fileThres.WarningThreshold = numDriveWarning.Value / 100;
                    fileThres.CriticalThreshold = numDriveCritical.Value / 100;
                    fileThres.FileCheckType = FileThreshold.FileCheckTypeEnum.Percent;
                }
                if (optMB.Checked)
                {
                    fileThres.WarningThreshold = numDriveWarning.Value;
                    fileThres.CriticalThreshold = numDriveCritical.Value;
                    fileThres.FileCheckType =    FileThreshold.FileCheckTypeEnum.MB;
                }
                if (OptDisabled.Checked)
                {
                    fileThres.FileCheckType = FileThreshold.FileCheckTypeEnum.None;
                }

                return fileThres;
            }
            set
            {
                fileThres = value;
                if (fileThres.InstanceID == -1)
                {
                    optInherit.Enabled = false;
                    fileThres.Inherited = false;
                }
                if (!fileThres.Inherited)
                {
                    optMB.Checked = fileThres.FileCheckType == FileThreshold.FileCheckTypeEnum.MB;
                    optPercent.Checked = fileThres.FileCheckType==  FileThreshold.FileCheckTypeEnum.Percent;
                    OptDisabled.Checked = fileThres.FileCheckType ==  FileThreshold.FileCheckTypeEnum.None;
                }
                else
                {
                    optInherit.Checked = true;
                }
                numDriveWarning.Value = fileThres.WarningThreshold;
                numDriveCritical.Value = fileThres.CriticalThreshold;
                if (fileThres.FileCheckType ==  FileThreshold.FileCheckTypeEnum.Percent)
                {
                    numDriveWarning.Value *= 100;
                    numDriveCritical.Value *= 100;
                }
            }
        }

        private void bttnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void optInherit_CheckedChanged(object sender, EventArgs e)
        {
            pnlThresholds.Enabled = false;
        }

        private void OptDisabled_CheckedChanged(object sender, EventArgs e)
        {
            pnlThresholds.Enabled = false;
        }

        private void optMB_CheckedChanged(object sender, EventArgs e)
        {
            pnlThresholds.Enabled = true;
            lblDriveCritical.Text = "MB";
            lblDriveWarning.Text = "MB";
            numDriveCritical.Maximum = Int32.MaxValue;
            numDriveWarning.Maximum = Int32.MaxValue;
        }

        private void optPercent_CheckedChanged(object sender, EventArgs e)
        {
            pnlThresholds.Enabled = true;
            lblDriveCritical.Text = "%";
            lblDriveWarning.Text = "%";
            numDriveCritical.Maximum = 100;
            numDriveWarning.Maximum = 100;
        }

        private void bttnUpdate_Click(object sender, EventArgs e)
        {
            FileThreshold.UpdateThresholds();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

    }
}
