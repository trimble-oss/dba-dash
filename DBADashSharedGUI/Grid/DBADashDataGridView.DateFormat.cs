using System;
using System.Globalization;
using System.Windows.Forms;
using DBADashSharedGUI;

namespace DBADashGUI.CustomReports
{
    /// <summary>
    /// How date and time cells are shown.  The grid's own general format leaves out the seconds, and sometimes the
    /// seconds and milliseconds are what matter - so by default they're shown, and the Date and Time Format menu picks
    /// another.  The choice is shared by every grid, and the host keeps it (see ViewerSettings.ConnectDateTimeFormat).
    /// A column with a Format of its own keeps it.
    /// </summary>
    public partial class DBADashDataGridView
    {
        /// <summary>The format date and time cells start with: seconds and milliseconds, as a sortable local time.</summary>
        public const string DefaultDateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";

        /// <summary>The formats on the Date and Time Format menu, with the label each is shown under.</summary>
        public static readonly (string Label, string Format)[] DateTimeFormats =
        [
            ("Milliseconds", DefaultDateTimeFormat),
            ("Seconds", "yyyy-MM-dd HH:mm:ss"),
            ("Minutes", "yyyy-MM-dd HH:mm"),
            ("Windows short", "g"),
            ("Windows long", "G")
        ];

        private static string _dateTimeFormat = DefaultDateTimeFormat;

        /// <summary>Raised when <see cref="DateTimeFormat"/> changes - every grid repaints, and the host can keep the choice.</summary>
        public static event EventHandler DateTimeFormatChanged;

        /// <summary>
        /// The .NET format date and time cells are shown with, unless their column has a Format of its own.  A blank
        /// value puts back <see cref="DefaultDateTimeFormat"/>.
        /// </summary>
        public static string DateTimeFormat
        {
            get => _dateTimeFormat;
            set
            {
                var format = string.IsNullOrWhiteSpace(value) ? DefaultDateTimeFormat : value.Trim();
                if (format == _dateTimeFormat) return;
                _dateTimeFormat = format;
                DateTimeFormatChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        /// <summary>Whether <paramref name="format"/> is a date and time format .NET accepts.</summary>
        public static bool IsValidDateTimeFormat(string format)
        {
            // Checked as it will be used: trimmed, and for both DateTime and DateTimeOffset cells.
            var normalized = format?.Trim() ?? string.Empty;
            if (normalized.Length == 0) return true;
            try
            {
                _ = DateTime.Now.ToString(normalized, CultureInfo.CurrentCulture);
                _ = DateTimeOffset.Now.ToString(normalized, CultureInfo.CurrentCulture);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        /// <summary>Repaints so cells show the format chosen - on any grid, not just the one it was chosen on.</summary>
        private void DateFormat_Changed(object sender, EventArgs e) => Invalidate();

        /// <summary>
        /// The Date and Time Format menu: the preset formats, each with an example, and a custom one.  Shared by the
        /// cell and column menus, so it's built for each.
        /// </summary>
        private ToolStripMenuItem GetDateTimeFormatMenuItem()
        {
            var menu = new ToolStripMenuItem("Date and Time Format");
            var presets = new ToolStripItem[DateTimeFormats.Length];
            for (var i = 0; i < DateTimeFormats.Length; i++)
            {
                var (label, format) = DateTimeFormats[i];
                var item = new ToolStripMenuItem($"{label}  ({DateTime.Now.ToString(format, CultureInfo.CurrentCulture)})");
                item.Click += (_, _) => DateTimeFormat = format;
                presets[i] = item;
            }
            var custom = new ToolStripMenuItem("Custom...", null, (_, _) => PromptDateTimeFormat());
            menu.DropDownItems.AddRange([.. presets, new ToolStripSeparator(), custom]);

            menu.DropDownOpening += (_, _) =>
            {
                for (var i = 0; i < DateTimeFormats.Length; i++)
                {
                    ((ToolStripMenuItem)presets[i]).Checked = DateTimeFormats[i].Format == DateTimeFormat;
                }
                custom.Checked = !Array.Exists(DateTimeFormats, f => f.Format == DateTimeFormat);
            };
            return menu;
        }

        private void PromptDateTimeFormat()
        {
            var format = DateTimeFormat;
            while (CommonShared.ShowInputDialog(ref format, "Date and Time Format",
                       description: "A .NET custom format, such as yyyy-MM-dd HH:mm:ss.fff or dd/MM/yyyy HH:mm:ss") == DialogResult.OK)
            {
                if (IsValidDateTimeFormat(format))
                {
                    DateTimeFormat = format;
                    return;
                }

                MessageBox.Show(this, $"'{format}' isn't a date and time format.", "Date and Time Format",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
