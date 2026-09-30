using DBADashGUI.Theme;
using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace DBADashGUI.CustomReports
{
    /// <summary>
    /// The full set of options for a column's data bar - style, colouring and scale - behind the quick
    /// choices on the grid's context menu.
    /// </summary>
    public sealed class DataBarConfig : Form
    {
        private readonly ComboBox cboStyle = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
        private readonly ComboBox cboColorMode = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
        private readonly Button bttnColor = new() { Width = 60, FlatStyle = FlatStyle.Flat };
        private readonly Button bttnEndColor = new() { Width = 60, FlatStyle = FlatStyle.Flat };
        private readonly Label lblColor = new() { Text = "Colour", AutoSize = true, Anchor = AnchorStyles.Left };
        private readonly Label lblEndColor = new() { Text = "High colour", AutoSize = true, Anchor = AnchorStyles.Left };
        private readonly CheckBox chkHigherIsBetter = new() { Text = "Higher is better", AutoSize = true };
        private readonly Label lblWarning = new() { Text = "Warning at (% of scale)", AutoSize = true, Anchor = AnchorStyles.Left };
        private readonly Label lblCritical = new() { Text = "Critical at (% of scale)", AutoSize = true, Anchor = AnchorStyles.Left };
        private readonly NumericUpDown numWarning = new() { Minimum = 0, Maximum = 100, Width = 80 };
        private readonly NumericUpDown numCritical = new() { Minimum = 0, Maximum = 100, Width = 80 };
        private readonly TextBox txtMinimum = new() { Width = 120, PlaceholderText = "Auto" };
        private readonly TextBox txtMaximum = new() { Width = 120, PlaceholderText = "Auto" };

        private static readonly (string Text, DataBarStyle Value)[] Styles =
        {
            ("Bar under value", DataBarStyle.Underline),
            ("Fill behind value", DataBarStyle.Fill)
        };

        private static readonly (string Text, DataBarColorMode Value)[] ColorModes =
        {
            ("Solid colour", DataBarColorMode.Solid),
            ("Traffic light", DataBarColorMode.TrafficLight),
            ("Gradient", DataBarColorMode.Gradient),
            ("Positive / negative", DataBarColorMode.PositiveNegative)
        };

        /// <summary>The settings chosen, or null when the user removed the bar.</summary>
        public DataBarSettings Settings { get; private set; }

        public DataBarConfig(string columnName, DataBarSettings settings)
        {
            // The fixed widths below are at 96 DPI - scale them with the screen, as the designer's forms are.
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = $"Data Bar: {columnName}";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(10);

            var layout = new TableLayoutPanel
            {
                ColumnCount = 2,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Location = new Point(Padding.Left, Padding.Top)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            AddRow(layout, FieldLabel("Style"), cboStyle);
            AddRow(layout, FieldLabel("Colouring"), cboColorMode);
            AddRow(layout, lblColor, bttnColor);
            AddRow(layout, lblEndColor, bttnEndColor);
            AddRow(layout, null, chkHigherIsBetter);
            AddRow(layout, lblWarning, numWarning);
            AddRow(layout, lblCritical, numCritical);
            AddRow(layout, FieldLabel("Minimum"), txtMinimum);
            AddRow(layout, FieldLabel("Maximum"), txtMaximum);
            AddRow(layout, null, new Label
            {
                Text = "Leave minimum and maximum blank to scale\nthe bars to the values in the grid.",
                AutoSize = true
            });

            var bttnOK = new Button { Text = "OK", DialogResult = DialogResult.None, AutoSize = true };
            var bttnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
            var bttnRemove = new Button { Text = "Remove", AutoSize = true };
            bttnOK.Click += (_, _) => Save();
            bttnRemove.Click += (_, _) =>
            {
                Settings = null;
                DialogResult = DialogResult.OK;
            };
            var buttons = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0, 10, 0, 0)
            };
            buttons.Controls.AddRange(new Control[] { bttnCancel, bttnOK, bttnRemove });
            var buttonRow = layout.RowCount++;
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(buttons, 0, buttonRow);
            layout.SetColumnSpan(buttons, 2);
            Controls.Add(layout);
            AcceptButton = bttnOK;
            CancelButton = bttnCancel;

            foreach (var s in Styles) cboStyle.Items.Add(s.Text);
            foreach (var m in ColorModes) cboColorMode.Items.Add(m.Text);

            settings ??= new DataBarSettings();
            cboStyle.SelectedIndex = Array.FindIndex(Styles, s => s.Value == settings.Style);
            cboColorMode.SelectedIndex = Array.FindIndex(ColorModes, m => m.Value == settings.ColorMode);
            bttnColor.BackColor = settings.Color;
            bttnEndColor.BackColor = settings.GradientEndColor;
            chkHigherIsBetter.Checked = settings.HigherIsBetter;
            numWarning.Value = Math.Clamp(settings.WarningThreshold, 0, 100);
            numCritical.Value = Math.Clamp(settings.CriticalThreshold, 0, 100);
            txtMinimum.Text = settings.Minimum?.ToString(CultureInfo.CurrentCulture) ?? string.Empty;
            txtMaximum.Text = settings.Maximum?.ToString(CultureInfo.CurrentCulture) ?? string.Empty;

            bttnColor.Click += (_, _) => PickColor(bttnColor);
            bttnEndColor.Click += (_, _) => PickColor(bttnEndColor);
            cboColorMode.SelectedIndexChanged += (_, _) => ShowColorModeOptions();
            ShowColorModeOptions();

            this.ApplyTheme();
            // The swatches show the colour, whatever the theme did to the buttons.
            bttnColor.BackColor = settings.Color;
            bttnEndColor.BackColor = settings.GradientEndColor;
        }

        /// <summary>
        /// Add a row at a fixed position.  Left to flow, a table skips its hidden controls and the ones after them
        /// shift back into the empty cells - so hiding the options for another colouring would jumble the rest.
        /// </summary>
        private static void AddRow(TableLayoutPanel layout, Control label, Control input)
        {
            var row = layout.RowCount++;
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            if (label != null) layout.Controls.Add(label, 0, row);
            layout.Controls.Add(input, 1, row);
        }

        private static Label FieldLabel(string text) => new() { Text = text, AutoSize = true, Anchor = AnchorStyles.Left };

        private DataBarColorMode SelectedColorMode => ColorModes[cboColorMode.SelectedIndex].Value;

        private void ShowColorModeOptions()
        {
            var mode = SelectedColorMode;
            lblColor.Visible = bttnColor.Visible = mode is DataBarColorMode.Solid or DataBarColorMode.Gradient;
            lblColor.Text = mode == DataBarColorMode.Gradient ? "Low colour" : "Colour";
            lblEndColor.Visible = bttnEndColor.Visible = mode == DataBarColorMode.Gradient;
            lblWarning.Visible = numWarning.Visible =
                lblCritical.Visible = numCritical.Visible = mode == DataBarColorMode.TrafficLight;
            chkHigherIsBetter.Visible = mode is DataBarColorMode.TrafficLight or DataBarColorMode.PositiveNegative;
            chkHigherIsBetter.Text = mode == DataBarColorMode.PositiveNegative ? "Positive is good" : "Higher is better";
        }

        private static void PickColor(Button swatch)
        {
            using var dialog = new ColorDialog { Color = swatch.BackColor, FullOpen = true };
            if (dialog.ShowDialog() == DialogResult.OK) swatch.BackColor = dialog.Color;
        }

        private static bool TryParseLimit(TextBox txt, out decimal? value)
        {
            value = null;
            if (string.IsNullOrWhiteSpace(txt.Text)) return true;
            if (!decimal.TryParse(txt.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed)) return false;
            value = parsed;
            return true;
        }

        private void Save()
        {
            if (!TryParseLimit(txtMinimum, out var min) || !TryParseLimit(txtMaximum, out var max))
            {
                MessageBox.Show("Minimum and maximum must be numbers, or blank to scale to the data.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (min.HasValue && max.HasValue && min >= max)
            {
                MessageBox.Show("Minimum must be less than maximum.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (SelectedColorMode == DataBarColorMode.TrafficLight && numWarning.Value > numCritical.Value)
            {
                MessageBox.Show("The warning threshold must not be above the critical threshold.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Settings = new DataBarSettings
            {
                Style = Styles[cboStyle.SelectedIndex].Value,
                ColorMode = SelectedColorMode,
                Color = bttnColor.BackColor,
                GradientEndColor = bttnEndColor.BackColor,
                HigherIsBetter = chkHigherIsBetter.Checked,
                WarningThreshold = numWarning.Value,
                CriticalThreshold = numCritical.Value,
                Minimum = min,
                Maximum = max
            };
            DialogResult = DialogResult.OK;
        }
    }
}
