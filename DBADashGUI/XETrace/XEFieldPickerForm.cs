using DBADashGUI.Theme;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.XETrace
{
    /// <summary>
    /// A small modal check-list dialog used to pick a subset of names (an event's data columns, or the global
    /// actions/"global fields").  Returns the checked set, or null if the user cancelled.
    /// </summary>
    internal static class XEFieldPickerForm
    {
        public static HashSet<string> Pick(IWin32Window owner, string title, IEnumerable<string> available,
            IEnumerable<string> selected)
        {
            var sel = new HashSet<string>(selected ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            var items = (available ?? Enumerable.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();

            using var form = new Form
            {
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.SizableToolWindow,
                MinimizeBox = false,
                MaximizeBox = false,
                ClientSize = new System.Drawing.Size(340, 460),
                ShowInTaskbar = false
            };

            var list = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                CheckOnClick = true,
                IntegralHeight = false
            };
            foreach (var item in items) list.Items.Add(item, sel.Contains(item));

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 44,
                Padding = new Padding(6)
            };
            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 80, Height = 30 };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 80, Height = 30 };
            var all = new Button { Text = "All", Width = 60, Height = 30 };
            var none = new Button { Text = "None", Width = 60, Height = 30 };
            all.Click += (_, _) => SetAll(list, true);
            none.Click += (_, _) => SetAll(list, false);
            buttons.Controls.AddRange(new Control[] { ok, cancel, none, all });

            form.Controls.Add(list);
            form.Controls.Add(buttons);
            form.AcceptButton = ok;
            form.CancelButton = cancel;
            form.ApplyTheme();

            return form.ShowDialog(owner) == DialogResult.OK
                ? new HashSet<string>(list.CheckedItems.Cast<string>(), StringComparer.OrdinalIgnoreCase)
                : null;
        }

        private static void SetAll(CheckedListBox list, bool value)
        {
            for (var i = 0; i < list.Items.Count; i++) list.SetItemChecked(i, value);
        }
    }
}
