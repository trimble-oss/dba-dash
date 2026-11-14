using DBADash;
using DBADash.DailyReport;
using DBADashGUI.Theme;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.DBADashAlerts.DailyReports
{
    /// <summary>Add or edit a daily report: schedule, instance scope, email channels and what to include.</summary>
    public class DailyReportEditForm : Form
    {
        private readonly string connectionString;
        private readonly DailyReportConfig config;
        private DBADashTagSet scope;

        private readonly TextBox txtName = new() { Width = 260 };
        private readonly CheckBox chkActive = new() { Text = "Active (sends on schedule)", AutoSize = true };
        private readonly TextBox txtTime = new() { Width = 70, Text = "08:00" };
        private readonly ComboBox cboType = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260 };
        private readonly Label lblTypeNote = new() { AutoSize = true, MaximumSize = new System.Drawing.Size(420, 0) };
        private readonly CheckBox[] chkDays = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" }
            .Select(d => new CheckBox { Text = d, AutoSize = true, Margin = new Padding(0, 3, 6, 0) }).ToArray();
        private readonly ComboBox cboRepeat = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
        private readonly TextBox txtUntil = new() { Width = 70 };

        /// <summary>The repeat choices: null is once a day</summary>
        private static readonly int?[] RepeatMinutes = { null, 15, 30, 60, 120, 180, 240, 360, 480, 720 };
        private readonly ComboBox cboTimeZone = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260 };
        private readonly Label lblScope = new() { AutoSize = true, MaximumSize = new System.Drawing.Size(360, 0) };
        private readonly Button btnScope = new Button { Text = "Choose tags...", AutoSize = true }.WithImage(Properties.Resources.Filter_16x);
        private readonly CheckedListBox clbChannels = new() { Height = 110, Width = 260, CheckOnClick = true };
        private readonly CheckBox chkPerformance = new() { Text = "Include performance: instances over the Performance Summary thresholds", AutoSize = true };
        private readonly CheckBox chkWeekOverWeek = new() { Text = "Compare with the same day in previous weeks (worse than usual)", AutoSize = true };
        private readonly NumericUpDown numThreshold = new() { Minimum = 1, Maximum = 1000, Value = 50, Width = 70 };
        private readonly Label lblChannelsNote = new() { AutoSize = true, MaximumSize = new System.Drawing.Size(420, 0), Text = "Only Email channels can receive daily reports. Recipients are the channel's To Email." };
        private readonly Label lblPerformanceScope = new() { AutoSize = true, MaximumSize = new System.Drawing.Size(360, 0) };
        private readonly Button btnPerformanceScope = new Button { Text = "Choose tags...", AutoSize = true }.WithImage(Properties.Resources.Filter_16x);
        private DBADashTagSet performanceScope;
        private readonly CheckBox chkAsUsual = new() { Text = "Only report a threshold breach when it's outside the usual range", AutoSize = true };
        private readonly CheckBox chkHours = new() { Text = "Only count", AutoSize = true, Padding = new Padding(0, 3, 0, 0) };
        private readonly ComboBox cboHoursFrom = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 70 };
        private readonly ComboBox cboHoursTo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 70 };
        private readonly CheckBox chkActivity = new() { Text = "Long running requests on each listed instance (DBCC, backups, index maintenance)", AutoSize = true };
        private readonly CheckBox chkCharts = new() { Text = "Charts of each listed instance by hour", AutoSize = true };
        private readonly CheckBox chkTopQueries = new() { Text = "Procedures and queries behind each listed instance", AutoSize = true };
        private readonly CheckBox chkResolved = new() { Text = "List issues resolved since the last report", AutoSize = true };
        private readonly CheckBox chkAlerts = new() { Text = "Open alerts (not acknowledged or in a blackout) - in a changes report, alerts raised, escalated or cleared", AutoSize = true };
        private readonly CheckBox chkOpenSummary = new() { Text = "Brief summary of everything still open (changes report)", AutoSize = true };
        private readonly CheckBox chkPlainText = new() { Text = "Send as plain text (no formatting or charts)", AutoSize = true };
        private readonly CheckBox chkAISummary = new() { Text = "AI summary at the top (sends names and figures, never query text, to the DBA Dash AI service)", AutoSize = true };
        private readonly Button btnPerformanceSettings = new Button { Text = "Performance settings...", AutoSize = true }.WithImage(Properties.Resources.SettingsOutline_16x);
        private DailyReportSettings settings = new();
        private readonly Button btnPreview = new Button { Text = "Preview...", AutoSize = true }.WithImage(Properties.Resources.VBReport_16x);
        private readonly Button btnSave = new Button { Text = "Save", DialogResult = DialogResult.None, AutoSize = true }.WithImage(Properties.Resources.Save_16x);
        private readonly Button btnCancel = new() { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };

        private readonly List<DailyReportChannelRef> emailChannels;

        public DailyReportEditForm(DailyReportConfig existing, string connectionString)
        {
            this.connectionString = connectionString;
            config = existing == null ? new DailyReportConfig() : Copy(existing);
            Text = existing == null ? "Add daily report" : "Edit daily report";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = DailyReportIcons.FormIcon;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            // Every Email channel, including ones in their disable window: leaving those out made them vanish from the list without a word.
            // They're marked as disabled instead - a report isn't sent to a channel while it's disabled (see DailyReportRunner.SendAsync)
            emailChannels = DailyReportLookups.GetChannels(connectionString)
                .Where(c => string.Equals(c.ChannelType, NotificationChannelBase.NotificationChannelTypes.Email.ToString(), StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (emailChannels.Count == 0)
            {
                lblChannelsNote.Text = "There are no Email notification channels yet. Add one in the alert configuration (Configure > Notification Channels), then come back here.";
            }
            else if (emailChannels.Any(c => !c.IsActive))
            {
                lblChannelsNote.Text += Environment.NewLine + "Channels marked (disabled) are in their disable window - reports aren't sent to them until it ends.";
            }

            var layout = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, Padding = new Padding(12) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            void Row(string label, Control control)
            {
                layout.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Top, Padding = new Padding(0, 6, 8, 0) });
                layout.Controls.Add(control);
            }

            cboType.Items.AddRange(new object[] { "Full report", "Changes only" });
            cboRepeat.Items.AddRange(RepeatMinutes.Select(m => (object)(m.HasValue ? DailyReportConfig.IntervalText(m.Value) : "Once a day")).ToArray());

            Row("Name", txtName);
            Row("", chkActive);
            Row("Report", cboType);
            Row("", lblTypeNote);
            Row("Days", FlowOf(chkDays));
            Row("Send at (HH:mm)", txtTime);
            Row("Repeat", FlowOf(cboRepeat, new Label { Text = "until (HH:mm)", AutoSize = true, Padding = new Padding(6, 6, 4, 0) }, txtUntil));
            Row("Time zone", cboTimeZone);
            Row("Instances", FlowOf(btnScope, lblScope));
            Row("Email channels", clbChannels);
            Row("", lblChannelsNote);
            Row("", chkPerformance);
            Row("", chkWeekOverWeek);
            Row("Change threshold %", numThreshold);
            Row("", chkAsUsual);
            Row("Performance instances", FlowOf(btnPerformanceScope, lblPerformanceScope));
            Row("Performance hours", FlowOf(chkHours, cboHoursFrom, new Label { Text = "to", AutoSize = true, Padding = new Padding(2, 6, 2, 0) }, cboHoursTo,
                new Label { Text = "(report time zone - best sent after the end)", AutoSize = true, Padding = new Padding(6, 6, 0, 0) }));
            Row("", chkCharts);
            Row("", chkTopQueries);
            Row("", chkActivity);
            Row("", btnPerformanceSettings);
            Row("", chkResolved);
            Row("", chkAlerts);
            Row("", chkOpenSummary);
            Row("", chkAISummary);
            Row("", chkPlainText);
            layout.Controls.Add(FlowOf(btnPreview, btnSave, btnCancel));
            layout.SetColumnSpan(layout.Controls[layout.Controls.Count - 1], 2);

            Controls.Add(layout);
            AcceptButton = btnSave;
            CancelButton = btnCancel;

            btnScope.Click += (_, _) => ChooseScope();
            btnPerformanceScope.Click += (_, _) => ChoosePerformanceScope();
            btnSave.Click += (_, _) => Save();
            btnPreview.Click += async (_, _) => await PreviewAsync();
            var hours = Enumerable.Range(0, 24).Select(h => (object)$"{h:00}:00").ToArray();
            cboHoursFrom.Items.AddRange(hours);
            cboHoursTo.Items.AddRange(hours);
            cboHoursFrom.SelectedIndex = 9;
            cboHoursTo.SelectedIndex = 17;
            chkHours.CheckedChanged += (_, _) => UpdatePerformanceOptions();
            chkWeekOverWeek.CheckedChanged += (_, _) => UpdatePerformanceOptions();
            chkPerformance.CheckedChanged += (_, _) => UpdatePerformanceOptions();
            chkPlainText.CheckedChanged += (_, _) => UpdatePerformanceOptions();
            cboType.SelectedIndexChanged += (_, _) => UpdatePerformanceOptions();
            cboRepeat.SelectedIndexChanged += (_, _) => txtUntil.Enabled = cboRepeat.SelectedIndex > 0;
            btnPerformanceSettings.Click += (_, _) => EditPerformanceSettings();
            this.ApplyTheme();
            Load += (_, _) => LoadValues();
        }

        private static FlowLayoutPanel FlowOf(params Control[] controls)
        {
            var flow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0) };
            flow.Controls.AddRange(controls);
            return flow;
        }

        private static DailyReportConfig Copy(DailyReportConfig c) => new()
        {
            DailyReportID = c.DailyReportID,
            ReportName = c.ReportName,
            IsActive = c.IsActive,
            ScheduleTime = c.ScheduleTime,
            TimeZone = c.TimeZone,
            DaysOfWeek = c.DaysOfWeek,
            IntervalMinutes = c.IntervalMinutes,
            WindowEndTime = c.WindowEndTime,
            ReportType = c.ReportType,
            ApplyToTagID = c.ApplyToTagID,
            ApplyToTagIDs = c.ApplyToTagIDs,
            ScopeDescription = c.ScopeDescription,
            IncludePerformance = c.IncludePerformance,
            IncludeWeekOverWeek = c.IncludeWeekOverWeek,
            ChangeThresholdPct = c.ChangeThresholdPct,
            Settings = (c.Settings ?? new DailyReportSettings()).Clone(),
            Channels = c.Channels.ToList()
        };

        private void LoadValues()
        {
            try
            {
                cboTimeZone.Items.AddRange(DailyReportLookups.GetTimeZones(connectionString).Cast<object>().ToArray());
                cboTimeZone.SelectedItem = config.TimeZone;
                if (cboTimeZone.SelectedItem == null) cboTimeZone.SelectedItem = "UTC";

                foreach (var ch in emailChannels)
                {
                    clbChannels.Items.Add(new ChannelItem(ch), config.ChannelIDs.Contains(ch.NotificationChannelID));
                }

                txtName.Text = config.ReportName ?? string.Empty;
                chkActive.Checked = config.IsActive;
                txtTime.Text = $"{(int)config.ScheduleTime.TotalHours:00}:{config.ScheduleTime.Minutes:00}";
                cboType.SelectedIndex = config.ReportType == DailyReportType.Changes ? 1 : 0;
                for (var i = 0; i < chkDays.Length; i++) chkDays[i].Checked = (config.DaysOfWeek & (1 << i)) != 0;
                var repeat = Array.IndexOf(RepeatMinutes, config.IntervalMinutes);
                cboRepeat.SelectedIndex = repeat >= 0 ? repeat : 0;
                txtUntil.Text = config.WindowEndTime.HasValue ? $"{(int)config.WindowEndTime.Value.TotalHours:00}:{config.WindowEndTime.Value.Minutes:00}" : string.Empty;
                txtUntil.Enabled = cboRepeat.SelectedIndex > 0;
                chkPerformance.Checked = config.IncludePerformance;
                chkWeekOverWeek.Checked = config.IncludeWeekOverWeek;
                numThreshold.Value = Math.Min(Math.Max(config.ChangeThresholdPct, 1), 1000);
                settings = (config.Settings ?? new DailyReportSettings()).Clone();
                chkCharts.Checked = settings.IncludeCharts;
                chkTopQueries.Checked = settings.IncludeTopQueries;
                chkActivity.Checked = settings.IncludeActivity;
                chkAsUsual.Checked = settings.ThresholdsOnlyWhenWorseThanUsual;
                chkHours.Checked = settings.HasPerformanceHours;
                if (settings.HasPerformanceHours)
                {
                    cboHoursFrom.SelectedIndex = settings.PerformanceHoursFrom!.Value;
                    cboHoursTo.SelectedIndex = settings.PerformanceHoursTo!.Value;
                }
                performanceScope = settings.PerformanceApplyToTagID == DBADashTagSet.MultipleTagsID
                    ? DBADashTagSet.Load(connectionString, settings.PerformanceApplyToTagIDs)
                    : settings.PerformanceApplyToTagID > 0 ? DBADashTagSet.FromTag(DBADashTag.GetTag(connectionString, settings.PerformanceApplyToTagID)) : DBADashTagSet.AllInstances();
                ShowPerformanceScope();
                chkResolved.Checked = settings.IncludeResolved;
                chkAlerts.Checked = settings.IncludeAlerts;
                chkOpenSummary.Checked = settings.IncludeOpenSummary;
                chkAISummary.Checked = settings.IncludeAISummary;
                chkPlainText.Checked = settings.PlainTextOnly;
                UpdatePerformanceOptions();
                scope = config.ApplyToTagID == DBADashTagSet.MultipleTagsID
                    ? DBADashTagSet.Load(connectionString, config.ApplyToTagIDs)
                    : config.ApplyToTagID > 0 ? DBADashTagSet.FromTag(DBADashTag.GetTag(connectionString, config.ApplyToTagID)) : DBADashTagSet.AllInstances();
                ShowScope();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Daily report", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// The performance options only apply when performance is in the report; the threshold only to the comparison.  A changes report
        /// lists checks only, so none of them apply to it - nor do the AI summary or the resolved list (which it always has).
        /// </summary>
        private void UpdatePerformanceOptions()
        {
            var full = cboType.SelectedIndex != 1;
            lblTypeNote.Text = full
                ? "Everything in scope: critical and warning checks, and performance if included."
                : "Only checks that are new, worse, better or resolved since its last send. Nothing is sent when nothing changed. Suits a repeating schedule.";
            chkPerformance.Enabled = full;
            chkWeekOverWeek.Enabled = full;
            chkResolved.Enabled = full;
            chkAISummary.Enabled = full;
            chkOpenSummary.Enabled = !full;
            var performance = full && (chkPerformance.Checked || chkWeekOverWeek.Checked);
            numThreshold.Enabled = full && chkWeekOverWeek.Checked;
            chkCharts.Enabled = performance && !chkPlainText.Checked;
            chkTopQueries.Enabled = performance;
            chkActivity.Enabled = performance;
            btnPerformanceScope.Enabled = performance;
            chkHours.Enabled = performance;
            cboHoursFrom.Enabled = cboHoursTo.Enabled = performance && chkHours.Checked;
            chkAsUsual.Enabled = full && chkPerformance.Checked && chkWeekOverWeek.Checked;
            btnPerformanceSettings.Enabled = performance;
        }

        private void EditPerformanceSettings()
        {
            using var frm = new DailyReportPerformanceSettingsForm(settings);
            if (frm.ShowDialog(this) == DialogResult.OK)
            {
                settings = frm.Settings;
            }
        }

        private void ShowScope()
        {
            lblScope.Text = scope == null || scope.IsAllInstances ? "All instances" : scope.ToString();
        }

        private void ShowPerformanceScope()
        {
            lblPerformanceScope.Text = performanceScope == null || performanceScope.IsAllInstances
                ? "Same as the report"
                : $"{performanceScope} (within the report's instances)";
        }

        /// <summary>Performance for some of the report's instances only - e.g. checks for production and DR, performance for production</summary>
        private void ChoosePerformanceScope()
        {
            using var picker = new Tagging.MultiTagPicker { SelectedTags = performanceScope ?? DBADashTagSet.AllInstances() };
            if (picker.ShowDialog(this) == DialogResult.OK)
            {
                performanceScope = picker.SelectedTags;
                ShowPerformanceScope();
            }
        }

        private void ChooseScope()
        {
            using var picker = new Tagging.MultiTagPicker { SelectedTags = scope ?? DBADashTagSet.AllInstances() };
            if (picker.ShowDialog(this) == DialogResult.OK)
            {
                scope = picker.SelectedTags;
                ShowScope();
            }
        }

        /// <summary>Checks the form and builds the report settings from it.  Shows a message and returns false if something is wrong.</summary>
        /// <param name="requireChannels">False for a preview, which doesn't send anything</param>
        private bool TryCollect(bool requireChannels, out DailyReportConfig result)
        {
            result = null;
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Enter a name for the report.", "Daily report", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (!TimeSpan.TryParseExact(txtTime.Text.Trim(), new[] { "h\\:mm", "hh\\:mm" }, CultureInfo.InvariantCulture, out var time))
            {
                MessageBox.Show("Enter the send time as HH:mm, for example 08:00.", "Daily report", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            var days = chkDays.Select((c, i) => c.Checked ? 1 << i : 0).Sum();
            if (days == 0)
            {
                MessageBox.Show("Select at least one day to send the report on.", "Daily report", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            var interval = RepeatMinutes[Math.Max(cboRepeat.SelectedIndex, 0)];
            TimeSpan? until = null;
            if (interval.HasValue && !string.IsNullOrWhiteSpace(txtUntil.Text))
            {
                if (!TimeSpan.TryParseExact(txtUntil.Text.Trim(), new[] { "h\\:mm", "hh\\:mm" }, CultureInfo.InvariantCulture, out var u))
                {
                    MessageBox.Show("Enter the end time as HH:mm, for example 18:00, or leave it blank to repeat until the end of the day.", "Daily report", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                if (u < time)
                {
                    MessageBox.Show("The end time must be after the send time.", "Daily report", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                until = u;
            }
            var channelIDs = clbChannels.CheckedItems.Cast<ChannelItem>().Select(c => c.Channel.NotificationChannelID).ToList();
            if (requireChannels && channelIDs.Count == 0)
            {
                MessageBox.Show(emailChannels.Count == 0
                    ? "There are no Email notification channels. Add one in the alert configuration first."
                    : "Select at least one email channel.", "Daily report", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (chkHours.Checked && cboHoursFrom.SelectedIndex == cboHoursTo.SelectedIndex)
            {
                MessageBox.Show("The performance hours start and end at the same time.  Choose different hours, or untick Only count to count the whole day.", "Daily report", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (cboTimeZone.SelectedItem == null)
            {
                MessageBox.Show("Select a time zone.", "Daily report", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            var tags = scope ?? DBADashTagSet.AllInstances();
            config.ReportName = txtName.Text.Trim();
            config.IsActive = chkActive.Checked;
            config.ScheduleTime = time;
            config.TimeZone = (string)cboTimeZone.SelectedItem;
            config.DaysOfWeek = days;
            config.IntervalMinutes = interval;
            config.WindowEndTime = until;
            config.ReportType = cboType.SelectedIndex == 1 ? DailyReportType.Changes : DailyReportType.Full;
            config.ApplyToTagID = tags.ApplyToTagID;
            config.ApplyToTagIDs = tags.ApplyToTagID == DBADashTagSet.MultipleTagsID ? tags.TagIDs : null;
            config.ScopeDescription = tags.IsAllInstances ? "{All}" : tags.ToString();
            config.IncludePerformance = chkPerformance.Checked;
            config.IncludeWeekOverWeek = chkWeekOverWeek.Checked;
            config.ChangeThresholdPct = (int)numThreshold.Value;
            settings.IncludeCharts = chkCharts.Checked;
            settings.IncludeTopQueries = chkTopQueries.Checked;
            settings.IncludeActivity = chkActivity.Checked;
            settings.ThresholdsOnlyWhenWorseThanUsual = chkAsUsual.Checked;
            settings.PerformanceHoursFrom = chkHours.Checked ? cboHoursFrom.SelectedIndex : null;
            settings.PerformanceHoursTo = chkHours.Checked ? cboHoursTo.SelectedIndex : null;
            var perfTags = performanceScope ?? DBADashTagSet.AllInstances();
            settings.PerformanceApplyToTagID = perfTags.IsAllInstances ? -1 : perfTags.ApplyToTagID;
            settings.PerformanceApplyToTagIDs = !perfTags.IsAllInstances && perfTags.ApplyToTagID == DBADashTagSet.MultipleTagsID ? perfTags.TagIDs : null;
            settings.PerformanceScopeDescription = perfTags.IsAllInstances ? null : perfTags.ToString();
            settings.IncludeResolved = chkResolved.Checked;
            settings.IncludeAlerts = chkAlerts.Checked;
            settings.IncludeOpenSummary = chkOpenSummary.Checked;
            settings.IncludeAISummary = chkAISummary.Checked;
            settings.PlainTextOnly = chkPlainText.Checked;
            config.Settings = settings.Clone();
            config.Channels = emailChannels.Where(c => channelIDs.Contains(c.NotificationChannelID)).ToList();
            result = config;
            return true;
        }

        private void Save()
        {
            if (!TryCollect(requireChannels: true, out var report)) return;
            try
            {
                DailyReportRepository.Save(connectionString, report);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (SqlException ex)
            {
                // The procs THROW a readable message for validation failures (duplicate name, bad time zone, ...)
                MessageBox.Show(ex.Message, "Daily report not saved", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Builds the report from the form as it is now (saved or not) and opens it in the default browser.
        /// Nothing is saved or sent.  The data is read from the repository now, so the preview matches what the email would show.
        /// </summary>
        private async Task PreviewAsync()
        {
            if (!TryCollect(requireChannels: false, out var report)) return;

            btnPreview.Enabled = false;
            Cursor = Cursors.WaitCursor;
            try
            {
                var content = (await Task.Run(() => DailyReportRunner.BuildAsync(report, connectionString, DateTime.UtcNow))).Content;
                var path = Path.Combine(Path.GetTempPath(), $"DBADash_DailyReport_Preview_{Guid.NewGuid():N}.html");
                File.WriteAllText(path, PreviewDocument(content), Encoding.UTF8);
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Preview daily report", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                Cursor = Cursors.Default;
                btnPreview.Enabled = true;
            }
        }

        /// <summary>Wraps the email body in a page, with the subject as the title and a note that this is a preview</summary>
        private static string PreviewDocument(DailyReportContent content) =>
            content.PreviewHtml("<div style=\"font-family:Segoe UI,Arial,sans-serif;color:#666;font-size:12px;padding:8px 16px;background:#fff5e4;border-bottom:1px solid #e49325\">Preview only - nothing was sent. Subject: " +
                                WebUtility.HtmlEncode(content.Subject) + "</div>");

        private class ChannelItem
        {
            public ChannelItem(DailyReportChannelRef channel) => Channel = channel;
            public DailyReportChannelRef Channel { get; }
            public override string ToString() => Channel.IsActive ? Channel.ChannelName : $"{Channel.ChannelName} (disabled)";
        }
    }
}
