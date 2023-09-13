using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using DBADashGUI.Theme;

namespace DBADashGUI.Performance
{
    public partial class PerformanceCounterThreshold : Form
    {
        public PerformanceCounterThreshold()
        {
            InitializeComponent();
            this.ApplyTheme();
        }

        private DataTable counters;
        private CounterThreshold threshold;
        public int? InstanceID;
        public string CounterName;
        public string ObjectName;
        public string CounterInstance;

        private void PerformanceCounterThreshold_Load(object sender, EventArgs e)
        {
            counters = CommonData.GetCounters();
            var objectNames = counters.AsEnumerable().Select(r => (string)r["object_name"]).Distinct().ToList();
            cboObject.DataSource = objectNames;
            cboInstances.DataSource = new BindingSource(CommonData.Instances, null);
            cboInstances.DisplayMember = "InstanceDisplayName";
            cboInstances.ValueMember = "InstanceID";

            chkAllInstances.Checked = InstanceID == null;
            cboInstances.SelectedValue = InstanceID;
            cboObject.Text = ObjectName;
            cboCounter.Text = CounterName;
            cboCounterInstance.Text = CounterInstance;

            foreach (var lnk in grpThresholds.Controls.OfType<LinkLabel>())
            {
                lnk.LinkColor = DashColors.LinkColor;
            }
        }

        private void CboObject_SelectedValueChanged(object sender, EventArgs e)
        {
            var counterNames = counters.AsEnumerable().Where(r => (string)r["object_name"] == cboObject.Text).Select(r => (string)r["counter_name"]).Distinct().ToList();
            cboCounter.DataSource = counterNames;
        }

        private void CboCounter_SelectedValueChanged(object sender, EventArgs e)
        {
            var instanceNames = counters.AsEnumerable().Where(r => (string)r["object_name"] == cboObject.Text && (string)r["counter_name"] == cboCounter.Text).Select(r => (string)r["instance_name"]).Distinct().ToList();
            cboCounterInstance.DataSource = instanceNames;
        }

        private void GetThresholds()
        {
            chkCritical.Text = chkAllInstances.Checked ? "System" : "Inherit";
            chkWarning.Text = chkCritical.Text;
            chkGood.Text = chkCritical.Text;
            threshold = CounterThreshold.GetCounterThreshold(cboObject.Text, cboCounter.Text, cboCounterInstance.Text, chkAllInstances.Checked ? null : Convert.ToInt32(cboInstances.SelectedValue));
            var inheritedThreshold = CounterThreshold.GetCounterThreshold(cboObject.Text, cboCounter.Text, cboCounterInstance.Text, null);
            if (threshold == null)
            {
                grpThresholds.Enabled = false;
            }
            else
            {
                grpThresholds.Enabled = true;
                numWarningFrom.Value = CoalesceDecimal(threshold.WarningFrom, inheritedThreshold.WarningFrom, inheritedThreshold.SystemWarningFrom);
                numWarningTo.Value = CoalesceDecimal(threshold.WarningTo, inheritedThreshold.WarningTo, inheritedThreshold.SystemWarningTo);
                numCriticalFrom.Value = CoalesceDecimal(threshold.CritialFrom, inheritedThreshold.CritialFrom, inheritedThreshold.SystemCritialFrom);
                numCriticalTo.Value = CoalesceDecimal(threshold.CritialTo, inheritedThreshold.CritialTo, inheritedThreshold.SystemCritialTo);
                numGoodFrom.Value = CoalesceDecimal(threshold.GoodFrom, inheritedThreshold.GoodFrom, inheritedThreshold.SystemGoodFrom);
                numGoodTo.Value = CoalesceDecimal(threshold.GoodTo, inheritedThreshold.CritialTo, inheritedThreshold.SystemGoodTo);
                chkWarning.Checked = !(threshold.WarningFrom.HasValue && threshold.WarningTo.HasValue);
                chkCritical.Checked = !(threshold.CritialFrom.HasValue && threshold.CritialTo.HasValue);
                chkGood.Checked = !(threshold.GoodFrom.HasValue && threshold.GoodTo.HasValue);
            }
        }

        private static decimal CoalesceDecimal(decimal? value1, decimal? value2, decimal? value3)
        {
            return value1.GetValueOrDefault(value2.GetValueOrDefault(value3.GetValueOrDefault()));
        }

        private void LnkCriticalToMax_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            numCriticalTo.Value = numCriticalTo.Maximum;
        }

        private void LnkWarningToMax_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            numWarningTo.Value = numWarningTo.Maximum;
        }

        private void LnkGoodToMax_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            numGoodTo.Value = numGoodTo.Maximum;
        }

        private void ChkCriticalEnabled_CheckedChanged(object sender, EventArgs e)
        {
            numCriticalFrom.Enabled = !chkCritical.Checked;
            numCriticalTo.Enabled = !chkCritical.Checked;
        }

        private void ChkWarningEnabled_CheckedChanged(object sender, EventArgs e)
        {
            numWarningFrom.Enabled = !chkWarning.Checked;
            numWarningTo.Enabled = !chkWarning.Checked;
        }

        private void ChkGoodEnabled_CheckedChanged(object sender, EventArgs e)
        {
            numGoodFrom.Enabled = !chkGood.Checked;
            numGoodTo.Enabled = !chkGood.Checked;
        }

        private void ChkAllInstances_CheckedChanged(object sender, EventArgs e)
        {
            cboInstances.Enabled = !chkAllInstances.Checked;
            GetThresholds();
        }


        private void CboCounterInstance_SelectedValueChanged(object sender, EventArgs e)
        {
            GetThresholds();
        }

        private void BttnUpdate_Click(object sender, EventArgs e)
        {
            if (threshold != null)
            {
                threshold.CritialFrom = chkCritical.Checked ? null : numCriticalFrom.Value;
                threshold.CritialTo = chkCritical.Checked ? null : numCriticalTo.Value;
                threshold.WarningFrom = chkWarning.Checked ? null : numWarningFrom.Value;
                threshold.WarningTo = chkWarning.Checked ? null : numWarningTo.Value;
                threshold.GoodFrom = chkGood.Checked ? null : numGoodFrom.Value;
                threshold.GoodTo = chkGood.Checked ? null : numGoodTo.Value;
                threshold.Update(chkUpdateAllInstances.Checked);
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                MessageBox.Show("Error - threshold is not available to set", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CboInstances_SelectedValueChanged(object sender, EventArgs e)
        {
            GetThresholds();
        }

        private void LnkDisableCritical_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            numCriticalFrom.Value = 0;
            numCriticalTo.Value = -1;
        }

        private void LnkDisableWarning_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            numWarningFrom.Value = 0;
            numWarningTo.Value = -1;
        }

        private void LnkDisableGood_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            numGoodFrom.Value = 0;
            numGoodTo.Value = -1;
        }
    }
}
