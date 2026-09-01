using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using DBADashGUI.Theme;

namespace DBADashGUI.Pickers
{
    /// <summary>
    /// Drop-down editing control used by <see cref="MinuteDurationEditor"/>.
    /// Presents separate day / hour / minute entry boxes for a duration that is
    /// stored as a number of minutes, e.g. [7] days [4] hrs [1] min.
    /// When <paramref name="allowNull"/> is set, a "Not set" checkbox is shown so the
    /// user can explicitly clear the value (null) rather than entering a duration.
    /// </summary>
    public sealed partial class DurationDropDown : UserControl
    {
        private readonly bool allowNull;
        private readonly IWindowsFormsEditorServiceCloser closer;
        private bool includeSeconds = false;
        private bool allowDays = true;

        /// <summary>Small abstraction so the control can close the drop-down when Enter is pressed.</summary>
        internal interface IWindowsFormsEditorServiceCloser
        {
            void CloseDropDown();
        }

        /// <summary>Parameterless constructor so the control can be dropped from the Toolbox / opened in the designer.</summary>
        public DurationDropDown() : this(null, false)
        {
        }

        internal DurationDropDown(IWindowsFormsEditorServiceCloser closer, bool allowNull)
        {
            this.closer = closer;
            this.allowNull = allowNull;

            // Allow the control's BackColor = Transparent (set in the designer) to actually
            // show the parent's background through, rather than painting a solid colour.
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);

            InitializeComponent();

            // Apply initial seconds visibility/layout based on IncludeSeconds (may be changed later by designer code)
            UpdateSecondsVisibility();

            if (!allowNull)
            {
                // The designer lays out the control with the "Not set" checkbox at the
                // top and the entry boxes below it. When null isn't allowed we hide the
                // checkbox and shift the boxes up to reclaim the space. This sets the
                // initial (design-scale) layout; LayoutHorizontal re-normalises the
                // vertical position at runtime once the control has been DPI-scaled.
                const int shift = 28;
                chkNotSet.Visible = false;
                // include seconds controls when shifting up to keep all inputs aligned on the same Y plane
                var controls = new System.Collections.Generic.List<Control> { numDays, lblDays, numHours, lblHours, numMinutes, lblMinutes };
                if (IncludeSeconds)
                {
                    controls.Add(numSeconds);
                    controls.Add(lblSeconds);
                }
                foreach (Control c in controls)
                {
                    c.Top -= shift;
                }
                Size = new Size(Width, Height - shift);
            }

            // Skip theming inside the WinForms designer: CurrentTheme isn't initialised
            // at design time, and an exception here would drop the control from the Toolbox.
            if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
            {
                this.ApplyTheme();
            }
        }

        private void ChkNotSet_CheckedChanged(object sender, EventArgs e)
        {
            var enabled = !chkNotSet.Checked;
            numDays.Enabled = enabled;
            numHours.Enabled = enabled;
            numMinutes.Enabled = enabled;
            if (numSeconds != null) numSeconds.Enabled = enabled && IncludeSeconds;
        }

        /// <summary>
        /// When true the control exposes seconds entry boxes. Default false for backward compatibility.
        /// </summary>
        [Category("Behavior")]
        [Description("Show seconds input fields in addition to days/hours/minutes.")]
        [DefaultValue(false)]
        public bool IncludeSeconds
        {
            get => includeSeconds;
            set
            {
                if (includeSeconds == value) return;
                includeSeconds = value;
                UpdateSecondsVisibility();
            }
        }

