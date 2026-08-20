using DBADash.XE;
using DBADashGUI.Theme;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.XETrace
{
    /// <summary>
    /// Per-event fields dialog.  Shows the event's data columns read-only (they're always captured - XE has no way to
    /// deselect them) and lets the user toggle the event's customizable columns (the <c>collect_*</c> switches), which
    /// are the only per-event options that actually change what's captured.  Mirrors SSMS's Event Fields tab.
    /// Returns the toggle name -&gt; on/off map, or null if cancelled.
    /// </summary>
    internal static class XEEventFieldsForm
    {
        public static Dictionary<string, bool> Show(IWin32Window owner, string eventName, IEnumerable<string> dataColumns,
            IReadOnlyList<XECustomizableFieldInfo> customizations, IDictionary<string, bool> current)
        {
            var cols = (dataColumns ?? Enumerable.Empty<string>()).OrderBy(c => c, StringComparer.OrdinalIgnoreCase).ToList();
            customizations ??= new List<XECustomizableFieldInfo>();

            using var form = new Form
            {
                Text = $"Event fields - {eventName}",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.SizableToolWindow,
                MinimizeBox = false,
                MaximizeBox = false,
                ClientSize = new Size(420, 520),
                ShowInTaskbar = false
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(8)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 55));

            layout.Controls.Add(new Label
            {
                Text = $"Always captured - data columns ({cols.Count}):",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);

            var dataList = new ListBox { Dock = DockStyle.Fill, SelectionMode = SelectionMode.None };
            dataList.Items.AddRange(cols.Cast<object>().ToArray());
            layout.Controls.Add(dataList, 0, 1);

            layout.Controls.Add(new Label
            {
                Text = customizations.Count > 0
                    ? "Optional - customizable columns:"
                    : "No customizable columns for this event.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 2);

            var custList = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true, IntegralHeight = false };
            foreach (var c in customizations)
            {
                var on = current != null && current.TryGetValue(c.Name, out var v) ? v : c.DefaultOn;
                custList.Items.Add(c.Name, on);
            }
            layout.Controls.Add(custList, 0, 3);

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

            // Tooltip the description of each customizable column.
            if (customizations.Any(c => !string.IsNullOrEmpty(c.Description)))
            {
                var tip = new ToolTip();
                custList.MouseMove += (_, e) =>
                {
                    var i = custList.IndexFromPoint(e.Location);
                    var text = i >= 0 && i < customizations.Count ? customizations[i].Description : null;
                    if (tip.GetToolTip(custList) != (text ?? string.Empty)) tip.SetToolTip(custList, text ?? string.Empty);
                };
            }

            form.ApplyTheme();
            if (form.ShowDialog(owner) != DialogResult.OK) return null;

            var result = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < customizations.Count; i++)
            {
                result[customizations[i].Name] = custList.GetItemChecked(i);
            }
            return result;
        }
    }
}
