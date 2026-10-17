using DBADash;
using DBADash.Messaging;
using DBADashGUI.Theme;
using DBADashService;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using static DBADash.Messaging.CollectionScheduleUpdateMessage;

namespace DBADashGUI.CollectionDates
{
    /// <summary>
    /// Choose a new schedule for one or more collections - inherit, disabled or a custom cron/seconds schedule - and
    /// whether it applies to the selected instances or to every instance on their service.  Sent to the service as a
    /// <see cref="CollectionScheduleUpdateMessage"/> by the caller.
    /// </summary>
    public partial class EditCollectionSchedule : Form
    {
        public EditCollectionSchedule()
        {
            InitializeComponent();
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<CollectionType> CollectionTypes { get; set; } = new();

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<string> InstanceNames { get; set; } = new();

        /// <summary>The services (collect agents) the selected instances are collected by.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<string> ServiceNames { get; set; } = new();

        /// <summary>The schedule the first selected collection currently reports - the starting point for editing.  Null if not reported.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public CollectionSchedule CurrentSchedule { get; set; }

        /// <summary>Something the user should know before changing the schedule, shown under the collections.  Null for none.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Note { get; set; }

        public ScheduleScopes Scope => rbService.Checked ? ScheduleScopes.Service : ScheduleScopes.Instance;

        /// <summary>The new schedule, or null to inherit (remove the override).</summary>
        public CollectionSchedule Schedule => rbInherit.Checked
            ? null
            : new CollectionSchedule
            {
                Schedule = rbDisabled.Checked ? string.Empty : txtSchedule.Text.Trim(),
                RunOnServiceStart = chkRunOnServiceStart.Checked
            };

        private void EditCollectionSchedule_Load(object sender, EventArgs e)
        {
            lblTarget.Text = $"Collections: {Summarize(CollectionTypes.Select(t => t.ToString()).ToList())}\nInstances: {Summarize(InstanceNames)}";
            if (!string.IsNullOrEmpty(Note))
            {
                // Room for two more lines, moving everything below down to make it
                var extra = lblTarget.Font.Height * 2 + 4;
                lblTarget.Text += "\n" + Note;
                lblTarget.Height += extra;
                foreach (var control in new Control[] { grpScope, grpSchedule, bttnOK, bttnCancel })
                {
                    control.Top += extra;
                }
                ClientSize = new System.Drawing.Size(ClientSize.Width, ClientSize.Height + extra);
            }
            rbInstance.Text = InstanceNames.Count == 1
                ? "This instance only (instance level override)"
                : $"The {InstanceNames.Count} selected instances only (instance level override)";
            rbService.Text = $"All instances collected by {Summarize(ServiceNames)} (service level)";

            if (CurrentSchedule == null)
            {
                rbInherit.Checked = true;
            }
            else if (string.IsNullOrEmpty(CurrentSchedule.Schedule))
            {
                rbDisabled.Checked = true;
            }
            else
            {
                rbCustom.Checked = true;
                txtSchedule.Text = CurrentSchedule.Schedule;
            }
            chkRunOnServiceStart.Checked = CurrentSchedule?.RunOnServiceStart ?? false;

            UpdateInheritText();
            UpdateControls();
            this.ApplyTheme();
        }

        private static string Summarize(List<string> items, int max = 5) =>
            items.Count <= max
                ? string.Join(", ", items)
                : string.Join(", ", items.Take(max)) + $" and {items.Count - max} more";

        private void UpdateInheritText()
        {
            if (Scope == ScheduleScopes.Instance)
            {
                rbInherit.Text = "Inherit (use the service level schedule)";
                return;
            }

            // Only the built-in default is known here - the service level schedule is what's being changed
            var defaultDescription = CollectionTypes.Count == 1 && CollectionSchedules.DefaultSchedules.TryGetValue(CollectionTypes[0], out var defaultSchedule)
                ? ": " + CronParser.GetScheduleDescriptionSafe(defaultSchedule.Schedule, runOnServiceStart: defaultSchedule.RunOnServiceStart)
                : string.Empty;
            rbInherit.Text = $"Default{defaultDescription}";
        }

        private void UpdateControls()
        {
            txtSchedule.Enabled = rbCustom.Checked;
            lnkBuilder.Enabled = rbCustom.Checked;
            chkRunOnServiceStart.Enabled = !rbInherit.Checked;

            if (!rbCustom.Checked)
            {
                lblDescription.Text = string.Empty;
                bttnOK.Enabled = true;
                return;
            }

            var schedule = txtSchedule.Text.Trim();
            var isValid = schedule.Length > 0 && IsValidSchedule(schedule);
            lblDescription.Text = schedule.Length == 0
                ? "Enter a cron expression or a time in seconds"
                : isValid
                    ? CronParser.GetScheduleDescriptionSafe(schedule)
                    : "Invalid schedule";
            lblDescription.ForeColor = isValid ? DashColors.Success : DashColors.Fail;
            bttnOK.Enabled = isValid;
        }

        private void Scope_CheckedChanged(object sender, EventArgs e) => UpdateInheritText();

        private void Schedule_CheckedChanged(object sender, EventArgs e) => UpdateControls();

        private void TxtSchedule_TextChanged(object sender, EventArgs e) => UpdateControls();

        private void LnkBuilder_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            using var frm = new CronExpressionBuilder(txtSchedule.Text.Trim()) { SupportsIntegerSeconds = true };
            if (frm.ShowDialog(this) == DialogResult.OK)
            {
                txtSchedule.Text = frm.CronExpression ?? string.Empty;
            }
        }

        private void BttnOK_Click(object sender, EventArgs e)
        {
            if (Scope == ScheduleScopes.Service &&
                MessageBox.Show(this,
                    $"This changes the schedule for every instance collected by {Summarize(ServiceNames)} that doesn't have its own instance level schedule.\n\nContinue?",
                    "Change Service Schedule", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }
            DialogResult = DialogResult.OK;
        }
    }
}
