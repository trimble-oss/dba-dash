using DBADashGUI.Theme;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace DBADashGUI.XETrace
{
    /// <summary>
    /// Load-time prompt dialog.  When a template has filters flagged to prompt, this asks for each value up front (in one
    /// dialog), pre-filled with the stored value as the default.  Returns the entered values in order, or null if
    /// cancelled (so the template load is abandoned and the current configuration is left untouched).
    /// Microsecond duration prompts (duration, cpu_time) are entered as a number + unit and returned as microseconds,
    /// matching the filter editor so the user never has to type/read raw microseconds.
    /// </summary>
    internal static class XETemplatePromptForm
    {
        public static List<string> Prompt(IWin32Window owner, string templateName,
            IReadOnlyList<(string Label, string Default, bool IsDuration)> prompts)
        {
            if (prompts == null || prompts.Count == 0) return new List<string>();

            using var form = new Form
            {
                Text = string.IsNullOrEmpty(templateName) ? "Trace template" : $"Load template - {templateName}",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.SizableToolWindow,
                MinimizeBox = false,
                MaximizeBox = false,
                ClientSize = new Size(420, Math.Min(120 + prompts.Count * 56, 520)),
                ShowInTaskbar = false
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                AutoScroll = true,
                Padding = new Padding(12)
            };
            layout.Controls.Add(new Label
            {
                Text = "Enter values for this template:",
                Dock = DockStyle.Top,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8)
            });

            // Each entry resolves its row to the value string to store (microseconds for a duration prompt), or an error.
            var getters = new List<Func<(bool Ok, string Value, string Error)>>();
            foreach (var (label, def, isDuration) in prompts)
            {
                layout.Controls.Add(new Label { Text = label, Dock = DockStyle.Top, AutoSize = true, Margin = new Padding(0, 6, 0, 2) });
                if (isDuration)
                {
                    getters.Add(AddDurationRow(layout, label, def));
                }
                else
                {
                    var tb = new TextBox { Dock = DockStyle.Top, Text = def ?? string.Empty, Width = 380 };
                    layout.Controls.Add(tb);
                    getters.Add(() => (true, tb.Text, null));
                }
            }

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 44,
                Padding = new Padding(6)
            };
            var ok = new Button { Text = "OK", Width = 80, Height = 30 };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 80, Height = 30 };
            buttons.Controls.AddRange(new Control[] { ok, cancel });

            // Validate on OK; keep the dialog open (don't set DialogResult) if any duration value can't be parsed.
            List<string> resolved = null;
            ok.Click += (_, _) =>
            {
                var values = new List<string>(getters.Count);
                foreach (var g in getters)
                {
                    var (rowOk, value, error) = g();
                    if (!rowOk)
                    {
                        MessageBox.Show(form, error, "Invalid value", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    values.Add(value);
                }
                resolved = values;
                form.DialogResult = DialogResult.OK;
            };

            form.Controls.Add(layout);
            form.Controls.Add(buttons);
            form.AcceptButton = ok;
            form.CancelButton = cancel;

            form.ApplyTheme();
            if (form.ShowDialog(owner) != DialogResult.OK) return null;

            return resolved;
        }

        /// <summary>Adds a number box + unit selector (pre-filled from the stored microseconds) and returns its resolver.</summary>
        private static Func<(bool Ok, string Value, string Error)> AddDurationRow(TableLayoutPanel layout, string label, string storedMicros)
        {
            var row = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 0, 0, 2)
            };
            var tb = new TextBox { Width = 120 };
            var cbo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 70, FormattingEnabled = true };
            // Populate Items directly (not DataSource): the combo isn't parented yet, so a DataSource binding wouldn't
            // have a BindingContext and SelectedIndex would throw.  Format renders each Unit by its Label.
            cbo.Items.AddRange(XEDurationUnits.Units);
            cbo.Format += (_, e) => { if (e.ListItem is XEDurationUnits.Unit u) e.Value = u.Label; };

            if (long.TryParse(storedMicros, NumberStyles.Integer, CultureInfo.InvariantCulture, out var micros) && micros >= 0)
            {
                var (value, unit) = XEDurationUnits.Decompose(micros);
                tb.Text = value.ToString("0", CultureInfo.CurrentCulture);
                cbo.SelectedIndex = XEDurationUnits.IndexOf(unit);
            }
            else
            {
                // Malformed stored value (not clean integer microseconds): keep it verbatim and treat it as µs so it
                // isn't silently rescaled - a fresh prompt with no stored value falls here too and starts at 0 µs.
                tb.Text = storedMicros ?? string.Empty;
                cbo.SelectedIndex = 0; // µs
            }

            row.Controls.Add(tb);
            row.Controls.Add(cbo);
            layout.Controls.Add(row);

            return () => XEDurationUnits.TryToMicroseconds(tb.Text, cbo.SelectedItem as XEDurationUnits.Unit, out var v, out var e)
                ? (true, v, null)
                : (false, null, $"{label} {e}");
        }
    }
}
