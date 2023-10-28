using System;
using System.Windows.Forms;
using DBADashGUI.Theme;

namespace DBADashGUI.AgentJobs
{
    public partial class AgentJobThresholdsConfig : Form
    {
        public int InstanceID;
        public Guid JobID;
        public string connectionString;

        private void GetConfig()
        {
            var threshold = AgentJobThreshold.GetAgentJobThreshold(InstanceID, JobID, connectionString);
            chkFailCount24Hrs.Checked = threshold.FailCount24HrsCritical != null && threshold.FailCount24HrsWarning != null;
            chkFailCount7Days.Checked = threshold.FailCount7DaysCritical != null && threshold.FailCount7DaysWarning != null;
            chkJobStep24Hrs.Checked = threshold.JobStepFails24HrsCritical != null && threshold.JobStepFails24HrsWarning != null;
            chkJobStep7Days.Checked = threshold.JobStepFails7DaysCritical != null && threshold.JobStepFails7DaysWarning != null;
            chkTimeSinceLast.Checked = threshold.TimeSinceLastFailureCritical != null && threshold.TimeSinceLastFailureWarning != null;
            chkTimeSinceLastSucceeded.Checked = threshold.TimeSinceLastSucceededCritical != null && threshold.TimeSinceLastSucceededWarning != null;
            chkInherit.Checked = threshold.IsInherited;
            numTimeSinceLastCritical.Value = threshold.TimeSinceLastFailureCritical ?? 0;
            numTimeSinceLastWarning.Value = threshold.TimeSinceLastFailureWarning ?? 0;
            numJobStep7DaysCritical.Value = threshold.JobStepFails7DaysCritical ?? 0;
            numJobStep7DaysWarning.Value = threshold.JobStepFails7DaysWarning ?? 0;
            numFailCount7DaysCritical.Value = threshold.FailCount7DaysCritical ?? 0;
            numFailCount7DaysWarning.Value = threshold.FailCount7DaysWarning ?? 0;
            numJobStep24HrsCritical.Value = threshold.JobStepFails24HrsCritical ?? 0;
            numJobStep24HrsWarning.Value = threshold.JobStepFails24HrsWarning ?? 0;
            numFailCount24HrsCritical.Value = threshold.FailCount24HrsCritical ?? 0;
            numFailCount24HrsWarning.Value = threshold.FailCount24HrsWarning ?? 0;
            numTimeSinceLastSucceededCritical.Value = threshold.TimeSinceLastSucceededCritical ?? 0;
            numTimeSinceLastSucceededWarning.Value = threshold.TimeSinceLastSucceededWarning ?? 0;
            chkLastFailIsCritical.Checked = threshold.LastFailIsCritical;
            chkLastFailIsWarning.Checked = threshold.LastFailIsWarning;
        }

        public AgentJobThreshold Threshold
        {
            get
            {
                var threshold = new AgentJobThreshold
                {
                    InstanceID = InstanceID,
                    JobID = JobID
                };
                if (chkFailCount24Hrs.Checked)
                {
                    threshold.FailCount24HrsCritical = (int?)numFailCount24HrsCritical.Value;
                    threshold.FailCount24HrsWarning = (int?)numFailCount24HrsWarning.Value;
                }
                if (chkFailCount7Days.Checked)
                {
                    threshold.FailCount7DaysCritical = (int?)numFailCount7DaysCritical.Value;
                    threshold.FailCount7DaysWarning = (int?)numFailCount7DaysWarning.Value;
                }

                if (chkJobStep24Hrs.Checked)
                {
                    threshold.JobStepFails24HrsCritical = (int?)numJobStep24HrsCritical.Value;
                    threshold.JobStepFails24HrsWarning = (int?)numJobStep24HrsWarning.Value;
                }
                if (chkJobStep7Days.Checked)
                {
                    threshold.JobStepFails7DaysCritical = (int?)numJobStep7DaysCritical.Value;
                    threshold.JobStepFails7DaysWarning = (int?)numJobStep7DaysWarning.Value;
                }
                if (chkTimeSinceLast.Checked)
                {
                    threshold.TimeSinceLastFailureCritical = (int?)numTimeSinceLastCritical.Value;
                    threshold.TimeSinceLastFailureWarning = (int?)numTimeSinceLastWarning.Value;
                }
                if (chkTimeSinceLastSucceeded.Checked)
                {
                    threshold.TimeSinceLastSucceededCritical = (int?)numTimeSinceLastSucceededCritical.Value;
                    threshold.TimeSinceLastSucceededWarning = (int)numTimeSinceLastSucceededWarning.Value;
                }

                threshold.IsInherited = chkInherit.Checked;
                threshold.LastFailIsCritical = chkLastFailIsCritical.Checked;
                threshold.LastFailIsWarning = chkLastFailIsWarning.Checked;
                return threshold;
            }
        }

        public AgentJobThresholdsConfig()
        {
            InitializeComponent();
            this.ApplyTheme();
        }

        private void AgentJobThresholdsConfig_Load(object sender, EventArgs e)
        {
            GetConfig();
        }

        private void ChkInherit_CheckedChanged(object sender, EventArgs e)
        {
            pnlThresholds.Enabled = !chkInherit.Checked;
        }

        private void ChkFailCount24Hrs_CheckedChanged(object sender, EventArgs e)
        {
            numFailCount24HrsCritical.Enabled = chkFailCount24Hrs.Checked;
            numFailCount24HrsWarning.Enabled = chkFailCount24Hrs.Checked;
        }

        private void ChkFailCount7Days_CheckedChanged(object sender, EventArgs e)
        {
            numFailCount7DaysCritical.Enabled = chkFailCount7Days.Checked;
            numFailCount7DaysWarning.Enabled = chkFailCount7Days.Checked;
        }

        private void ChkJobStep24Hrs_CheckedChanged(object sender, EventArgs e)
        {
            numJobStep24HrsCritical.Enabled = chkJobStep24Hrs.Checked;
            numJobStep24HrsWarning.Enabled = chkJobStep24Hrs.Checked;
        }

        private void ChkJobStep7Days_CheckedChanged(object sender, EventArgs e)
        {
            numJobStep7DaysCritical.Enabled = chkJobStep7Days.Checked;
            numJobStep7DaysWarning.Enabled = chkJobStep7Days.Checked;
        }

        private void ChkTimeSinceLast_CheckedChanged(object sender, EventArgs e)
        {
            numTimeSinceLastCritical.Enabled = chkTimeSinceLast.Checked;
            numTimeSinceLastWarning.Enabled = chkTimeSinceLast.Checked;
        }

        private void ChkTimeSinceLastSucceeded_CheckedChanged(object sender, EventArgs e)
        {
            numTimeSinceLastSucceededCritical.Enabled = chkTimeSinceLastSucceeded.Checked;
            numTimeSinceLastSucceededWarning.Enabled = chkTimeSinceLastSucceeded.Checked;
        }

        private void BttnUpdate_Click(object sender, EventArgs e)
        {
            Threshold.Save(connectionString);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void ChkLastFailIsWarning_CheckedChanged(object sender, EventArgs e)
        {
            if (chkLastFailIsWarning.Checked)
            {
                chkLastFailIsCritical.Checked = false;
            }
        }

        private void ChkLastFailIsCritical_CheckedChanged(object sender, EventArgs e)
        {
            if (chkLastFailIsCritical.Checked)
            {
                chkLastFailIsWarning.Checked = false;
            }
        }
    }
}