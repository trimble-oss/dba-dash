using System;
using System.Windows.Forms;
using DBADashGUI.Theme;

namespace DBADashGUI.Backups
{
    public partial class BackupThresholdsConfig : Form
    {

        public int InstanceID = -1;
        public int DatabaseID = -1;

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
                numFullCritical.Value = (int)thresholds.FullCritical;
                numFullWarning.Value = (int)thresholds.FullWarning;
            }
            else
            {

                chkFull.Checked = false;
            }
            if (thresholds.DiffCritical != null && thresholds.DiffWarning != null)
            {
                chkDiff.Checked = true;
                numDiffCritical.Value = (int)thresholds.DiffCritical;
                numDiffWarning.Value = (int)thresholds.DiffWarning;
            }
            else
            {

                chkDiff.Checked = false;
            }
            if (thresholds.LogCritical != null && thresholds.LogWarning != null)
            {
                chkLog.Checked = true;
                numLogCritical.Value = (int)thresholds.LogCritical;
                numLogWarning.Value = (int)thresholds.LogWarning;
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
                if (chkFull.Checked) { thresholds.FullCritical = (int?)numFullCritical.Value; }
                if (chkFull.Checked) { thresholds.FullWarning = (int?)numFullWarning.Value; }
                if (chkDiff.Checked) { thresholds.DiffCritical = (int?)numDiffCritical.Value; }
                if (chkDiff.Checked) { thresholds.DiffWarning = (int?)numDiffWarning.Value; }
                if (chkLog.Checked) { thresholds.LogCritical = (int?)numLogCritical.Value; }
                if (chkLog.Checked) { thresholds.LogWarning = (int?)numLogWarning.Value; }
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
            DialogResult = DialogResult.OK;

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
