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
                // checkbox and shift the boxes up to reclaim the space.
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

        private void UpdateSecondsVisibility()
        {
            try
            {
                var minWidth = IncludeSeconds ? 380 : 278;
                this.MinimumSize = new Size(minWidth, 0);
                if (this.Size.Width < minWidth) this.Size = new Size(minWidth, this.Size.Height);

                if (numSeconds != null)
                {
                    numSeconds.Visible = IncludeSeconds;
                    lblSeconds.Visible = IncludeSeconds;
                    if (!IncludeSeconds)
                    {
                        // clear any seconds value when hidden to avoid accidental carry-through
                        try { numSeconds.Value = 0; } catch { }
                    }
                    else
                    {
                        // Ensure seconds controls align with minutes/hours when shown
                        try
                        {
                            // Align vertical position with minutes label/box
                            numSeconds.Top = numMinutes.Top;
                            lblSeconds.Top = lblMinutes.Top;
                            // Place seconds immediately to the right of minutes label
                            var rightOfMinutes = lblMinutes.Left + lblMinutes.Width + 6;
                            numSeconds.Left = Math.Max(numSeconds.Left, rightOfMinutes);
                            lblSeconds.Left = numSeconds.Left + numSeconds.Width + 6;
                        }
                        catch
                        {
                            // ignore layout errors at design time
                        }
                    }
                }
            }
            catch
            {
                // ignore designer/runtime differences
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

        /// <summary>The edited duration in minutes, or null when "Not set" is enabled and checked.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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

        private decimal TotalMinutes
        {
            get => (numDays.Value * 1440m) + (numHours.Value * 60m) + numMinutes.Value + (GetSecondsValue() / 60m);
            set
            {
                // value is minutes (may include fractional minutes representing seconds)
                var total = value < 0 ? 0m : value;
                var days = Math.Floor(total / 1440m);
                var remainder = total - (days * 1440m);
                var hours = Math.Floor(remainder / 60m);
                var remainder2 = remainder - (hours * 60m);
                var minutes = Math.Floor(remainder2);
                var seconds = (remainder2 - minutes) * 60m;

                numDays.Value = Math.Min(days, numDays.Maximum);
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