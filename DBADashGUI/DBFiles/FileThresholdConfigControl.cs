using System;
using System.Windows.Forms;

namespace DBADashGUI.DBFiles
{
    public partial class FileThresholdConfigControl : UserControl
    {
        public FileThresholdConfigControl()
        {
            InitializeComponent();
        }

        private FileThreshold fileThres = new();
        public FileThreshold FileThreshold
        {
            get
            {
                fileThres.Inherited = chkInherit.Checked;
                if (optPercent.Checked)
                {
                    fileThres.WarningThreshold = numWarning.Value / 100;
                    fileThres.CriticalThreshold = numCritical.Value / 100;
                    fileThres.FileCheckType = FileThreshold.FileCheckTypeEnum.Percent;
                }
                if (optMB.Checked)
                {
                    fileThres.WarningThreshold = numWarning.Value;
                    fileThres.CriticalThreshold = numCritical.Value;
                    fileThres.FileCheckType = FileThreshold.FileCheckTypeEnum.MB;
                }
                if (OptDisabled.Checked)
                {
                    fileThres.FileCheckType = FileThreshold.FileCheckTypeEnum.None;
                }
                fileThres.PctMaxCheckEnabled = !chkMaxSizeDisable.Checked;
                fileThres.PctMaxSizeCriticalThreshold = numMaxSizeCritical.Value / 100;
                fileThres.PctMaxSizeWarningThreshold = numMaxSizeWarning.Value / 100;
                fileThres.ZeroAuthgrowthOnly = chkZeroAutogrowthOnly.Checked;
                return fileThres;
            }
            set
            {
                fileThres = value;
                var setValueByThres = fileThres;

                chkInherit.Enabled = fileThres.InstanceID != -1;


                if (fileThres.Inherited)
                {
                    chkInherit.Checked = true;
                    var inherited = fileThres.GetInheritedThreshold();
                    setValueByThres = inherited;
                }
                else
                {
                    chkInherit.Checked = false;
                }


                optMB.Checked = setValueByThres.FileCheckType == FileThreshold.FileCheckTypeEnum.MB;
                optPercent.Checked = setValueByThres.FileCheckType == FileThreshold.FileCheckTypeEnum.Percent;
                OptDisabled.Checked = setValueByThres.FileCheckType == FileThreshold.FileCheckTypeEnum.None;
                numWarning.Value = setValueByThres.WarningThreshold;
                numCritical.Value = setValueByThres.CriticalThreshold;
                numMaxSizeCritical.Value = setValueByThres.PctMaxSizeCriticalThreshold > 1 ? 100 : setValueByThres.PctMaxSizeCriticalThreshold * 100;
                numMaxSizeWarning.Value = setValueByThres.PctMaxSizeWarningThreshold > 1 ? 100 : setValueByThres.PctMaxSizeWarningThreshold * 100;
                chkMaxSizeDisable.Checked = !setValueByThres.PctMaxCheckEnabled;
                chkZeroAutogrowthOnly.Checked = setValueByThres.ZeroAuthgrowthOnly;
                if (fileThres.FileCheckType == FileThreshold.FileCheckTypeEnum.Percent)
                {
                    numWarning.Value *= 100;
                    numCritical.Value *= 100;
                }
            }
        }


        private void OptDisabled_CheckedChanged(object sender, EventArgs e)
        {
            numWarning.Enabled = !OptDisabled.Checked;
            numCritical.Enabled = !OptDisabled.Checked;
            chkZeroAutogrowthOnly.Enabled = !OptDisabled.Checked;
        }


        private void SetThresholdType()
        {
            if (optMB.Checked)
            {
                lblDriveCritical.Text = "MB";
                lblDriveWarning.Text = "MB";
                numCritical.Maximum = int.MaxValue;
                numWarning.Maximum = int.MaxValue;
            }
            else
            {
                lblDriveCritical.Text = "%";
                lblDriveWarning.Text = "%";
                numCritical.Maximum = 100;
                numWarning.Maximum = 100;
            }
        }


        private void ChkInherit_CheckedChanged(object sender, EventArgs e)
        {
            grpMaxSize.Enabled = !chkInherit.Checked;
            grpFreespace.Enabled = !chkInherit.Checked;
        }

        private void ChkMaxSizeDisable_CheckedChanged(object sender, EventArgs e)
        {
            numMaxSizeCritical.Enabled = !chkMaxSizeDisable.Checked;
            numMaxSizeWarning.Enabled = !chkMaxSizeDisable.Checked;
            if (!chkMaxSizeDisable.Checked && numMaxSizeWarning.Value == 0 && numMaxSizeCritical.Value == 0)
            {
                numMaxSizeCritical.Value = 90;
                numMaxSizeWarning.Value = 80;
            }
        }

        private void OptMBPct_CheckedChanged(object sender, EventArgs e)
        {
            SetThresholdType();
        }
    }
}
