using DBADash;
using DBADashGUI.Theme;
using DBADashSharedGUI;
using DBADashService;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace DBADashGUI
{
    /// <summary>
    /// Edits a set of collection schedule overrides against a base schedule - the service schedule against the built-in
    /// defaults, or an instance's against the service schedule.  Used by the service config tool and by the GUI, which
    /// can also offer to apply the changes temporarily (<see cref="AllowTemporary"/>).
    /// </summary>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public partial class ScheduleConfig : Form
    {
        public ScheduleConfig()
        {
            InitializeComponent();
        }

        /// <summary>Offer to apply the changes temporarily, reverting at <see cref="TemporaryUntil"/>.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool AllowTemporary { get; set; }

        /// <summary>Describes the temporary schedules in effect, with an option to end them now.  Empty when there are none.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string TemporarySchedulesText { get; set; }

        /// <summary>When temporary changes revert, in the time zone the caller displays.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DateTime TemporaryUntil
        {
            get => dtpTemporaryUntil.Value;
            set => dtpTemporaryUntil.Value = value;
        }

        /// <summary>True if the user chose to apply the changes temporarily.</summary>
        public bool ApplyTemporarily => AllowTemporary && chkTemporary.Checked;

        /// <summary>True if the user chose to end the temporary schedules in effect.  The dialog closes with OK.</summary>
        public bool EndTemporarySchedules { get; private set; }

        /// <summary>
        /// For a service schedule: how many instances follow each collection's service schedule (rather than their own),
        /// out of <see cref="InstanceCount"/>.  When set, an Instances column shows which rows a change would affect, and
        /// <see cref="ShowInstances"/> lists them.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Dictionary<CollectionType, int> InstancesFollowing { get; set; }

        /// <summary>The number of instances the service schedule applies to.  See <see cref="InstancesFollowing"/>.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int InstanceCount { get; set; }

        /// <summary>Lists the instances - for a collection when one is given, showing which follow its service schedule.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Action<IWin32Window, CollectionType?> ShowInstances { get; set; }

        private const string InstancesColumnName = "Instances";

        private CollectionSchedules userSchedule;
        public CollectionSchedules BaseSchedule = CollectionSchedules.DefaultSchedules;

        // For copy/paste function
        private string copySchedule;

        private bool copyDefault;
        private bool copyRunOnStart;
        private bool hasCopy;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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
            dgv.Columns.Add(new DataGridViewLinkColumn() { Name = "ScheduleDescription", HeaderText = "Schedule Description", ReadOnly = false, TrackVisitedState = false, LinkBehavior = LinkBehavior.SystemDefault });
            dgv.Columns.Add(new DataGridViewCheckBoxColumn() { Name = "RunOnStart", HeaderText = "Run on service start" });
            dgv.Columns.Add(new DataGridViewCheckBoxColumn() { Name = "Default", HeaderText = "Default", ToolTipText = "Uncheck to supply a custom cron schedule or check to use the default schedule." });

            if (userSchedule != null)
            {
                foreach (var s in userSchedule)
                {
                    if (s.Key == CollectionType.ScheduleInfo) continue; // Internal bookkeeping type - not user configurable
                    int idx = dgv.Rows.Add(Enum.GetName(typeof(CollectionType), s.Key), s.Value.Schedule, CronParser.GetScheduleDescription(s.Value.Schedule, s.Value.RunOnServiceStart), s.Value.RunOnServiceStart, false);
                    FormatRow(idx);
                }
            }
            foreach (var s in BaseSchedule)
            {
                if (s.Key == CollectionType.ScheduleInfo) continue; // Internal bookkeeping type - not user configurable
                if (userSchedule == null || !userSchedule.ContainsKey(s.Key))
                {
                    int idx = dgv.Rows.Add(Enum.GetName(typeof(CollectionType), s.Key), s.Value.Schedule, CronParser.GetScheduleDescription(s.Value.Schedule, s.Value.RunOnServiceStart), s.Value.RunOnServiceStart, true);
                    FormatRow(idx);
                }
            }
            AddInstancesColumn();
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);

            var hasTemporary = !string.IsNullOrEmpty(TemporarySchedulesText);
            pnlTemporary.Visible = AllowTemporary || hasTemporary;
            chkTemporary.Visible = AllowTemporary;
            dtpTemporaryUntil.Visible = AllowTemporary;
            lblTemporarySchedules.Text = TemporarySchedulesText;
            lnkEndTemporary.Visible = hasTemporary;
            this.ApplyTheme();
        }

        /// <summary>
        /// Shows, for a service schedule, how many instances each row applies to - those without their own schedule for
        /// the collection - so it's clear what a change affects.  See <see cref="InstancesFollowing"/>.
        /// </summary>
        private void AddInstancesColumn()
        {
            if (InstancesFollowing == null) return;

            dgv.Columns.Add(new DataGridViewLinkColumn
            {
                Name = InstancesColumnName,
                HeaderText = "Instances Affected",
                ReadOnly = true,
                TrackVisitedState = false,
                ToolTipText = "Instances that follow the service schedule for this collection, so a change to it applies to them.  Instances with their own schedule for the collection aren't affected.  Click to list them."
            });
            foreach (DataGridViewRow row in dgv.Rows)
            {
                var type = Enum.Parse<CollectionType>((string)row.Cells["CollectionType"].Value);
                var following = InstancesFollowing.GetValueOrDefault(type, InstanceCount);
                row.Cells[InstancesColumnName].Value = following == InstanceCount ? $"All {InstanceCount}" : $"{following} of {InstanceCount}";
            }

            if (ShowInstances != null)
            {
                var tsInstances = new ToolStripButton($"{InstanceCount} Instances")
                {
                    Alignment = ToolStripItemAlignment.Right,
                    ToolTipText = "List the instances the service schedule applies to, with any schedules of their own"
                };
                tsInstances.Click += (_, _) => ShowInstances(this, null);
                toolStrip1.Items.Add(tsInstances);
            }
        }

        private void ChkTemporary_CheckedChanged(object sender, EventArgs e)
        {
            dtpTemporaryUntil.Enabled = chkTemporary.Checked;
        }

        private void LnkEndTemporary_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            EndTemporarySchedules = true;
            DialogResult = DialogResult.OK;
        }

        private void Dgv_RowValidating(object sender, DataGridViewCellCancelEventArgs e)
        {
            var schedule = (string)dgv[1, e.RowIndex].Value;
            var runOnServiceStart = Convert.ToBoolean(dgv.Rows[e.RowIndex].Cells["RunOnStart"].Value);

            try
            {
                dgv[2, e.RowIndex].Value = CronParser.GetScheduleDescription(schedule, runOnServiceStart);
                // A valid expression can still never fire (e.g. 30 February), which stops the collections being scheduled
                if (!DBADash.Messaging.CollectionScheduleUpdateMessage.IsValidSchedule(schedule))
                {
                    throw new ArgumentException($"The schedule '{schedule}' never runs.");
                }
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Invalid cron expression");
                e.Cancel = true;
            }
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
            static void FormatCell(DataGridViewCell cell)
            {
                cell.Style.BackColor = cell.ReadOnly ? DashColors.GrayLight : Color.White;
                cell.Style.SelectionBackColor = cell.ReadOnly ? DashColors.TrimbleGray : DashColors.BluePale;
                cell.Style.SelectionForeColor = DashColors.White;
            }
            var row = dgv.Rows[idx];
            bool isDefault = (bool)row.Cells["Default"].Value;
            row.Cells["Schedule"].ReadOnly = isDefault;
            row.Cells["RunOnStart"].ReadOnly = isDefault;
            FormatCell(row.Cells["Schedule"]);
            FormatCell(row.Cells["RunOnStart"]);
            FormatCell(row.Cells["ScheduleDescription"]);
            FormatCell(row.Cells["CollectionType"]);
            FormatCell(row.Cells["Default"]);
            if (isDefault)
            {
                CollectionType collectType = (CollectionType)Enum.Parse(typeof(CollectionType), (string)row.Cells["CollectionType"].Value);
                row.Cells["Schedule"].Value = BaseSchedule[collectType].Schedule;
                row.Cells["RunOnStart"].Value = BaseSchedule[collectType].RunOnServiceStart;
            }
            row.Cells["ScheduleDescription"].Value = CronParser.GetScheduleDescription(Convert.ToString(row.Cells["Schedule"].Value), Convert.ToBoolean(row.Cells["RunOnStart"].Value));
        }

 

        private void Dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && ShowInstances != null && dgv.Columns[e.ColumnIndex].Name == InstancesColumnName)
            {
                ShowInstances(this, Enum.Parse<CollectionType>((string)dgv.Rows[e.RowIndex].Cells["CollectionType"].Value));
                return;
            }

            if (e.ColumnIndex == dgv.Columns["Default"].Index)
            {
                dgv.EndEdit();
                FormatRow(e.RowIndex);
            }

            // If the schedule description link was clicked, open the cron expression builder
            if (e.RowIndex >= 0 && e.ColumnIndex == dgv.Columns["ScheduleDescription"].Index)
            {
                var row = dgv.Rows[e.RowIndex];
                var currentSchedule = Convert.ToString(row.Cells["Schedule"].Value);
                var isDefault = (bool)row.Cells["Default"].Value;
                using (var dlg = new CronExpressionBuilder(currentSchedule) { SupportsIntegerSeconds = true, SupportsDefault = true, UseDefaultValue = isDefault })
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        row.Cells["Default"].Value = dlg.UseDefaultValue;
                        if (!dlg.UseDefaultValue)
                        {
                            row.Cells["Schedule"].Value = dlg.CronExpression ?? string.Empty;
                        }
                        FormatRow(e.RowIndex);
                    }
                }
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