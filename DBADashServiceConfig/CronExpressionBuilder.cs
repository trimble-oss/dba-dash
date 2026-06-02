using DBADash;
using System;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using DBADashGUI.Theme;

namespace DBADashServiceConfig
{
    /// <summary>
    /// WinForms dialog that helps construct and edit Quartz-style cron expressions.
    /// The control delegates parsing and day-token utilities to <see cref="CronParser"/>.
    /// </summary>
    public partial class CronExpressionBuilder : Form
    {
        private CronParser.FrequencyMode SelectedFrequencyMode => (CronParser.FrequencyMode)cboFrequency.SelectedIndex;

        private bool _suspendUiUpdates = false;

        /// <summary>
        /// The current cron expression produced by the builder.
        /// </summary>
        public string CronExpression { get; private set; }

        /// <summary>
        /// Create a new builder. Optionally provide an existing cron expression to initialize the form.
        /// </summary>
        /// <param name="currentExpression">Existing cron expression to load into the builder (may be null).</param>
        public CronExpressionBuilder(string currentExpression = null)
        {
            InitializeComponent();
            CronExpression = currentExpression;
        }

        /// <summary>
        /// Build a Quartz cron expression for the "every N seconds" frequency using UI state.
        /// </summary>
        private string BuildEveryNSecondsExpression()
        {
            var n = (int)numInterval.Value;
            var days = GetSelectedDayNames();
            // seconds-based expressions generally ignore days; if days specified, include day-of-week
            if (days.Length == 0 || days.Length == CronParser.DayNames().Length)
            {
                var baseSec = numSecondBase.Visible ? (int)numSecondBase.Value : dtpTime.Value.Second;
                return $"{baseSec}/{n} * * * * ?"; // e.g. "5/10 * * * * ?"
            }
            var baseSecDays = numSecondBase.Visible ? (int)numSecondBase.Value : dtpTime.Value.Second;
            var compressed = CompressDayTokens(days);
            if (compressed.Length == 1 && compressed[0] == "*")
            {
                return $"{baseSecDays}/{n} * * * * ?";
            }
            return $"{baseSecDays}/{n} * * ? * {string.Join(',', compressed)}";
        }

