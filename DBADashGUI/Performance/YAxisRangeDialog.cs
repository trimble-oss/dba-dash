using DBADashGUI.Theme;
using System;
using System.Globalization;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    /// <summary>
    /// Small dialog used to set a fixed minimum/maximum for a chart's Y-axis.
    /// Leaving a field blank keeps that bound on auto-scaling.
    /// </summary>
    public class YAxisRangeDialog : Form
    {
        private readonly TextBox txtMin;
        private readonly TextBox txtMax;

        public double? AxisMin { get; private set; }
        public double? AxisMax { get; private set; }

        public YAxisRangeDialog(double? min, double? max)
        {
            AxisMin = min;
            AxisMax = max;

            Text = "Y-Axis Range";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(300, 190);

            var lblMin = new Label { Text = "Minimum:", Left = 12, Top = 18, Width = 80, TextAlign = System.Drawing.ContentAlignment.MiddleLeft };
            var lblMax = new Label { Text = "Maximum:", Left = 12, Top = 52, Width = 80, TextAlign = System.Drawing.ContentAlignment.MiddleLeft };

            txtMin = new TextBox { Left = 100, Top = 15, Width = 180, Text = min?.ToString(CultureInfo.CurrentCulture) ?? string.Empty };
            txtMax = new TextBox { Left = 100, Top = 49, Width = 180, Text = max?.ToString(CultureInfo.CurrentCulture) ?? string.Empty };

            var lblHint = new Label { Text = "Leave a field blank for auto-scaling.", Left = 12, Top = 86, Width = 268, ForeColor = System.Drawing.SystemColors.GrayText };

            var bttnOK = new Button { Text = "OK", Left = 112, Top = 140, Width = 80, Height = 30, DialogResult = DialogResult.OK };
            var bttnCancel = new Button { Text = "Cancel", Left = 200, Top = 140, Width = 80, Height = 30, DialogResult = DialogResult.Cancel };
            bttnOK.Click += BttnOK_Click;

            Controls.AddRange(new Control[] { lblMin, lblMax, txtMin, txtMax, lblHint, bttnOK, bttnCancel });
            AcceptButton = bttnOK;
            CancelButton = bttnCancel;

            this.ApplyTheme();
        }

        private void BttnOK_Click(object sender, EventArgs e)
        {
            if (!TryParse(txtMin.Text, out var min))
            {
                MessageBox.Show("Minimum must be a number or left blank.", "Invalid Value", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }
            if (!TryParse(txtMax.Text, out var max))
            {
                MessageBox.Show("Maximum must be a number or left blank.", "Invalid Value", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }
            if (min.HasValue && max.HasValue && min.Value >= max.Value)
            {
                MessageBox.Show("Minimum must be less than maximum.", "Invalid Range", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }
            AxisMin = min;
            AxisMax = max;
        }

        private static bool TryParse(string text, out double? value)
        {
            value = null;
            if (string.IsNullOrWhiteSpace(text)) return true;
            if (double.TryParse(text.Trim(), NumberStyles.Any, CultureInfo.CurrentCulture, out var d))
            {
                value = d;
                return true;
            }
            return false;
        }
    }
}
