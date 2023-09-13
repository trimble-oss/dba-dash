using System;
using System.Windows.Forms;
using DBADashGUI.Theme;

namespace DBADashGUI.Backups
{
    public partial class BackupThresholdsConfig : Form
    {

        public Int32 InstanceID = -1;
        public Int32 DatabaseID = -1;

        public BackupThresholdsConfig()
        {
            InitializeComponent();
            this.ApplyTheme();
        }

        private void GetThresholds()
        {
            var thresholds = BackupThresholds.GetThresholds(InstanceID, DatabaseID);
            if (thresholds.FullCritical != null && thresholds.FullWarning != null)
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
            txtExcluded.Text = thresholds.ExcludedDBs;
            numMinimumAge.Value = thresholds.MinimumAge;
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

        public BackupThresholds BackupThreshold
        {
            get
            {
                var thresholds = new BackupThresholds
                {
                    DatabaseID = DatabaseID,
                    InstanceID = InstanceID,
                    Inherit = chkBackupInherit.Checked,
                    UseFG = chkUseFG.Checked,
                    UsePartial = chkUsePartial.Checked,
                    ExcludedDBs = txtExcluded.Text.Trim(),
                    MinimumAge = Convert.ToInt32(numMinimumAge.Value)
                };
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
            GetThresholds();
        }

        private void ChkBackupInherit_CheckedChanged(object sender, EventArgs e)
        {
            pnlBackupThresholds.Enabled = !chkBackupInherit.Checked;
        }

        private void BttnUpdate_Click(object sender, EventArgs e)
        {
            BackupThreshold.Save();
            this.DialogResult = DialogResult.OK;

        }

        private void ChkFull_CheckedChanged(object sender, EventArgs e)
        {
            numFullWarning.Enabled = chkFull.Checked;
            numFullCritical.Enabled = chkFull.Checked;
        }

        private void ChkDiff_CheckedChanged(object sender, EventArgs e)
        {
            numDiffCritical.Enabled = chkDiff.Checked;
            numDiffWarning.Enabled = chkDiff.Checked;
        }

        private void ChkLog_CheckedChanged(object sender, EventArgs e)
        {
            numLogCritical.Enabled = chkLog.Checked;
            numLogWarning.Enabled = chkLog.Checked;
        }
    }
}
