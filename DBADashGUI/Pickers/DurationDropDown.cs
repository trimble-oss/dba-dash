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
    internal sealed class DurationDropDown : UserControl
    {
        private readonly NumericUpDown numDays = new() { Minimum = 0, Maximum = 1000000, Width = 60 };
        private readonly NumericUpDown numHours = new() { Minimum = 0, Maximum = 23, Width = 48 };
        private readonly NumericUpDown numMinutes = new() { Minimum = 0, Maximum = 59, Width = 48 };
        private readonly CheckBox chkNotSet;
        private readonly bool allowNull;
        private readonly IWindowsFormsEditorServiceCloser closer;

        /// <summary>Small abstraction so the control can close the drop-down when Enter is pressed.</summary>
        internal interface IWindowsFormsEditorServiceCloser
        {
            void CloseDropDown();
        }

        public DurationDropDown(IWindowsFormsEditorServiceCloser closer = null, bool allowNull = false)
        {
            this.closer = closer;
            this.allowNull = allowNull;

            var boxesTop = 8;
            if (allowNull)
            {
                chkNotSet = new CheckBox
                {
                    Text = "Not set",
                    AutoSize = true,
                    Location = new Point(8, 6)
                };
                chkNotSet.CheckedChanged += ChkNotSet_CheckedChanged;
                Controls.Add(chkNotSet);
                boxesTop = 34;
            }

            numDays.Location = new Point(8, boxesTop);
            numHours.Location = new Point(110, boxesTop);
            numMinutes.Location = new Point(196, boxesTop);

            Controls.Add(numDays);
            Controls.Add(NewLabel("days", 70, boxesTop + 4));
            Controls.Add(numHours);
            Controls.Add(NewLabel("hrs", 160, boxesTop + 4));
            Controls.Add(numMinutes);
            Controls.Add(NewLabel("mins", 246, boxesTop + 4));

            Size = new Size(290, boxesTop + 35);

            foreach (var num in new[] { numDays, numHours, numMinutes })
            {
                num.KeyDown += Num_KeyDown;
            }

            this.ApplyTheme();
        }

        private static Label NewLabel(string text, int x, int y) => new()
        {
            Text = text,
            AutoSize = true,
            Location = new Point(x, y)
        };

        private void ChkNotSet_CheckedChanged(object sender, EventArgs e)
        {
            var enabled = !chkNotSet.Checked;
            numDays.Enabled = enabled;
            numHours.Enabled = enabled;
            numMinutes.Enabled = enabled;
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
            get => (numDays.Value * 1440m) + (numHours.Value * 60m) + numMinutes.Value;
            set
            {
                var total = value < 0 ? 0 : decimal.Truncate(value);
                var days = Math.Floor(total / 1440m);
                var remainder = total - (days * 1440m);
                var hours = Math.Floor(remainder / 60m);
                var minutes = remainder - (hours * 60m);

                numDays.Value = Math.Min(days, numDays.Maximum);
                numHours.Value = Math.Min(hours, numHours.Maximum);
                numMinutes.Value = Math.Min(minutes, numMinutes.Maximum);
            }
        }
    }
}