        /// <summary>
        /// Initialize the dialog controls and, if an initial expression is provided, attempt to map it into the UI.
        /// </summary>
        private void CronExpressionBuilder_Load(object sender, EventArgs e)
        {
            cboFrequency.Items.AddRange(new object[]
            {
                "None (Disabled)",
                "Every N Seconds",
                "Every N Minutes",
                "Every N Hours",
                "Daily at Time",
                "Weekly on Day(s)",
                "Custom"
            });
            cboFrequency.SelectedIndex = (int)CronParser.FrequencyMode.Custom; // Default to custom

            chkDaysOfWeek.Items.AddRange(new object[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" });
            chkDaysOfWeek.ItemCheck += (_, _) => BeginInvoke(new Action(SafeUpdatePreview));

            numInterval.Minimum = 1;
            numInterval.Maximum = 59;
            numInterval.Value = 1;

            numSecondBase.Value = 0;
            numMinuteBase.Value = 0;
            numHourBase.Value = 0;

            dtpTime.Format = DateTimePickerFormat.Time;
            dtpTime.ShowUpDown = true;
            dtpTime.Value = DateTime.Today.AddHours(3); // Default 3:00 AM

            if (!string.IsNullOrEmpty(CronExpression))
            {
                if (!TryParseIntoBuilder(CronExpression))
                {
                    // Show unsupported expressions as Custom
                    cboFrequency.SelectedIndex = (int)CronParser.FrequencyMode.Custom;
                    txtCustom.Text = CronExpression;
                }
            }
            else
            {
                cboFrequency.SelectedIndex = (int)CronParser.FrequencyMode.None; // None
                // ensure no days are selected by default
                ClearAllDays();
            }

            UpdateVisibility();
            UpdatePreview();

            // Apply application theme
            this.ApplyTheme();
            txtCron.BackColor = this.BackColor;
            chkDaysOfWeek.BackColor = txtCustom.BackColor;
        }

        /// <summary>
        /// Frequency selection changed - update control visibility and preview.
        /// </summary>
        private void CboFrequency_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Capture current preview expression before switching, so we can use it as a starting point for Custom
            var currentPreview = txtCron.Text;
            UpdateVisibility();
            // If user switched to Custom, populate it with the current expression
            if (SelectedFrequencyMode == CronParser.FrequencyMode.Custom)
            {
                txtCustom.Text = currentPreview;
            }
            SafeUpdatePreview();
        }

        /// <summary>
        /// Update control visibility and enabled state based on the selected frequency.
        /// </summary>
        private void UpdateVisibility()
        {
            var freq = (int)SelectedFrequencyMode;
            var isInterval = SelectedFrequencyMode is CronParser.FrequencyMode.EveryNSeconds or CronParser.FrequencyMode.EveryNMinutes or CronParser.FrequencyMode.EveryNHours;
            // 0=None, 1=N Seconds, 2=N Minutes, 3=N Hours, 4=Daily, 5=Weekly, 6=Custom
            numInterval.Visible = isInterval;
            lblInterval.Visible = isInterval;
            // Cap interval to 23 when Hours selected (index 3), otherwise allow up to 59
            numInterval.Maximum = freq == 3 ? 23 : 59;
            if (numInterval.Value > numInterval.Maximum)
            {
                numInterval.Value = numInterval.Maximum;
            }
            // Show dtpTime only for Daily and Weekly
            dtpTime.Visible = SelectedFrequencyMode is CronParser.FrequencyMode.Daily or CronParser.FrequencyMode.Weekly;
            lblTime.Visible = SelectedFrequencyMode is CronParser.FrequencyMode.Daily or CronParser.FrequencyMode.Weekly;
            lblOffset.Visible = isInterval;
            // Show base numeric controls for seconds/minutes/hours only for interval modes to avoid overlap with dtpTime
            numSecondBase.Visible = isInterval;
            numMinuteBase.Visible = isInterval;
            numHourBase.Visible = isInterval;
            // Enable/disable based on applicability
            numSecondBase.Enabled = isInterval;
            numMinuteBase.Enabled = SelectedFrequencyMode is CronParser.FrequencyMode.EveryNMinutes or CronParser.FrequencyMode.EveryNHours;
            numHourBase.Enabled = SelectedFrequencyMode is CronParser.FrequencyMode.EveryNHours;
            if (!numSecondBase.Enabled) numSecondBase.Value = 0;
            if (!numMinuteBase.Enabled) numMinuteBase.Value = 0;
            if (!numHourBase.Enabled) numHourBase.Value = 0;
            // Show day-of-week selector for seconds, minutes, hours and weekly frequencies
            chkDaysOfWeek.Visible = SelectedFrequencyMode is CronParser.FrequencyMode.EveryNSeconds or CronParser.FrequencyMode.EveryNMinutes or CronParser.FrequencyMode.EveryNHours or CronParser.FrequencyMode.Weekly;
            // If days control is hidden, ensure no hidden selections remain
            if (!chkDaysOfWeek.Visible)
            {
                ClearAllDays();
            }
            lblDay.Visible = SelectedFrequencyMode is CronParser.FrequencyMode.EveryNSeconds or CronParser.FrequencyMode.EveryNMinutes or CronParser.FrequencyMode.EveryNHours or CronParser.FrequencyMode.Weekly;
            txtCustom.Visible = SelectedFrequencyMode == CronParser.FrequencyMode.Custom;
            lblCustom.Visible = SelectedFrequencyMode == CronParser.FrequencyMode.Custom;

        }

        /// <summary>
        /// Update the preview text and computed next run times for the currently built expression.
        /// </summary>
        private void UpdatePreview()
        {
            var expression = BuildExpression();
            txtCron.Text = expression ?? string.Empty;
            if (string.IsNullOrEmpty(expression))
            {
                lblPreview.Text = "No expression";
                return;
            }
            if (Quartz.CronExpression.IsValidExpression(expression))
            {
                string desc;
                try { desc = CronExpressionDescriptor.ExpressionDescriptor.GetDescription(expression); }
                catch (Exception ex)
                {
                    Trace.TraceWarning("Cron description generation failed: {0}", ex.Message);
                    desc = "Valid expression";
                }

                // Append next 3 run times (one per line)
                try
                {
                    var cron = new Quartz.CronExpression(expression);
                    var after = DateTimeOffset.Now;
                    var nextRuns = new System.Text.StringBuilder();
                    for (int i = 0; i < 3; i++)
                    {
                        var next = cron.GetNextValidTimeAfter(after);
                        if (!next.HasValue)
                        {
                            break;
                        }
                        // format using local time
                        nextRuns.AppendLine(next.Value.LocalDateTime.ToString("G"));
                        after = next.Value;
                    }
                    lblPreview.Text = desc + (nextRuns.Length > 0 ? Environment.NewLine + "Next run times:" + Environment.NewLine + nextRuns.ToString().TrimEnd() : string.Empty);
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("Cron next-run calculation failed: {0}", ex.Message);
                    lblPreview.Text = desc;
                }
            }
            else
            {
                lblPreview.Text = "Invalid cron expression";
            }
        }

        /// <summary>
        /// Build the cron expression string for the current UI state.
        /// </summary>
        private string BuildExpression()
        {
            return SelectedFrequencyMode switch
            {
                CronParser.FrequencyMode.None => string.Empty,
                CronParser.FrequencyMode.EveryNSeconds => BuildEveryNSecondsExpression(),
                CronParser.FrequencyMode.EveryNMinutes => BuildEveryNMinutesExpression(),
                CronParser.FrequencyMode.EveryNHours => BuildEveryNHoursExpression(),
                CronParser.FrequencyMode.Daily => $"{dtpTime.Value.Second} {dtpTime.Value.Minute} {dtpTime.Value.Hour} * * ?",
                CronParser.FrequencyMode.Weekly => BuildWeeklyExpression(),
                CronParser.FrequencyMode.Custom => txtCustom.Text.Trim(),
                _ => string.Empty
            };
        }

        /// <summary>
        /// Build a weekly cron expression using the selected days and time controls.
        /// </summary>
        private string BuildWeeklyExpression()
        {
            var days = GetSelectedDayNames();
            if (days.Length == 0) return string.Empty;
            var compressed = CompressDayTokens(days);
            if (compressed.Length == 1 && compressed[0] == "*")
            {
                return $"{dtpTime.Value.Second} {dtpTime.Value.Minute} {dtpTime.Value.Hour} * * ?";
            }
            return $"{dtpTime.Value.Second} {dtpTime.Value.Minute} {dtpTime.Value.Hour} ? * {string.Join(',', compressed)}";
        }

        // Use canonical day names from shared CronParser
        // CronParser.DayNames()

        // Returns selected day names (e.g. ["MON","WED"]) in the DayNames() form
        /// <summary>
        /// Return selected day tokens in the canonical 3-letter form used by CronParser.DayNames().
        /// </summary>
        private string[] GetSelectedDayNames()
        {
            var names = CronParser.DayNames();
            return Enumerable.Range(0, chkDaysOfWeek.Items.Count)
                .Where(i => chkDaysOfWeek.GetItemChecked(i))
                .Select(i => names[i])
                .ToArray();
        }

        // Set checkbox states from an array of day name tokens (e.g. ["MON","TUE"]).
        /// <summary>
        /// Set the day-of-week checkboxes from canonical day name tokens (e.g. "MON","TUE").
        /// </summary>
        /// <param name="selectedDays">Canonical day tokens to select.</param>
        private void SetSelectedDaysFromNames(string[] selectedDays)
        {
            var names = CronParser.DayNames();
            for (int i = 0; i < chkDaysOfWeek.Items.Count; i++)
            {
                chkDaysOfWeek.SetItemChecked(i, selectedDays.Contains(names[i], StringComparer.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Normalize user-provided day tokens via the shared CronParser implementation.
        /// </summary>
        private string[] NormalizeDayTokens(string[] tokens) => CronParser.NormalizeDayTokens(tokens);

        // NameToIndex logic moved into CronParser; keep local method removed to avoid duplication.

        // Compress an expanded list of day tokens (e.g. [MON,TUE,WED,THU,FRI]) into simplified tokens like [MON-FRI].
        // Preserves order and handles wrap-around ranges.
        // Compression delegated to shared CronParser
        internal string[] CompressDayTokens(string[] expanded) => CronParser.CompressDayTokens(expanded);

        /// <summary>
        /// Clear all day-of-week checkbox selections.
        /// </summary>
        private void ClearAllDays()
        {
            for (int i = 0; i < chkDaysOfWeek.Items.Count; i++)
            {
                chkDaysOfWeek.SetItemChecked(i, false);
            }
        }

        /// <summary>
        /// Build a cron expression for the "every N minutes" frequency.
        /// </summary>
        private string BuildEveryNMinutesExpression()
        {
            var n = (int)numInterval.Value;
            var days = GetSelectedDayNames();
            // If 0 or all 7 days selected, treat as no day restriction
            if (days.Length == 0 || days.Length == CronParser.DayNames().Length)
            {
                var baseMin = numMinuteBase.Visible ? (int)numMinuteBase.Value : dtpTime.Value.Minute;
                var baseSec = numSecondBase.Visible ? (int)numSecondBase.Value : dtpTime.Value.Second;
                return $"{baseSec} {baseMin}/{n} * * * ?";
            }
            var baseMinDays = numMinuteBase.Visible ? (int)numMinuteBase.Value : dtpTime.Value.Minute;
            var baseSecDays = numSecondBase.Visible ? (int)numSecondBase.Value : dtpTime.Value.Second;
            var compressed = CompressDayTokens(days);
            if (compressed.Length == 1 && compressed[0] == "*")
            {
                return $"{baseSecDays} {baseMinDays}/{n} * * * ?";
            }
            return $"{baseSecDays} {baseMinDays}/{n} * ? * {string.Join(',', compressed)}"; // every N minutes on selected days
        }

        /// <summary>
        /// Build a cron expression for the "every N hours" frequency.
        /// </summary>
        private string BuildEveryNHoursExpression()
        {
            var n = (int)numInterval.Value;
            var days = GetSelectedDayNames();
            // If 0 or all 7 days selected, treat as no day restriction
            if (days.Length == 0 || days.Length == CronParser.DayNames().Length)
            {
                var baseHour = numHourBase.Visible ? (int)numHourBase.Value : dtpTime.Value.Hour;
                var baseMin = numMinuteBase.Visible ? (int)numMinuteBase.Value : dtpTime.Value.Minute;
                var baseSec = numSecondBase.Visible ? (int)numSecondBase.Value : dtpTime.Value.Second;
                return $"{baseSec} {baseMin} {baseHour}/{n} * * ?"; // every N hours, start at base
            }
            var baseHourDays = numHourBase.Visible ? (int)numHourBase.Value : dtpTime.Value.Hour;
            var baseMinDays = numMinuteBase.Visible ? (int)numMinuteBase.Value : dtpTime.Value.Minute;
            var baseSecDays = numSecondBase.Visible ? (int)numSecondBase.Value : dtpTime.Value.Second;
            var compressed = CompressDayTokens(days);
            if (compressed.Length == 1 && compressed[0] == "*")
            {
                return $"{baseSecDays} {baseMinDays} {baseHourDays}/{n} * * ?";
            }
            return $"{baseSecDays} {baseMinDays} {baseHourDays}/{n} ? * {string.Join(',', compressed)}"; // every N hours on selected days
        }

        /// <summary>
        /// Attempt to map a cron expression back to builder controls. Returns true if successful.
        /// Parsing is delegated to <see cref="CronParser.TryParseCronState"/>; unsupported
        /// expressions will return false so callers can fallback to Custom.
        /// </summary>
        /// <param name="cron">Cron expression to parse into the UI.</param>
        /// <returns>True when the expression was successfully mapped to UI state.</returns>
        private bool TryParseIntoBuilder(string cron)
        {
            if (!CronParser.TryParseCronState(cron, out var parsedState)) return false;
            // Map CronParser.ParsedCronState to the UI ApplyParsedState
            ApplyParsedState(parsedState);
            return true;
        }

        // Parsing is delegated to DBADash.CronParser.TryParseCronState. See CronParser for supported shapes.

        /// <summary>
        /// Commit the current expression and close the dialog with OK if valid.
        /// </summary>
        private void BttnOK_Click(object sender, EventArgs e)
        {
            var expression = BuildExpression();
            if (!string.IsNullOrEmpty(expression) && !Quartz.CronExpression.IsValidExpression(expression))
            {
                MessageBox.Show("The cron expression is not valid.", "Invalid Expression", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            CronExpression = expression;
            DialogResult = DialogResult.OK;
        }


        private void BttnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void NumInterval_ValueChanged(object sender, EventArgs e) => UpdatePreview();

        private void DtpTime_ValueChanged(object sender, EventArgs e) => UpdatePreview();



        /// <summary>
        /// Custom expression text changed - refresh preview.
        /// </summary>
        private void TxtCustom_TextChanged(object sender, EventArgs e) => UpdatePreview();

        private void SafeUpdatePreview()
        {
            if (_suspendUiUpdates) return;
            UpdatePreview();
        }

        /// <summary>
        /// Apply a parsed cron state to the UI controls. Values are clamped to control ranges and UI
        /// update events are suppressed while the values are applied.
        /// </summary>
        /// <param name="state">Parsed cron state produced by <see cref="CronParser.TryParseCronState"/>.</param>
        public void ApplyParsedState(CronParser.ParsedCronState state)
        {
            if (state == null) return;
            try
            {
                _suspendUiUpdates = true;
                this.SuspendLayout();

                cboFrequency.SelectedIndex = (int)state.Mode;

                // Ensure numeric values are within range
                if (state.Interval < numInterval.Minimum) state.Interval = (int)numInterval.Minimum;
                if (state.Interval > numInterval.Maximum) state.Interval = (int)numInterval.Maximum;
                numInterval.Value = state.Interval == 0 ? numInterval.Minimum : state.Interval;

                if (numSecondBase.Enabled)
                {
                    var sec = state.BaseSecond;
                    if (sec < numSecondBase.Minimum) sec = (int)numSecondBase.Minimum;
                    if (sec > numSecondBase.Maximum) sec = (int)numSecondBase.Maximum;
                    numSecondBase.Value = sec;
                }
                if (numMinuteBase.Enabled)
                {
                    var min = state.BaseMinute;
                    if (min < numMinuteBase.Minimum) min = (int)numMinuteBase.Minimum;
                    if (min > numMinuteBase.Maximum) min = (int)numMinuteBase.Maximum;
                    numMinuteBase.Value = min;
                }
                if (numHourBase.Enabled)
                {
                    var hr = state.BaseHour;
                    if (hr < numHourBase.Minimum) hr = (int)numHourBase.Minimum;
                    if (hr > numHourBase.Maximum) hr = (int)numHourBase.Maximum;
                    numHourBase.Value = hr;
                }

                // Set dtpTime if we have a coherent time
                try
                {
                    dtpTime.Value = DateTime.Today.AddHours(state.BaseHour).AddMinutes(state.BaseMinute).AddSeconds(state.BaseSecond);
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("Setting dtpTime failed: {0}", ex.Message);
                }

                // Days
                ClearAllDays();
                if (state.SelectedDays != null && state.SelectedDays.Length > 0)
                {
                    SetSelectedDaysFromNames(state.SelectedDays);
                }

                // Custom expression
                if (!string.IsNullOrEmpty(state.CustomExpression)) txtCustom.Text = state.CustomExpression;

                UpdateVisibility();
            }
            finally
            {
                this.ResumeLayout();
                _suspendUiUpdates = false;
                UpdatePreview();
            }
        }
    }
}
