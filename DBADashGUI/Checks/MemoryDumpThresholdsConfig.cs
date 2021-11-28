using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            refreshData();
        }

        private void refreshData()
        {
            var thres = MemoryDumpThresholds.GetMemoryDumpThresholds();
            chkCritical.Checked = thres.MemoryDumpCriticalThresholdHrs != null;
            chkWarning.Checked = thres.MemoryDumpWarningThresholdHrs != null;
            numWarning.Value = thres.MemoryDumpWarningThresholdHrs ?? 0;
            numCritical.Value = thres.MemoryDumpCriticalThresholdHrs ?? 0;
            lblAckDate.Text = thres.MemoryDumpAckDate.ToString();
            lnkClear.Enabled = thres.MemoryDumpAckDate != null;
        }

        private void bttnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult= DialogResult.Cancel; 
        }

        private void update()
        {
            var thres = new MemoryDumpThresholds()
            {
                MemoryDumpWarningThresholdHrs = chkWarning.Checked ? Convert.ToInt32(numWarning.Value) : null,
                MemoryDumpCriticalThresholdHrs = chkCritical.Checked ? Convert.ToInt32(numCritical.Value) : null
            };
            thres.Save();
        }

        private void bttnUpdate_Click(object sender, EventArgs e)
        {
            update();
            this.DialogResult = DialogResult.OK;
        }

        private void chkCritical_CheckedChanged(object sender, EventArgs e)
        {
            numCritical.Enabled = chkCritical.Checked;
        }

        private void numWarning_CheckChanged(object sender, EventArgs e)
        {
            numWarning.Enabled = chkWarning.Checked;
        }

        private void lnkClear_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MemoryDumpThresholds.Acknowledge(true);
            this.DialogResult = DialogResult.OK;
        }

        private void lnkAcknowledge_Click(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MemoryDumpThresholds.Acknowledge();
            this.DialogResult=DialogResult.OK;
        }
    }
}