        /// <summary>
        /// When false the days box is hidden and the duration is expressed in hours/minutes(/seconds) only,
        /// with the hours box allowed to exceed 23. Default true. Use for durations that are always short.
        /// </summary>
        [Category("Behavior")]
        [Description("Show the days input field. Set false for durations that are always under a day (or measured in hours).")]
        [DefaultValue(true)]
        public bool AllowDays
        {
            get => allowDays;
            set
            {
                if (allowDays == value) return;
                allowDays = value;
                numDays.Visible = value;
                lblDays.Visible = value;
                // Without a days box, hours must be able to hold the whole duration (>23).
                numHours.Maximum = value ? 23m : 1000000m;
                if (!value)
                {
                    try { numDays.Value = 0; } catch { }
                }
                LayoutHorizontal();
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // The constructor lays the boxes out using design-DPI (96) child sizes. By the time
            // the control is shown it has been scaled to the current DPI and the AutoSize labels
            // have re-measured (often a shade wider), so re-run the layout to size the control to
            // its final content - otherwise the right-most label is clipped on scaled sessions.
            LayoutHorizontal();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            // A DPI change re-scales the font; re-flow so widths track the new label sizes.
            LayoutHorizontal();
        }

        private void UpdateSecondsVisibility()
        {
            if (numSeconds != null)
            {
                numSeconds.Visible = IncludeSeconds;
                lblSeconds.Visible = IncludeSeconds;
                if (!IncludeSeconds)
                {
                    // clear any seconds value when hidden to avoid accidental carry-through
                    try { numSeconds.Value = 0; } catch { }
                }
            }
            LayoutHorizontal();
        }

        /// <summary>
        /// Positions the day / hour / minute (/ second) boxes left-to-right, skipping the hidden ones
        /// (days when <see cref="AllowDays"/> is false, seconds when <see cref="IncludeSeconds"/> is false),
        /// and sizes the control to fit. In the inline use it also normalises the vertical position (see below).
        /// </summary>
        private void LayoutHorizontal()
        {
            try
            {
                // Gaps are authored in logical (96 DPI) units; scale them so spacing holds up
                // on high-DPI / scaled (e.g. RDP) sessions instead of staying fixed in pixels.
                var numToLabelGap = LogicalToDeviceUnits(2);
                var groupGap = LogicalToDeviceUnits(8);
                var x = 0;

                void Place(NumericUpDown num, Label lbl)
                {
                    num.Left = x;
                    lbl.Left = x + num.Width + numToLabelGap;
                    x = lbl.Left + lbl.Width + groupGap;
                }

                if (AllowDays) Place(numDays, lblDays);
                Place(numHours, lblHours);
                Place(numMinutes, lblMinutes);
                if (IncludeSeconds && numSeconds != null)
                {
                    Place(numSeconds, lblSeconds);
                }

                // Vertical normalisation for the inline use (no "Not set" checkbox). A NumericUpDown cannot
                // shrink below its PreferredHeight, so when the form runs below its design scale (100%, or
                // Azure Bastion web RDP, versus the 125% the layout was authored at) AutoScaleMode.Font
                // budgets each box shorter than it can actually be and pushes its Top negative to keep it
                // "centered" - clipping the top border and up-spin arrow. Pin the row back to Top = 0 (where
                // it sits at design scale, aligned with the external row label) and grow the control to fit
                // the boxes' real height so nothing is clipped.
                if (!chkNotSet.Visible)
                {
                    var labelOffset = LogicalToDeviceUnits(4);   // labels sit slightly below the boxes (baseline)
                    foreach (var num in new[] { numDays, numHours, numMinutes, numSeconds })
                    {
                        if (num != null) num.Top = 0;
                    }
                    foreach (var lbl in new[] { lblDays, lblHours, lblMinutes, lblSeconds })
                    {
                        if (lbl != null) lbl.Top = labelOffset;
                    }
                }
                else if (IncludeSeconds && numSeconds != null)
                {
                    // Drop-down use: keep seconds aligned with the minutes row (which sits below the checkbox).
                    numSeconds.Top = numMinutes.Top;
                    lblSeconds.Top = lblMinutes.Top;
                }

                var contentBottom = 0;
                foreach (Control c in Controls)
                {
                    if (c.Visible && c.Bottom > contentBottom) contentBottom = c.Bottom;
                }
                var requiredHeight = contentBottom + LogicalToDeviceUnits(2);

                MinimumSize = new Size(x, requiredHeight);
                if (Width != x) Width = x;
                if (Height < requiredHeight) Height = requiredHeight;
            }
            catch
            {
                // ignore designer/runtime layout differences
            }
        }

        private void Num_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                closer?.CloseDropDown();
                e.Handled = true;
            }
        }

        /// <summary>
        /// The edited duration in minutes, or null when "Not set" is enabled and checked.
        /// Serialized by the designer (when non-zero) so an initial value set on the design surface is persisted.
        /// </summary>
        [Category("Behavior")]
        [Description("The duration in minutes. Set on the design surface to give the control an initial value.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public decimal? Value
        {
            get => allowNull && chkNotSet.Checked ? null : TotalMinutes;
            set
            {
                if (value == null)
                {
                    if (allowNull) { chkNotSet.Checked = true; }
                    TotalMinutes = 0;
                }
                else
                {
                    if (allowNull) { chkNotSet.Checked = false; }
                    TotalMinutes = value.Value;
                }
            }
        }

        // Designer support: only emit Value into InitializeComponent when a duration was actually set.
        private bool ShouldSerializeValue() => (Value ?? 0m) != 0m;

        private void ResetValue() => Value = 0m;

        /// <summary>
        /// The edited duration expressed in seconds, or null when "Not set" is checked. Convenience over
        /// <see cref="Value"/> (which is in minutes) for callers whose stored unit is seconds - avoids a
        /// minutes-to-seconds conversion (and the rounding mistakes that come with it) at the call site.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public decimal? TotalSeconds
        {
            get => Value * 60m;
            set => Value = value / 60m;
        }

        private decimal TotalMinutes
        {
            get => (numDays.Value * 1440m) + (numHours.Value * 60m) + numMinutes.Value + (GetSecondsValue() / 60m);
            set
            {
                // value is minutes (may include fractional minutes representing seconds)
                var total = value < 0 ? 0m : value;

                // With the days box hidden, fold whole days into the (uncapped) hours box instead of splitting them out.
                var days = 0m;
                var remainder = total;
                if (allowDays)
                {
                    days = Math.Floor(total / 1440m);
                    remainder = total - (days * 1440m);
                }
                var hours = Math.Floor(remainder / 60m);
                var remainder2 = remainder - (hours * 60m);
                var minutes = Math.Floor(remainder2);
                var seconds = (remainder2 - minutes) * 60m;

                numDays.Value = allowDays ? Math.Min(days, numDays.Maximum) : 0m;
                numHours.Value = Math.Min(hours, numHours.Maximum);
                numMinutes.Value = Math.Min(minutes, numMinutes.Maximum);
                SetSecondsValue(decimal.Truncate(seconds));
            }
        }

        private decimal GetSecondsValue()
        {
            try
            {
                return numSeconds?.Value ?? 0m;
            }
            catch
            {
                return 0m;
            }
        }

        private void SetSecondsValue(decimal val)
        {
            try
            {
                if (numSeconds != null)
                {
                    numSeconds.Value = Math.Min(val, numSeconds.Maximum);
                }
            }
            catch
            {
                // ignore
            }
        }
    }
}