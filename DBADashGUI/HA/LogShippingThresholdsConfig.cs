using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.LogShipping
{
    public partial class LogShippingThresholdsConfig : Form
    {
        public LogShippingThresholdsConfig()
        {
            InitializeComponent();
        }

        public Int32 InstanceID;
        public Int32 DatabaseID;

        private void chkLRLatency_CheckedChanged(object sender, EventArgs e)
        {
            numLRLatencyCritical.Enabled = chkLRLatency.Checked;
            numLRLatencyWarning.Enabled = chkLRLatency.Checked;
        }

        private void chkLRTimeSinceLast_CheckedChanged(object sender, EventArgs e)
        {
            numLRTimeSinceLastCritical.Enabled = chkLRTimeSinceLast.Checked;
            numLRTimeSinceLastWarning.Enabled = chkLRTimeSinceLast.Checked;
        }

        private void chkLRInherit_CheckedChanged(object sender, EventArgs e)
        {
            pnlThresholds.Enabled = !chkLRInherit.Checked;
        }

        private void LogShippingThresholdsConfig_Load(object sender, EventArgs e)
        {
            var threshold =  LogShippingThreshold.GetLogShippingThreshold(InstanceID, DatabaseID);
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
                    threshold.LatencyCriticalThreshold = (Int32?)numLRLatencyCritical.Value;
                    threshold.LatencyWarningThreshold = (Int32?)numLRLatencyWarning.Value;
                }
                if(chkLRTimeSinceLast.Checked & !chkLRInherit.Checked)
                {
                    threshold.TimeSinceLastCriticalThreshold = (Int32?)numLRTimeSinceLastCritical.Value;
                    threshold.TimeSinceLastWarningThreshold = (Int32?)numLRTimeSinceLastWarning.Value;
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
                    numLRTimeSinceLastCritical.Value = (Int32)value.TimeSinceLastCriticalThreshold;
                    numLRTimeSinceLastWarning.Value = (Int32)value.TimeSinceLastWarningThreshold;
                }
                chkLRLatency.Checked = value.LatencyCriticalThreshold != null && value.LatencyWarningThreshold != null;
                if (chkLRLatency.Checked)
                {
                    numLRLatencyCritical.Value = (Int32)value.LatencyCriticalThreshold;
                    numLRLatencyWarning.Value = (Int32)value.LatencyWarningThreshold;
                }
                if (InstanceID == -1)
                {
                    chkLRInherit.Checked = false;
                    chkLRInherit.Enabled = false;
                }
            }
        }

        private void bttnUpdate_Click(object sender, EventArgs e)
        {
            Threshold.Save();
            this.DialogResult = DialogResult.OK;
        }

        private void chkExcludePeriod_CheckedChanged(object sender, EventArgs e)
        {
            if (chkExcludePeriod.Checked && numExcludePeriod.Value == 0 || !chkExcludePeriod.Checked && numExcludePeriod.Value > 0)
            {
                numExcludePeriod.Value = chkExcludePeriod.Checked ? 1440 : 0;
            }
        }

        private void numExcludePeriod_Validated(object sender, EventArgs e)
        {
            chkExcludePeriod.Checked = numExcludePeriod.Value > 0;
        }
    }
}
