using DBADash;
using DBADashGUI.Theme;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace DBADashGUI.Options_Menu
{
    public partial class RepoSettings : Form
    {
        private const int AlertAutoCloseThresholdMins = 1440;
        private const int AlertMaxNotificationCount = 6;

        public RepoSettings()
        {
            InitializeComponent();
            this.ApplyTheme();
        }

        private List<(string SettingName, string Description, Type DataType, object value)> Settings => new()
        {
            ("GUISummaryCacheDuration", "How long to cache the summary tab data for in seconds", typeof(int), Config.ClientSummaryCacheDuration),
            ("GUIDefaultCommandTimeout", "Default command timeout in seconds", typeof(int),Config.DefaultCommandTimeout),
            ("GUISummaryCommandTimeout", "Summary command timeout in seconds", typeof(int),Config.SummaryCommandTimeout),
            ("GUIDrivePerformanceMaxDrives", "Maximum number of drives to show in the drive performance tab", typeof(int),Config.DrivePerformanceMaxDrives),
            ("IdleWarningThresholdForSleepingSessionWithOpenTran", "Idle warning threshold for sleeping session with open transaction on the Running Queries tab", typeof(double),Config.IdleWarningThresholdForSleepingSessionWithOpenTran),
            ("IdleCriticalThresholdForSleepingSessionWithOpenTran", "Idle critical threshold for sleeping session with open transaction on the Running Queries tab", typeof(double),Config.IdleCriticalThresholdForSleepingSessionWithOpenTran),
            ("GUICellToolTipMaxLength", "Maximum length of cell tool tip", typeof(int),Config.CellToolTipMaxLength),
            ("CPUCriticalThreshold","CPU utilization % critical threshold",typeof(int), Config.CPUCriticalThreshold),
            ("CPUWarningThreshold","CPU utilization % warning threshold",typeof(int), Config.CPUWarningThreshold),
            ("CPULowThreshold","CPU utilization % low threshold",typeof(int), Config.CPULowThreshold),
            ("ReadLatencyCriticalThreshold","Read latency ms critical threshold",typeof(int), Config.ReadLatencyCriticalThreshold),
            ("ReadLatencyWarningThreshold","Read latency ms warning threshold",typeof(int), Config.ReadLatencyWarningThreshold),
            ("ReadLatencyGoodThreshold","Read latency ms good threshold",typeof(int), Config.ReadLatencyGoodThreshold),
            ("WriteLatencyCriticalThreshold","Write latency ms critical threshold",typeof(int), Config.WriteLatencyCriticalThreshold),
            ("WriteLatencyWarningThreshold","Write latency ms warning threshold",typeof(int), Config.WriteLatencyWarningThreshold),
            ("WriteLatencyGoodThreshold","Write latency ms good threshold",typeof(int), Config.WriteLatencyGoodThreshold),
            ("MinIOPsThreshold","Minimum IOPs threshold.  Below this threshold, the latency thresholds don't apply. Performance summary tab.",typeof(int), Config.MinIOPsThreshold),
            ("CriticalWaitCriticalThreshold","Critical wait ms/sec critical threshold.  Performance summary tab.",typeof(int), Config.CriticalWaitCriticalThreshold),
            ("CriticalWaitWarningThreshold","Critical wait ms/sec warning threshold. Performance summary tab.",typeof(int), Config.CriticalWaitWarningThreshold),
            ("HardDeleteThresholdDays","Remove all the data associated with a (soft) deleted instance a specified number of days after the last collection.",typeof(int?),Config.HardDeleteThresholdDays ),
            ("GUISlowQueriesDrillDownMaxRows", "Max drill down rows for Slow Queries tab", typeof(int),Config.SlowQueriesDrillDownMaxRows),
            ("AlertAutoCloseThresholdMins","Automatically close resolved alerts after a specified period of time (mins)", typeof(int),AlertAutoCloseThresholdMins ),
            ("AlertMaxNotificationCount " , "Maximum number of alert notifications to send" , typeof(int) , AlertMaxNotificationCount),
            ("ExcludeClosedAlertsWithNotesFromPurge","Exclude closed alerts with notes from data retention",typeof(bool),true)
        };

        private void Options_Load(object sender, EventArgs e)
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            dgv.Columns.Clear();
            dgv.Rows.Clear();
            dgv.Columns.AddRange(
                new DataGridViewTextBoxColumn() { HeaderText = "Setting Name", ReadOnly = true, Width = 300, Visible = false },
                new DataGridViewTextBoxColumn() { HeaderText = "Description", ReadOnly = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill },
                new DataGridViewTextBoxColumn() { HeaderText = "Value", Width = 100, SortMode = DataGridViewColumnSortMode.NotSortable }
            );

            foreach (var setting in Settings)
            {
                var row = dgv.Rows.Add(setting.SettingName, setting.Description, setting.value);
                dgv.Rows[row].Cells[2].ValueType = setting.DataType;
            }
        }

        private void CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            var settingName = dgv.Rows[e.RowIndex].Cells[0].Value.ToString();
            var settingValue = dgv.Rows[e.RowIndex].Cells[2].Value;
            try
            {
                RepositorySettings.UpdateSetting(settingName, settingValue, Common.ConnectionString);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            Config.RefreshConfig();
            ThemeExtensions.CellToolTipMaxLength = Config.CellToolTipMaxLength;
        }

        private void BttnOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void ChkSettingName_CheckedChanged(object sender, EventArgs e)
        {
            dgv.Columns[0].Visible = chkSettingName.Checked;
        }

        private void BttnResetDefaults_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to reset all settings to their default values?",
                    "Reset Defaults", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            Config.ResetDefaults();
            LoadSettings();
        }
    }
}