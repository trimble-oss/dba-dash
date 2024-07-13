using System;
using System.Windows.Forms;

namespace DBADashGUI.Checks
{
    public partial class MemoryDumpThresholdsConfig : Form
    {
        public MemoryDumpThresholdsConfig()
        {
            InitializeComponent();
        }

        private void MemoryDumpThresholdsConfig_Load(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void RefreshData()
        {
            var thres = MemoryDumpThresholds.GetMemoryDumpThresholds();
            chkCritical.Checked = thres.MemoryDumpCriticalThresholdHrs != null;
            chkWarning.Checked = thres.MemoryDumpWarningThresholdHrs != null;
            numWarning.Value = thres.MemoryDumpWarningThresholdHrs ?? 0;
            numCritical.Value = thres.MemoryDumpCriticalThresholdHrs ?? 0;
            lblAckDate.Text = thres.MemoryDumpAckDate.ToString();
            lnkClear.Enabled = thres.MemoryDumpAckDate != null;
        }

        private void BttnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void UpdateThresholds()
        {
            var thres = new MemoryDumpThresholds()
            {
                MemoryDumpWarningThresholdHrs = chkWarning.Checked ? Convert.ToInt32(numWarning.Value) : null,
                MemoryDumpCriticalThresholdHrs = chkCritical.Checked ? Convert.ToInt32(numCritical.Value) : null
            };
            thres.Save();
        }

        private void BttnUpdate_Click(object sender, EventArgs e)
        {
            UpdateThresholds();
            DialogResult = DialogResult.OK;
        }

        private void ChkCritical_CheckedChanged(object sender, EventArgs e)
        {
            numCritical.Enabled = chkCritical.Checked;
        }

        private void NumWarning_CheckChanged(object sender, EventArgs e)
        {
            numWarning.Enabled = chkWarning.Checked;
        }

        private void LnkClear_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MemoryDumpThresholds.Acknowledge(true);
            DialogResult = DialogResult.OK;
        }

        private void LnkAcknowledge_Click(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MemoryDumpThresholds.Acknowledge();
            DialogResult = DialogResult.OK;
        }
    }
}
