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
                    fileThres.FileCheckType =    FileThreshold.FileCheckTypeEnum.MB;
                }
                if (OptDisabled.Checked)
                {
                    fileThres.FileCheckType = FileThreshold.FileCheckTypeEnum.None;
                }
                fileThres.PctMaxCheckEnabled = !chkMaxSizeDisable.Checked;
                fileThres.PctMaxSizeCriticalThreshold = numMaxSizeCritical.Value/100;
                fileThres.PctMaxSizeWarningThreshold = numMaxSizeWarning.Value/100;
                fileThres.ZeroAuthgrowthOnly = chkZeroAutogrowthOnly.Checked;
                return fileThres;
            }
            set
            {
                fileThres = value;
                var setValueByThres = fileThres;
                if (fileThres.InstanceID == -1)
                {
                    chkInherit.Enabled = false;
                    fileThres.Inherited = false;
                }
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
                optPercent.Checked = setValueByThres.FileCheckType==  FileThreshold.FileCheckTypeEnum.Percent;
                OptDisabled.Checked = setValueByThres.FileCheckType ==  FileThreshold.FileCheckTypeEnum.None;      
                numWarning.Value = setValueByThres.WarningThreshold;
                numCritical.Value = setValueByThres.CriticalThreshold;
                numMaxSizeCritical.Value = setValueByThres.PctMaxSizeCriticalThreshold>1 ? 100 : setValueByThres.PctMaxSizeCriticalThreshold * 100;
                numMaxSizeWarning.Value = setValueByThres.PctMaxSizeWarningThreshold>1 ? 100 : setValueByThres.PctMaxSizeWarningThreshold * 100;
                chkMaxSizeDisable.Checked = !setValueByThres.PctMaxCheckEnabled;
                chkZeroAutogrowthOnly.Checked = setValueByThres.ZeroAuthgrowthOnly;
                if (fileThres.FileCheckType ==  FileThreshold.FileCheckTypeEnum.Percent)
                {
                    numWarning.Value *= 100;
                    numCritical.Value *= 100;
                }
            }
        }

        private void bttnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }



        private void OptDisabled_CheckedChanged(object sender, EventArgs e)
        {
            numWarning.Enabled = !OptDisabled.Checked;
            numCritical.Enabled= !OptDisabled.Checked;
            chkZeroAutogrowthOnly.Enabled = !OptDisabled.Checked;
        }

        private void optMB_CheckedChanged(object sender, EventArgs e)
        {
            setThresholdType();
        }

        private void setThresholdType()
        {
            if (optMB.Checked)
            {
                lblDriveCritical.Text = "MB";
                lblDriveWarning.Text = "MB";
                numCritical.Maximum = Int32.MaxValue;
                numWarning.Maximum = Int32.MaxValue;
            }
            else
            {
                lblDriveCritical.Text = "%";
                lblDriveWarning.Text = "%";
                numCritical.Maximum = 100;
                numWarning.Maximum = 100;
            }
        }

        private void optPercent_CheckedChanged(object sender, EventArgs e)
        {
            setThresholdType();
        }

        private void bttnUpdate_Click(object sender, EventArgs e)
        {
            FileThreshold.UpdateThresholds();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void chkInherit_CheckedChanged(object sender, EventArgs e)
        {
            grpMaxSize.Enabled = !chkInherit.Checked;
            grpFreespace.Enabled = !chkInherit.Checked;
        }

        private void chkMaxSizeDisable_CheckedChanged(object sender, EventArgs e)
        {
            numMaxSizeCritical.Enabled = !chkMaxSizeDisable.Checked;
            numMaxSizeWarning.Enabled = !chkMaxSizeDisable.Checked;
            if(!chkMaxSizeDisable.Checked && numMaxSizeWarning.Value==0 && numMaxSizeCritical.Value == 0)
            {
                numMaxSizeCritical.Value = 90;
                numMaxSizeWarning.Value = 80;
            }
        }
    }
}
