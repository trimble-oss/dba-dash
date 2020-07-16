using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBAChecksGUI.AgentJobs
{
    public partial class AgentJobThresholdsConfig : Form
    {

        public Int32 InstanceID;
        public Guid JobID;
        public string connectionString;


        private void getConfig()
        {
            var threshold = AgentJobThreshold.GetAgentJobThreshold(InstanceID, JobID, connectionString);
            chkFailCount24Hrs.Checked = threshold.FailCount24HrsCritical != null && threshold.FailCount24HrsWarning != null;
            chkFailCount7Days.Checked = threshold.FailCount7DaysCritical != null && threshold.FailCount7DaysWarning != null;
            chkJobStep24Hrs.Checked = threshold.JobStepFails24HrsCritical != null && threshold.JobStepFails24HrsWarning != null;
            chkJobStep7Days.Checked = threshold.JobStepFails7DaysCritical != null && threshold.JobStepFails7DaysWarning != null;
            chkTimeSinceLast.Checked = threshold.TimeSinceLastFailureCritical != null && threshold.TimeSinceLastFailureWarning != null;
            chkTimeSinceLastSucceeded.Checked = threshold.TimeSinceLastSucceededCritical != null && threshold.TimeSinceLastSucceededWarning != null;
            chkInherit.Checked = threshold.IsInherited;
            numTimeSinceLastCritical.Value = threshold.TimeSinceLastFailureCritical==null ? 0 : (Int32)threshold.TimeSinceLastFailureCritical;
            numTimeSinceLastWarning.Value = threshold.TimeSinceLastFailureWarning == null ? 0 : (Int32)threshold.TimeSinceLastFailureWarning;
            numJobStep7DaysCritical.Value = threshold.JobStepFails7DaysCritical == null ? 0 : (Int32)threshold.JobStepFails7DaysCritical;
            numJobStep7DaysWarning.Value = threshold.JobStepFails7DaysWarning == null ? 0 : (Int32)threshold.JobStepFails7DaysWarning;
            numFailCount7DaysCritical.Value = threshold.FailCount7DaysCritical == null ? 0 : (Int32)threshold.FailCount7DaysCritical;
            numFailCount7DaysWarning.Value = threshold.FailCount7DaysWarning == null ? 0 : (Int32)threshold.FailCount7DaysWarning;
            numJobStep24HrsCritical.Value = threshold.JobStepFails24HrsCritical == null ? 0 : (Int32)threshold.JobStepFails24HrsCritical;
            numJobStep24HrsWarning.Value = threshold.JobStepFails24HrsWarning == null ? 0 : (Int32)threshold.JobStepFails24HrsWarning;
            numFailCount24HrsCritical.Value = threshold.FailCount24HrsCritical == null ? 0 : (Int32)threshold.FailCount24HrsCritical;
            numFailCount24HrsWarning.Value = threshold.FailCount24HrsWarning == null ? 0 : (Int32)threshold.FailCount24HrsWarning;
            numTimeSinceLastSucceededCritical.Value = threshold.TimeSinceLastSucceededCritical==null ? 0 : (Int32)threshold.TimeSinceLastSucceededCritical;
            numTimeSinceLastSucceededWarning.Value = threshold.TimeSinceLastSucceededWarning == null ? 0 : (Int32)threshold.TimeSinceLastSucceededWarning;
            chkLastFailIsCritical.Checked = threshold.LastFailIsCritical;
            chkLastFailIsWarning.Checked = threshold.LastFailIsWarning;
        }

        public AgentJobThreshold Threshold
        {
            get
            {
                var threshold = new AgentJobThreshold();
                threshold.InstanceID = InstanceID;
                threshold.JobID = JobID;
                if (chkFailCount24Hrs.Checked)
                {
                    threshold.FailCount24HrsCritical = (Int32?)numFailCount24HrsCritical.Value;
                    threshold.FailCount24HrsWarning = (Int32?)numFailCount24HrsWarning.Value; 
                }
                if (chkFailCount7Days.Checked)
                {
                    threshold.FailCount7DaysCritical = (Int32?)numFailCount7DaysCritical.Value;
                    threshold.FailCount7DaysWarning = (Int32?)numFailCount7DaysWarning.Value;
                }

                if (chkJobStep24Hrs.Checked) 
                {
                    threshold.JobStepFails24HrsCritical = (Int32?)numJobStep24HrsCritical.Value;
                    threshold.JobStepFails24HrsWarning = (Int32?)numJobStep24HrsWarning.Value;
                }
                if (chkJobStep7Days.Checked)
                {
                    threshold.JobStepFails7DaysCritical= (Int32?)numJobStep7DaysCritical.Value;
                    threshold.JobStepFails7DaysWarning =(Int32?)numJobStep7DaysWarning.Value;
                }
                if (chkTimeSinceLast.Checked) {
                    threshold.TimeSinceLastFailureCritical =(Int32?)numTimeSinceLastCritical.Value;
                    threshold.TimeSinceLastFailureWarning = (Int32?)numTimeSinceLastWarning.Value;
                }
                if (chkTimeSinceLastSucceeded.Checked) {
                    threshold.TimeSinceLastSucceededCritical = (Int32?)numTimeSinceLastSucceededCritical.Value;
                    threshold.TimeSinceLastSucceededWarning = (Int32)numTimeSinceLastSucceededWarning.Value;
                }
               
                threshold.IsInherited= chkInherit.Checked;
                threshold.LastFailIsCritical= chkLastFailIsCritical.Checked;
                threshold.LastFailIsWarning= chkLastFailIsWarning.Checked;
                return threshold;
            }
        }


        public AgentJobThresholdsConfig()
        {
            InitializeComponent();
        }

        private void AgentJobThresholdsConfig_Load(object sender, EventArgs e)
        {
            getConfig();
        }

        private void chkInherit_CheckedChanged(object sender, EventArgs e)
        {
            pnlThresholds.Enabled = !chkInherit.Checked;
        }

        private void chkFailCount24Hrs_CheckedChanged(object sender, EventArgs e)
        {
            numFailCount24HrsCritical.Enabled = chkFailCount24Hrs.Checked;
            numFailCount24HrsWarning.Enabled = chkFailCount24Hrs.Checked;
        }

        private void chkFailCount7Days_CheckedChanged(object sender, EventArgs e)
        {
            numFailCount7DaysCritical.Enabled = chkFailCount7Days.Checked;
            numFailCount7DaysWarning.Enabled = chkFailCount7Days.Checked;
        }

        private void chkJobStep24Hrs_CheckedChanged(object sender, EventArgs e)
        {
            numJobStep24HrsCritical.Enabled = chkJobStep24Hrs.Checked;
            numJobStep24HrsWarning.Enabled = chkJobStep24Hrs.Checked;
        }

        private void chkJobStep7Days_CheckedChanged(object sender, EventArgs e)
        {
            numJobStep7DaysCritical.Enabled = chkJobStep7Days.Checked;
            numJobStep7DaysWarning.Enabled = chkJobStep7Days.Checked;
        }

        private void chkTimeSinceLast_CheckedChanged(object sender, EventArgs e)
        {
            numTimeSinceLastCritical.Enabled = chkTimeSinceLast.Checked;
            numTimeSinceLastWarning.Enabled = chkTimeSinceLast.Checked;
        }

        private void chkTimeSinceLastSucceeded_CheckedChanged(object sender, EventArgs e)
        {
            numTimeSinceLastSucceededCritical.Enabled = chkTimeSinceLastSucceeded.Checked;
            numTimeSinceLastSucceededWarning.Enabled = chkTimeSinceLastSucceeded.Checked;
        }

        private void bttnUpdate_Click(object sender, EventArgs e)
        {
            Threshold.Save(connectionString);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void chkLastFailIsWarning_CheckedChanged(object sender, EventArgs e)
        {
            if (chkLastFailIsWarning.Checked) 
            {
                chkLastFailIsCritical.Checked = false; 
            }
        }

        private void chkLastFailIsCritical_CheckedChanged(object sender, EventArgs e)
        {
            if (chkLastFailIsCritical.Checked)
            {
                chkLastFailIsWarning.Checked = false;
            }
        }
    }
}
