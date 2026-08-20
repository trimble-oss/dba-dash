using DBADashGUI.Theme;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.XETrace
{
    /// <summary>
    /// Load-time prompt dialog.  When a template has filters flagged to prompt, this asks for each value up front (in one
    /// dialog), pre-filled with the stored value as the default.  Returns the entered values in order, or null if
    /// cancelled (so the template load is abandoned and the current configuration is left untouched).
    /// </summary>
    internal static class XETemplatePromptForm
    {
        public static List<string> Prompt(IWin32Window owner, string templateName, IReadOnlyList<(string Label, string Default)> prompts)
        {
            if (prompts == null || prompts.Count == 0) return new List<string>();

            using var form = new Form
            {
                Text = string.IsNullOrEmpty(templateName) ? "Trace template" : $"Load template - {templateName}",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.SizableToolWindow,
                MinimizeBox = false,
                MaximizeBox = false,
                ClientSize = new Size(420, System.Math.Min(120 + prompts.Count * 56, 520)),
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

            var boxes = new List<TextBox>();
            foreach (var (label, def) in prompts)
            {
                layout.Controls.Add(new Label { Text = label, Dock = DockStyle.Top, AutoSize = true, Margin = new Padding(0, 6, 0, 2) });
                var tb = new TextBox { Dock = DockStyle.Top, Text = def ?? string.Empty, Width = 380 };
                boxes.Add(tb);
                layout.Controls.Add(tb);
            }

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 44,
                Padding = new Padding(6)
            };
            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 80, Height = 30 };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 80, Height = 30 };
            buttons.Controls.AddRange(new Control[] { ok, cancel });

            form.Controls.Add(layout);
            form.Controls.Add(buttons);
            form.AcceptButton = ok;
            form.CancelButton = cancel;

            form.ApplyTheme();
            if (boxes.Count > 0) form.Shown += (_, _) => boxes[0].Focus();
            if (form.ShowDialog(owner) != DialogResult.OK) return null;

            return boxes.Select(b => b.Text).ToList();
        }
    }
}
