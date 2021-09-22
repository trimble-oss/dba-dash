using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.LastGoodCheckDB
{
    public partial class LastGoodCheckDBConfig : Form
    {

        LastGoodCheckDBThreshold threshold;

        public LastGoodCheckDBThreshold Threshold
        {
            get{

                threshold.Inherit = chkInherit.Visible && chkInherit.Checked;
                threshold.WarningThreshold = chkEnabled.Checked ? (Int32?)numWarning.Value : null;
                threshold.CriticalThreshold = chkEnabled.Checked ? (Int32?)numCritical.Value : null;
                threshold.MinimumAge =chkEnabled.Checked ?  (Int32)numMinimumAge.Value : 0;
                threshold.ExcludedDatabases =chkEnabled.Checked ? txtExcluded.Text : string.Empty;
                return threshold;

            }
            set{
                threshold = value;
                chkInherit.Visible = !(threshold.InstanceID == -1 && threshold.DatabaseID == -1);
                chkInherit.Checked = threshold.Inherit;
                numMinimumAge.Value = threshold.MinimumAge;
                txtExcluded.Text = threshold.ExcludedDatabases;
                if(threshold.WarningThreshold!= null && threshold.CriticalThreshold != null)
                {
                    numWarning.Value = (Int32)threshold.WarningThreshold;
                    numCritical.Value = (Int32)threshold.CriticalThreshold;
                }
                else
                {
                    chkEnabled.Checked = false;
                }
            }
        }

        public LastGoodCheckDBConfig()
        {
            InitializeComponent();
        }

        private void chkEnabled_CheckedChanged(object sender, EventArgs e)
        {
            numWarning.Enabled = chkEnabled.Checked;
            numCritical.Enabled = chkEnabled.Checked;
            numMinimumAge.Enabled = chkEnabled.Checked;
            txtExcluded.Enabled = chkEnabled.Checked;
        }

        private void chkInherit_CheckedChanged(object sender, EventArgs e)
        {
            pnlThresholds.Enabled = !chkInherit.Checked;
        }

        private void bttnUpdate_Click(object sender, EventArgs e)
        {
            Threshold.Save();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
