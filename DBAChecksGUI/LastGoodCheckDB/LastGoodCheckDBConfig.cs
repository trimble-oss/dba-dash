using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBAChecksGUI.LastGoodCheckDB
{
    public partial class LastGoodCheckDBConfig : Form
    {

        LastGoodCheckDBThreshold threshold;

        public LastGoodCheckDBThreshold Threshold
        {
            get{

                threshold.Inherit = chkInherit.Visible ? chkInherit.Checked : false;
                threshold.WarningThreshold = chkEnabled.Checked ? (Int32?)numWarning.Value : null;
                threshold.CriticalThreshold = chkEnabled.Checked ? (Int32?)numCritical.Value : null;
                return threshold;

            }
            set{
                threshold = value;
                chkInherit.Visible = !(threshold.InstanceID == -1 && threshold.DatabaseID == -1);
                chkInherit.Checked = threshold.Inherit;
                if(threshold.WarningThreshold!= null && threshold.CriticalThreshold != null)
                {
                    numWarning.Value = (Int32)threshold.WarningThreshold;
                    numCritical.Value = (Int32)threshold.CriticalThreshold;
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
