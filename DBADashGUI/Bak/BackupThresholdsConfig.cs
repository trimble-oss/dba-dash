using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.Backups
{
    public partial class BackupThresholdsConfig : Form
    {

        public Int32 InstanceID = -1;
        public Int32 DatabaseID = -1;
        public string ConnectionString;


        public BackupThresholdsConfig()
        {
            InitializeComponent();
        }

        private void getThresholds()
        {
            var thresholds = BackupThresholds.GetThresholds(InstanceID, DatabaseID, ConnectionString);
            if (thresholds.FullCritical!=null && thresholds.FullWarning !=null)
            {
                chkFull.Checked = true;
                numFullCritical.Value = (Int32)thresholds.FullCritical;
                numFullWarning.Value = (Int32)thresholds.FullWarning;
            }
            else
            {

                chkFull.Checked = false;
            }
            if (thresholds.DiffCritical != null && thresholds.DiffWarning != null)
            {
                chkDiff.Checked = true;
                numDiffCritical.Value = (Int32)thresholds.DiffCritical;
                numDiffWarning.Value = (Int32)thresholds.DiffWarning;
            }
            else
            {

                chkDiff.Checked = false;
            }
            if (thresholds.LogCritical != null && thresholds.LogWarning != null)
            {
                chkLog.Checked = true;
                numLogCritical.Value = (Int32)thresholds.LogCritical;
                numLogWarning.Value = (Int32)thresholds.LogWarning;
            }
            else
            {

                chkLog.Checked = false;
            }

            chkBackupInherit.Checked = thresholds.Inherit;
            chkUsePartial.Checked = thresholds.UsePartial;
            chkUseFG.Checked = thresholds.UseFG;
            if (InstanceID == -1)
            {
                chkBackupInherit.Checked = false;
                chkBackupInherit.Enabled = false;
            }
            else
            {
                chkBackupInherit.Enabled = true;
            }


        }

        public BackupThresholds BackupThreshold { 
            get
            {
                var thresholds = new BackupThresholds();
                thresholds.DatabaseID = DatabaseID;
                thresholds.InstanceID = InstanceID;
                thresholds.Inherit = chkBackupInherit.Checked;
                thresholds.UseFG = chkUseFG.Checked;
                thresholds.UsePartial = chkUsePartial.Checked;
                if (chkFull.Checked) { thresholds.FullCritical = (Int32?)numFullCritical.Value; }
                if (chkFull.Checked) { thresholds.FullWarning = (Int32?)numFullWarning.Value; }
                if (chkDiff.Checked) { thresholds.DiffCritical = (Int32?)numDiffCritical.Value; }
                if (chkDiff.Checked) { thresholds.DiffWarning = (Int32?)numDiffWarning.Value; }
                if (chkLog.Checked) { thresholds.LogCritical = (Int32?)numLogCritical.Value; }
                if (chkLog.Checked) { thresholds.LogWarning = (Int32?)numLogWarning.Value; }
                return thresholds;
            }
        }
        

        private void BackupThresholds_Load(object sender, EventArgs e)
        {
            getThresholds();
        }

        private void chkBackupInherit_CheckedChanged(object sender, EventArgs e)
        {
            pnlBackupThresholds.Enabled = !chkBackupInherit.Checked;
        }

        private void bttnUpdate_Click(object sender, EventArgs e)
        {
            BackupThreshold.Save(ConnectionString);
            this.DialogResult = DialogResult.OK;

        }

        private void chkFull_CheckedChanged(object sender, EventArgs e)
        {
            numFullWarning.Enabled = chkFull.Checked;
            numFullCritical.Enabled = chkFull.Checked;
        }

        private void chkDiff_CheckedChanged(object sender, EventArgs e)
        {
            numDiffCritical.Enabled = chkDiff.Checked;
            numDiffWarning.Enabled = chkDiff.Checked;
        }

        private void chkLog_CheckedChanged(object sender, EventArgs e)
        {
            numLogCritical.Enabled = chkLog.Checked;
            numLogWarning.Enabled = chkLog.Checked;
        }
    }
}
