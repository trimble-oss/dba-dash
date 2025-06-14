using DBADash;
using DBADashService;
using Humanizer;
using Quartz;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using DBADashGUI.Theme;

namespace DBADashServiceConfig
{
    public partial class ScheduleConfig : Form
    {
        public ScheduleConfig()
        {
            InitializeComponent();
        }

        private CollectionSchedules userSchedule;
        public CollectionSchedules BaseSchedule = CollectionSchedules.DefaultSchedules;

        // For copy/paste function
        private string copySchedule;

        private bool copyDefault;
        private bool copyRunOnStart;
        private bool hasCopy;

        public CollectionSchedules ConfiguredSchedule
        {
            get
            {
                var schedule = new CollectionSchedules();
                foreach (DataGridViewRow row in dgv.Rows)
                {
                    if (!(bool)row.Cells["Default"].Value)
                    {
                        schedule.Add((CollectionType)Enum.Parse(typeof(CollectionType), (string)row.Cells["CollectionType"].Value), new CollectionSchedule() { Schedule = (string)row.Cells["Schedule"].Value, RunOnServiceStart = (bool)row.Cells["RunOnStart"].Value });
                    }
                }
                return schedule;
            }
            set => userSchedule = value;
        }

        private void ScheduleConfig_Load(object sender, EventArgs e)
        {
            dgv.Columns.Clear();
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "CollectionType", HeaderText = "Collection Type", ReadOnly = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Schedule", HeaderText = "Schedule", ToolTipText = "Cron expression or time in seconds" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "ScheduleDescription", HeaderText = "Schedule Description", ReadOnly = true });
            dgv.Columns.Add(new DataGridViewCheckBoxColumn() { Name = "RunOnStart", HeaderText = "Run on service start" });
            dgv.Columns.Add(new DataGridViewCheckBoxColumn() { Name = "Default", HeaderText = "Default", ToolTipText = "Uncheck to supply a custom cron schedule or check to use the default schedule." });

            if (userSchedule != null)
            {
                foreach (var s in userSchedule)
                {
                    int idx = dgv.Rows.Add(Enum.GetName(typeof(CollectionType), s.Key), s.Value.Schedule, GetScheduleDescription(s.Value.Schedule), s.Value.RunOnServiceStart, false);
                    FormatRow(idx);
                }
            }
            foreach (var s in BaseSchedule)
            {
                if (userSchedule == null || !userSchedule.ContainsKey(s.Key))
                {
                    int idx = dgv.Rows.Add(Enum.GetName(typeof(CollectionType), s.Key), s.Value.Schedule, GetScheduleDescription(s.Value.Schedule), s.Value.RunOnServiceStart, true);
                    FormatRow(idx);
                }
            }
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
            dgv.ApplyTheme();
        }

        public static string GetScheduleDescription(string schedule)
        {
            if (string.IsNullOrEmpty(schedule))
            {
                return "Disabled";
            }
            if (int.TryParse(schedule, out int seconds))
            {
                return TimeSpan.FromSeconds(seconds).Humanize(5);
            }
            else
            {
                if (CronExpression.IsValidExpression(schedule)) // Check expression is valid for Quartz
                {
                    return CronExpressionDescriptor.ExpressionDescriptor.GetDescription(schedule);
                }
                else
                {
                    throw new Exception("Invalid cron expression");
                }
            }
        }

        private void Dgv_RowValidating(object sender, DataGridViewCellCancelEventArgs e)
        {
            var schedule = (string)dgv[1, e.RowIndex].Value;

            try
            {
                dgv[2, e.RowIndex].Value = GetScheduleDescription(schedule);
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Invalid cron expression");
                e.Cancel = true;
            }
        }

        private void Dgv_CellValidated(object sender, DataGridViewCellEventArgs e)
        {
        }

        private void Dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (int i = 0; i < e.RowCount - 1; i++)
            {
                dgv.Rows[e.RowIndex + i].Cells["Schedule"].ReadOnly = (bool)dgv.Rows[e.RowIndex + i].Cells["Default"].Value;
                dgv.Rows[e.RowIndex + i].Cells["RunOnStart"].ReadOnly = (bool)dgv.Rows[e.RowIndex + i].Cells["Default"].Value;
            }
        }

        private void Dgv_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == dgv.Columns["Default"].Index)
            {
                FormatRow(e.RowIndex);
            }
        }

        private void FormatRow(int idx)
        {
            var row = dgv.Rows[idx];
            bool isDefault = (bool)row.Cells["Default"].Value;
            row.Cells["Schedule"].ReadOnly = isDefault;
            row.Cells["RunOnStart"].ReadOnly = isDefault;
            row.Cells["Schedule"].Style.BackColor = isDefault ? Color.LightGray : Color.White;
            row.Cells["RunOnStart"].Style.BackColor = isDefault ? Color.LightGray : Color.White;
            row.Cells["ScheduleDescription"].Style.BackColor = isDefault ? Color.LightGray : Color.AliceBlue;
            row.Cells["CollectionType"].Style.BackColor = isDefault ? Color.LightGray : Color.AliceBlue;
            if (isDefault)
            {
                CollectionType collectType = (CollectionType)Enum.Parse(typeof(CollectionType), (string)row.Cells["CollectionType"].Value);
                row.Cells["Schedule"].Value = BaseSchedule[collectType].Schedule;
                row.Cells["RunOnStart"].Value = BaseSchedule[collectType].RunOnServiceStart;
            }
            row.Cells["ScheduleDescription"].Value = GetScheduleDescription(Convert.ToString(row.Cells["Schedule"].Value));
        }

        private void Dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == dgv.Columns["Default"].Index)
            {
                dgv.EndEdit();
                FormatRow(e.RowIndex);
            }
        }

        private void BttnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void BttnOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void LnkCron_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var psi = new ProcessStartInfo("https://github.com/trimble-oss/dba-dash/blob/main/Docs/Collection.md#cron-expressions") { UseShellExecute = true };
            Process.Start(psi);
        }

        private void Dgv_SelectionChanged(object sender, EventArgs e)
        {
            tsCopy.Enabled = dgv.SelectedRows.Count == 1;
            tsPaste.Enabled = hasCopy && dgv.SelectedRows.Count > 0;
            tsSetDefault.Enabled = dgv.SelectedRows.Count > 0;
            tsDisable.Enabled = dgv.SelectedRows.Count > 0;
        }

        private void TsPaste_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgv.SelectedRows)
            {
                row.Cells["Default"].Value = copyDefault;
                row.Cells["RunOnStart"].Value = copyRunOnStart;
                row.Cells["Schedule"].Value = copySchedule;
                FormatRow(row.Index);
            }
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count == 1)
            {
                var row = dgv.SelectedRows[0];
                copySchedule = Convert.ToString(row.Cells["Schedule"].Value);
                copyDefault = Convert.ToBoolean(row.Cells["Default"].Value);
                copyRunOnStart = Convert.ToBoolean(row.Cells["RunOnStart"].Value);
                hasCopy = true;
                tsPaste.Enabled = true;
            }
        }

        private void TsSetDefault_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgv.SelectedRows)
            {
                row.Cells["Default"].Value = true;
                FormatRow(row.Index);
            }
        }

        private void TsDisable_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgv.SelectedRows)
            {
                row.Cells["Default"].Value = false;
                row.Cells["RunOnStart"].Value = false;
                row.Cells["Schedule"].Value = "";
                FormatRow(row.Index);
            }
        }
    }
}