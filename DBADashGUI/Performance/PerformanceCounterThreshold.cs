using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    public partial class PerformanceCounterThreshold : Form
    {
        public PerformanceCounterThreshold()
        {
            InitializeComponent();
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
            var objectNames= counters.AsEnumerable().Select(r => (string)r["object_name"]).Distinct().ToList();
            cboObject.DataSource = objectNames;
            cboInstances.DataSource = new BindingSource(CommonData.Instances,null);   
            cboInstances.DisplayMember = "InstanceDisplayName";
            cboInstances.ValueMember = "InstanceID";

            chkAllInstances.Checked = InstanceID == null;
            cboInstances.SelectedValue = InstanceID;
            cboObject.Text = ObjectName;
            cboCounter.Text = CounterName;       
            cboCounterInstance.Text = CounterInstance;
        }

        private void cboObject_SelectedValueChanged(object sender, EventArgs e)
        {
            var counterNames = counters.AsEnumerable().Where(r=>(string)r["object_name"]==cboObject.Text).Select(r => (string)r["counter_name"]).Distinct().ToList();
            cboCounter.DataSource = counterNames;
        }

        private void cboCounter_SelectedValueChanged(object sender, EventArgs e)
        {
            var instanceNames = counters.AsEnumerable().Where(r => (string)r["object_name"] == cboObject.Text && (string)r["counter_name"]==cboCounter.Text).Select(r => (string)r["instance_name"]).Distinct().ToList();
            cboCounterInstance.DataSource = instanceNames;
        }

        private void getThresholds()
        {
            chkCritical.Text = chkAllInstances.Checked ? "Disable" : "Inherit";
            chkWarning.Text = chkCritical.Text;
            chkGood.Text = chkCritical.Text;
            threshold = CounterThreshold.GetCounterThreshold(cboObject.Text, cboCounter.Text, cboCounterInstance.Text, chkAllInstances.Checked ? null : Convert.ToInt32(cboInstances.SelectedValue));
            var inheritedThreshold = CounterThreshold.GetCounterThreshold(cboObject.Text, cboCounter.Text, cboCounterInstance.Text,null);
            if (threshold == null)
            {
                grpThresholds.Enabled = false;
            }
            else
            {
                grpThresholds.Enabled = true;
                numWarningFrom.Value = threshold.WarningFrom.GetValueOrDefault(inheritedThreshold.WarningFrom.GetValueOrDefault());
                numWarningTo.Value = threshold.WarningTo.GetValueOrDefault(inheritedThreshold.WarningTo.GetValueOrDefault());
                numCriticalFrom.Value = threshold.CritialFrom.GetValueOrDefault(inheritedThreshold.CritialFrom.GetValueOrDefault());
                numCriticalTo.Value = threshold.CritialTo.GetValueOrDefault(inheritedThreshold.CritialTo.GetValueOrDefault());
                numGoodFrom.Value = threshold.GoodFrom.GetValueOrDefault(inheritedThreshold.GoodFrom.GetValueOrDefault());
                numGoodTo.Value = threshold.GoodTo.GetValueOrDefault(inheritedThreshold.CritialTo.GetValueOrDefault());
                chkWarning.Checked = !(threshold.WarningFrom.HasValue && threshold.WarningTo.HasValue);
                chkCritical.Checked = !(threshold.CritialFrom.HasValue && threshold.CritialTo.HasValue);
                chkGood.Checked = !(threshold.GoodFrom.HasValue && threshold.GoodTo.HasValue);
            }
        }

        private void lnkCriticalToMax_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            numCriticalTo.Value = numCriticalTo.Maximum;
        }

        private void lnkWarningToMax_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            numWarningTo.Value = numWarningTo.Maximum;
        }

        private void lnkGoodToMax_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            numGoodTo.Value = numGoodTo.Maximum;
        }

        private void chkCriticalEnabled_CheckedChanged(object sender, EventArgs e)
        {
            numCriticalFrom.Enabled = !chkCritical.Checked;
            numCriticalTo.Enabled = !chkCritical.Checked;
        }

        private void chkWarningEnabled_CheckedChanged(object sender, EventArgs e)
        {
            numWarningFrom.Enabled = !chkWarning.Checked;
            numWarningTo.Enabled= !chkWarning.Checked;
        }

        private void chkGoodEnabled_CheckedChanged(object sender, EventArgs e)
        {
            numGoodFrom.Enabled = !chkGood.Checked;
            numGoodTo.Enabled=!chkGood.Checked;
        }

        private void chkAllInstances_CheckedChanged(object sender, EventArgs e)
        {
            cboInstances.Enabled = !chkAllInstances.Checked;
            getThresholds();
        }


        private void cboCounterInstance_SelectedValueChanged(object sender, EventArgs e)
        {
            getThresholds();
        }

        private void bttnUpdate_Click(object sender, EventArgs e)
        {
            if (threshold != null)
            {
                threshold.CritialFrom = chkCritical.Checked ? null : numCriticalFrom.Value ;
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

        private void cboInstances_SelectedValueChanged(object sender, EventArgs e)
        {
            getThresholds();
        }
    }
}
