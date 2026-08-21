using DBADashGUI.Theme;
using System.Drawing;
using System.Windows.Forms;

namespace DBADashGUI.XETrace
{
    /// <summary>
    /// A small modal warning dialog with Continue/Cancel and an optional "Don't show this again" checkbox.
    /// Used for the ad-hoc trace pre-flight warnings (first-run cost warning, many-instances warning).
    /// </summary>
    internal static class XEWarningForm
    {
        /// <summary>Result of showing the warning.</summary>
        public readonly struct WarningResult
        {
            public WarningResult(bool @continue, bool suppress)
            {
                Continue = @continue;
                Suppress = suppress;
            }

            /// <summary>True if the user chose to continue; false if they cancelled.</summary>
            public bool Continue { get; }

            /// <summary>True if the user ticked "Don't show this again" (only meaningful when showSuppress was set).</summary>
            public bool Suppress { get; }
        }

        public static WarningResult Show(IWin32Window owner, string title, string message, bool showSuppress)
        {
            const int textWidth = 380;
            const int leftMargin = 64;
            const int topMargin = 18;

            using var form = new Form
            {
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false
            };

            // Size the label to the wrapped text so long, multi-paragraph messages aren't clipped.
            var textSize = TextRenderer.MeasureText(message, form.Font, new Size(textWidth, 0),
                TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);

            var icon = new PictureBox
            {
                Image = SystemIcons.Warning.ToBitmap(),
                SizeMode = PictureBoxSizeMode.AutoSize,
                Location = new Point(16, topMargin)
            };

            var label = new Label
            {
                AutoSize = false,
                Location = new Point(leftMargin, topMargin),
                Size = new Size(textWidth, textSize.Height),
                Text = message
            };

            var suppress = new CheckBox
            {
                Text = "Don't show this warning again",
                AutoSize = true,
                Location = new Point(leftMargin, topMargin + textSize.Height + 12),
                Visible = showSuppress
            };

            var contentBottom = topMargin + textSize.Height + (showSuppress ? 12 + suppress.PreferredSize.Height : 0);
            form.ClientSize = new Size(leftMargin + textWidth + 16, contentBottom + 16 + 44);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 44,
                Padding = new Padding(6)
            };
            var ok = new Button { Text = "Continue", DialogResult = DialogResult.OK, Width = 90, Height = 30 };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 90, Height = 30 };
            buttons.Controls.AddRange(new Control[] { ok, cancel });

            form.Controls.Add(label);
            form.Controls.Add(icon);
            form.Controls.Add(suppress);
            form.Controls.Add(buttons);
            form.AcceptButton = ok;
            form.CancelButton = cancel;
            form.ApplyTheme();

            var dr = form.ShowDialog(owner);
            return new WarningResult(dr == DialogResult.OK, showSuppress && suppress.Checked);
        }
    }
}
