using System;
using System.Windows.Forms;
using DBADashGUI.Theme;

namespace DBADashGUI.LogShipping
{
    public partial class LogShippingThresholdsConfig : Form
    {
        public LogShippingThresholdsConfig()
        {
            InitializeComponent();
            this.ApplyTheme();
        }

        public int InstanceID;
        public int DatabaseID;

        private void ChkLRLatency_CheckedChanged(object sender, EventArgs e)
        {
            numLRLatencyCritical.Enabled = chkLRLatency.Checked;
            numLRLatencyWarning.Enabled = chkLRLatency.Checked;
        }

        private void ChkLRTimeSinceLast_CheckedChanged(object sender, EventArgs e)
        {
            numLRTimeSinceLastCritical.Enabled = chkLRTimeSinceLast.Checked;
            numLRTimeSinceLastWarning.Enabled = chkLRTimeSinceLast.Checked;
        }

        private void ChkLRInherit_CheckedChanged(object sender, EventArgs e)
        {
            pnlThresholds.Enabled = !chkLRInherit.Checked;
        }

        private void LogShippingThresholdsConfig_Load(object sender, EventArgs e)
        {
            var threshold = LogShippingThreshold.GetLogShippingThreshold(InstanceID, DatabaseID);
            Threshold = threshold;
        }

        public LogShippingThreshold Threshold
        {
            get
            {
                var threshold = new LogShippingThreshold
                {
                    InstanceID = InstanceID,
                    DatabaseID = DatabaseID,
                    Inherited = chkLRInherit.Checked,
                    NewDatabaseExcludePeriod = Convert.ToInt32(numExcludePeriod.Value)
                };
                if (chkLRLatency.Checked & !chkLRInherit.Checked)
                {
                    threshold.LatencyCriticalThreshold = (int?)numLRLatencyCritical.Value;
                    threshold.LatencyWarningThreshold = (int?)numLRLatencyWarning.Value;
                }
                if (chkLRTimeSinceLast.Checked & !chkLRInherit.Checked)
                {
                    threshold.TimeSinceLastCriticalThreshold = (int?)numLRTimeSinceLastCritical.Value;
                    threshold.TimeSinceLastWarningThreshold = (int?)numLRTimeSinceLastWarning.Value;
                }
                return threshold;
            }
            set
            {
                chkLRInherit.Checked = value.Inherited;
                chkLRTimeSinceLast.Checked = value.TimeSinceLastCriticalThreshold != null && value.TimeSinceLastWarningThreshold != null;
                chkExcludePeriod.Checked = value.NewDatabaseExcludePeriod > 0;
                numExcludePeriod.Value = value.NewDatabaseExcludePeriod;
                if (chkLRTimeSinceLast.Checked)
                {
                    numLRTimeSinceLastCritical.Value = (int)value.TimeSinceLastCriticalThreshold;
                    numLRTimeSinceLastWarning.Value = (int)value.TimeSinceLastWarningThreshold;
                }
                chkLRLatency.Checked = value.LatencyCriticalThreshold != null && value.LatencyWarningThreshold != null;
                if (chkLRLatency.Checked)
                {
                    numLRLatencyCritical.Value = (int)value.LatencyCriticalThreshold;
                    numLRLatencyWarning.Value = (int)value.LatencyWarningThreshold;
                }
                if (InstanceID == -1)
                {
                    chkLRInherit.Checked = false;
                    chkLRInherit.Enabled = false;
                }
            }
        }

        private void BttnUpdate_Click(object sender, EventArgs e)
        {
            Threshold.Save();
            DialogResult = DialogResult.OK;
        }

        private void ChkExcludePeriod_CheckedChanged(object sender, EventArgs e)
        {
            if (chkExcludePeriod.Checked && numExcludePeriod.Value == 0 || !chkExcludePeriod.Checked && numExcludePeriod.Value > 0)
            {
                numExcludePeriod.Value = chkExcludePeriod.Checked ? 1440 : 0;
            }
        }

        private void NumExcludePeriod_Validated(object sender, EventArgs e)
        {
            chkExcludePeriod.Checked = numExcludePeriod.Value > 0;
        }
    }
}
