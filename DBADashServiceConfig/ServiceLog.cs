using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using DBADash;
using DBADashGUI.CustomReports;
using DBADashGUI.Theme;
using Newtonsoft.Json;

namespace DBADashServiceConfig
{
    public partial class ServiceLog : Form
    {
        private readonly string logsFolder = Path.Combine(Application.StartupPath, "Logs");

        /// <summary>A JSON log, as a grid - see <see cref="JsonRecords"/>.  Shown in place of the text box.</summary>
        private readonly DBADashDataGridView gridLog = new()
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            Visible = false
        };

        /// <summary>What couldn't be read from a JSON log - often the last line of one still being written.</summary>
        private readonly WarningLabel lblWarning = new() { Dock = DockStyle.Top, Visible = false };

        /// <summary>Below a JSON log: how many rows it shows, and how to show the columns that are hidden.</summary>
        private readonly Label lblStatus = new()
        {
            Dock = DockStyle.Bottom,
            AutoSize = false,
            AutoEllipsis = true,
            Padding = new Padding(6, 0, 6, 0),
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
            Visible = false
        };

        public ServiceLog()
        {
            InitializeComponent();
            lblStatus.Height = TextRenderer.MeasureText("X", lblStatus.Font).Height + 8;
            gridLog.DataBindingComplete += GridLog_DataBindingComplete;
            gridLog.ColumnStateChanged += GridLog_ColumnStateChanged;
            gridLog.ColumnWidthChanged += GridLog_ColumnWidthChanged;
            gridLog.GridFilterChanged += (_, _) => rowFilter = gridLog.RowFilter;
            // Docked in reverse order: the panel at the top, the warning below it, the status at the bottom, then the log
            // filling the rest.
            Controls.Add(gridLog);
            Controls.Add(lblWarning);
            Controls.Add(lblStatus);
            txtLog.BringToFront();
            gridLog.BringToFront();
            panel1.SendToBack();
            this.ApplyTheme();
        }

        private void ServiceLog_Load(object sender, EventArgs e)
        {
            LoadFormat();
            RefreshLogs();
            Activated += ServiceLog_Activated; // Scroll to end of log when form is activated (doesn't work in form load event)
        }

        private void ServiceLog_Activated(object sender, EventArgs e)
        {
            // Scroll to end of log when form is activated (doesn't work in form load event)
            Activated -= ServiceLog_Activated;
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.ScrollToCaret();
        }

        private void RefreshLogs()
        {
            if (Directory.Exists(logsFolder))
            {
                cboLogs.Items.Clear();
                var directory = new DirectoryInfo(logsFolder);
                foreach (var f in directory.EnumerateFiles()
                             .Where(f => LogExtensions.Contains(f.Extension, StringComparer.OrdinalIgnoreCase))
                             .Select(f => (File: f, Period: ServiceLogPeriod(f.Name)))
                             .OrderByDescending(f => f.Period != null)
                             .ThenByDescending(f => f.Period, StringComparer.Ordinal)
                             .ThenByDescending(f => f.File.LastWriteTime))
                {
                    cboLogs.Items.Add(f.File.Name);
                }
                if (cboLogs.Items.Count > 0)
                {
                    cboLogs.SelectedIndex = 0;
                }
                else
                {
                    ShowText("Log file not found. This is created when the service is started.");
                }
            }
            else
            {
                ShowText($"Log folder does not exist '{logsFolder}'. This is created when the service is started.");
            }
        }

        /// <summary>Text logs, and JSON ones - Serilog's compact format, which is .json by default, or .clef.</summary>
        private static readonly string[] LogExtensions = [".txt", ".json", ".clef"];

        /// <summary>
        /// A rolling service log is named for the period it covers - log-yyyyMMddHH.json by default (.txt for a text log),
        /// with _001 etc. added when the file was locked - so the name orders the logs.  Last write time doesn't: while the
        /// service holds the current log open, the time Windows lists for it lags behind, and an older file can look newer.
        /// The whole name is matched, from the "log-" prefix in serilog.json, so another file that happens to end in a
        /// date (e.g. a copy of a GUI log) isn't taken for the latest service log.
        /// </summary>
        /// <returns>The period and sequence as a string that sorts in date order, or null for any other file, e.g. FatalError.txt</returns>
        private static string ServiceLogPeriod(string fileName)
        {
            var match = ServiceLogNameRegex.Match(fileName);
            if (!match.Success) return null;
            // Pad so logs from a different rolling interval (e.g. daily - yyyyMMdd) sort alongside hourly ones
            return match.Groups["period"].Value.PadRight(12, '0') + match.Groups["seq"].Value.PadLeft(6, '0');
        }

        private static readonly Regex ServiceLogNameRegex = new(@"^log-(?<period>\d{4,12})(?:_(?<seq>\d+))?\.(?:txt|json|clef)$", RegexOptions.IgnoreCase);

        private void LoadLog(string fileName)
        {
            var filter = txtLogFilter.Text.Trim();
            var filePath = Path.Combine(logsFolder, fileName);
            if (!File.Exists(filePath))
            {
                ShowText($"File not found '{filePath}'");
                return;
            }

            try
            {
                if (IsJson(filePath))
                {
                    LoadJsonLog(filePath, filter);
                }
                else
                {
                    LoadTextLog(filePath, filter);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                ShowText($"Error reading '{filePath}'{Environment.NewLine}{ex.Message}");
            }
        }

        /// <summary>The service still has its current log open, so it's read with shared read/write.</summary>
        private static StreamReader OpenLog(string filePath) =>
            new(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete));

        /// <summary>
        /// A JSON log starts with a record - "{".  Decided from what's in the file rather than its name, so a log written
        /// by a configuration of the user's own is shown as it is, whatever it's called.
        /// </summary>
        private static bool IsJson(string filePath)
        {
            using var reader = OpenLog(filePath);
            int c;
            while ((c = reader.Read()) >= 0)
            {
                if (!char.IsWhiteSpace((char)c)) return c == '{';
            }

            return false;
        }

        private void LoadTextLog(string filePath, string filter)
        {
            var sb = new StringBuilder();
            using (var sr = OpenLog(filePath))
            {
                while (sr.ReadLine() is { } line)
                {
                    if (filter == string.Empty || line.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    {
                        sb.AppendLine(line);
                    }
                }
            }

            ShowText(sb.ToString());
        }

        private void LoadJsonLog(string filePath, string filter)
        {
            var ds = JsonRecords.Load(() => OpenLog(filePath));
            var table = ds.Tables.Count > 0 ? ds.Tables[0] : new DataTable();
            if (filter != string.Empty) table = Filter(table, filter);
            ToLocalTime(table);
            OrderLogColumns(table);

            // A DataView rather than the table, as that's what the grid's own filtering works through.  With the filter
            // the grid had, so a refresh or another log file keeps it.
            var view = new DataView(table);
            var filterWarning = ApplyRowFilter(view);

            lblWarning.Text = string.Join("  ", new[] { table.ExtendedProperties[JsonRecords.LoadWarningProperty] as string, filterWarning }
                .Where(w => !string.IsNullOrEmpty(w)));
            lblWarning.Visible = !string.IsNullOrEmpty(lblWarning.Text);
            txtLog.Visible = false;
            gridLog.Visible = true;
            // The new log's columns are added visible and at the default width - not the user's choice
            configuringColumns = true;
            try
            {
                gridLog.DataSource = null;
                gridLog.Columns.Clear();
                columnsToConfigure = true;
                gridLog.DataSource = view;
            }
            finally
            {
                configuringColumns = false;
            }
        }

        /// <summary>The grid's own filter - its Edit Filter, or a filter by value - as it was last set.</summary>
        private string rowFilter = string.Empty;

        /// <summary>
        /// Filters <paramref name="view"/> as the grid was filtered.  A filter on a column the log doesn't have - one only
        /// another log file's messages use - can't apply: it's cleared, and the warning returned says so.
        /// </summary>
        private string ApplyRowFilter(DataView view)
        {
            if (string.IsNullOrEmpty(rowFilter)) return null;
            try
            {
                view.RowFilter = rowFilter;
                return null;
            }
            catch (InvalidExpressionException ex)
            {
                var filter = rowFilter;
                rowFilter = string.Empty;
                return $"The grid filter {filter} doesn't apply to this log, so it was cleared: {ex.Message}";
            }
        }

        /// <summary>
        /// Set when a log is loaded, so its columns are set up once - not again each time sorting or filtering rebinds
        /// the grid, which would undo what the user changed and jump back to the end.
        /// </summary>
        private bool columnsToConfigure;

        /// <summary>
        /// The rows with <paramref name="filter"/> in any column - as the text log keeps the lines it's in.  The grid's own
        /// filters can narrow it down from there.
        /// </summary>
        private static DataTable Filter(DataTable table, string filter)
        {
            var filtered = table.Clone();
            foreach (var key in table.ExtendedProperties.Keys)
            {
                filtered.ExtendedProperties[key] = table.ExtendedProperties[key];
            }

            foreach (DataRow row in table.Rows)
            {
                if (row.ItemArray.Any(value => value is not DBNull && value?.ToString()?.Contains(filter, StringComparison.OrdinalIgnoreCase) == true))
                {
                    filtered.ImportRow(row);
                }
            }

            return filtered;
        }

        /// <summary>
        /// Serilog writes a compact log's timestamps in UTC.  The service runs on this machine, and its text log is in
        /// local time, so the JSON one is shown in local time too.
        /// </summary>
        private static void ToLocalTime(DataTable table)
        {
            if (table.Columns["Timestamp"] is not { } column || column.DataType != typeof(DateTime)) return;

            foreach (DataRow row in table.Rows)
            {
                if (row[column] is DateTime { Kind: DateTimeKind.Utc } utc) row[column] = utc.ToLocalTime();
            }

            table.AcceptChanges();
        }

        /// <summary>
        /// The columns the user has shown or hidden, and the widths they've dragged them to, by name - applied to each log
        /// loaded, so a refresh or another log file keeps them.  For as long as the app runs, so reopening the dialog does too.
        /// </summary>
        private static readonly Dictionary<string, bool> chosenVisibility = new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, int> chosenWidths = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>While the grid's columns are being set up, so the changes made here aren't taken for the user's.</summary>
        private bool configuringColumns;

        private void GridLog_ColumnStateChanged(object sender, DataGridViewColumnStateChangedEventArgs e)
        {
            if (!configuringColumns && e.StateChanged == DataGridViewElementStates.Visible)
            {
                chosenVisibility[e.Column.Name] = e.Column.Visible;
                ShowStatus();
            }
        }

        /// <summary>The rows shown - of how many, when the grid's filter leaves some out - and the columns hidden.</summary>
        private void ShowStatus()
        {
            if (gridLog.DataSource is not DataView view)
            {
                lblStatus.Visible = false;
                return;
            }

            var total = view.Table?.Rows.Count ?? 0;
            var shown = gridLog.Rows.Count;
            var rows = shown == total ? $"{shown:N0} {(shown == 1 ? "row" : "rows")}" : $"{shown:N0} of {total:N0} rows";
            var hidden = gridLog.Columns.Cast<DataGridViewColumn>().Count(c => !c.Visible);
            lblStatus.Text = hidden == 0
                ? rows
                : $"{rows}.  {hidden:N0} {(hidden == 1 ? "column" : "columns")} hidden - right-click a column header, Columns, to show them.";
            lblStatus.Visible = true;
        }

        private void GridLog_ColumnWidthChanged(object sender, DataGridViewColumnEventArgs e)
        {
            if (!configuringColumns) chosenWidths[e.Column.Name] = e.Column.Width;
        }

        private void GridLog_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            // Each time, as filtering rebinds the grid too
            if (!columnsToConfigure)
            {
                ShowStatus();
                return;
            }
            columnsToConfigure = false;
            configuringColumns = true;
            try
            {
                ConfigureColumns();
            }
            finally
            {
                configuringColumns = false;
            }

            ShowStatus();

            // The latest events are at the end, as the text log scrolls to them.
            if (gridLog.Rows.Count > 0)
            {
                gridLog.FirstDisplayedScrollingRowIndex = gridLog.Rows.Count - 1;
            }
        }

        /// <summary>
        /// The columns of a log shown, in this order: when, how severe, which connection - DBA Dash adds the ConnectionID
        /// to everything logged while it works on one - what, how long it took and on which thread, then the exception,
        /// and any lines that couldn't be read.  The event's other properties are mostly the values its message shows,
        /// or the same on every event; they're hidden, to filter or group on, or to show again.
        /// </summary>
        private static readonly string[] ShownLogColumns =
            ["Timestamp", "Level", "ConnectionID", "Message", "Elapsed", "ThreadId", "Exception", JsonRecords.UnreadableColumnName];

        /// <summary>
        /// Puts a log's <see cref="ShownLogColumns"/> first, in their order - in the table rather than the grid, so the
        /// grid's columns are made in that order.  The rest follow in the log's own order.
        /// </summary>
        private static void OrderLogColumns(DataTable table)
        {
            if (table.TableName != JsonRecords.LogTableName) return;
            var ordinal = 0;
            foreach (var name in ShownLogColumns)
            {
                if (table.Columns[name] is { } column) column.SetOrdinal(ordinal++);
            }
        }

        private void ConfigureColumns()
        {
            if (gridLog.Columns["Timestamp"] is { } timestamp && timestamp.ValueType == typeof(DateTime))
            {
                timestamp.DefaultCellStyle.Format = "yyyy-MM-dd HH:mm:ss.fff";
            }

            // A log shows the columns that matter in DBA Dash's logs, in their order, and hides the event's other
            // properties - unless the user has shown them, which the Columns menu does.  Other JSON shows every column.
            var isLog = (gridLog.DataSource as DataView)?.Table?.TableName == JsonRecords.LogTableName;
            foreach (DataGridViewColumn column in gridLog.Columns)
            {
                column.Visible = chosenVisibility.TryGetValue(column.Name, out var visible)
                    ? visible
                    : !isLog || ShownLogColumns.Contains(column.Name, StringComparer.OrdinalIgnoreCase);
            }

            gridLog.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            foreach (DataGridViewColumn column in gridLog.Columns)
            {
                column.Width = chosenWidths.TryGetValue(column.Name, out var width) ? width : Math.Min(column.Width, 600);
            }

            // The message takes the width the other columns leave - with the properties hidden, there's room for it.  Set
            // once rather than as a Fill column: a Fill column can't be widened by dragging, nor can a column after it.
            if (gridLog.Columns["Message"] is { Visible: true } message && !chosenWidths.ContainsKey(message.Name))
            {
                var others = gridLog.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible && c != message).Sum(c => c.Width);
                message.Width = Math.Max(message.Width, gridLog.ClientSize.Width - others - SystemInformation.VerticalScrollBarWidth);
            }
        }

        private void ShowText(string text)
        {
            gridLog.Visible = false;
            gridLog.DataSource = null;
            lblWarning.Visible = false;
            lblStatus.Visible = false;
            txtLog.Visible = true;
            txtLog.Text = text;
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.ScrollToCaret();
        }

        /// <summary>The format the service writes its log in - see <see cref="ServiceLogFormat"/>.  None until it's read.</summary>
        private ServiceLogFileFormat currentFormat;

        private static ServiceLogFileFormat Other(ServiceLogFileFormat format) =>
            format == ServiceLogFileFormat.Json ? ServiceLogFileFormat.Text : ServiceLogFileFormat.Json;

        private static string Describe(ServiceLogFileFormat format) => format == ServiceLogFileFormat.Json ? "JSON" : "Text";

        /// <summary>
        /// A link to switch the service to the other format - an action on the service, and confirmed before it's done, so
        /// it isn't taken for a choice of how this log is shown.  Hidden when the configuration can't be read, or has no
        /// log file to switch.
        /// </summary>
        private void LoadFormat()
        {
            try
            {
                currentFormat = ServiceLogFormat.GetFormat(Application.StartupPath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                currentFormat = ServiceLogFileFormat.None;
            }

            lnkSwitchFormat.Visible = currentFormat != ServiceLogFileFormat.None;
            if (!lnkSwitchFormat.Visible) return;

            lnkSwitchFormat.Text = $"Switch service log format to {Describe(Other(currentFormat))}";
            toolTip1.SetToolTip(lnkSwitchFormat,
                $"The service writes its log as {Describe(currentFormat)}.  JSON logs are shown as a grid that can be sorted and filtered.");
        }

        private void LnkSwitchFormat_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var format = Other(currentFormat);
            var confirm = MessageBox.Show(
                $"The service writes its log as {Describe(currentFormat)}.  Switch it to {Describe(format)}?\n\n" +
                "This changes the service's logging configuration, and takes effect when the service is restarted.  Logs " +
                "already written stay as they are.  The choice is kept through upgrades.",
                "Switch Service Log Format", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (confirm != DialogResult.Yes) return;

            try
            {
                ServiceLogFormat.SetFormat(Application.StartupPath, format);
                MessageBox.Show($"The service will write its log as {Describe(format)} once it's restarted.",
                    "Switch Service Log Format", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException)
            {
                MessageBox.Show($"Unable to change the log format: {ex.Message}", "Switch Service Log Format",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            LoadFormat();
        }

        private void BttnRefreshLog_Click(object sender, EventArgs e)
        {
            RefreshLogs();
        }

        private void CboLogs_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadLog(cboLogs.Text);
        }

        private void TxtLogFilter_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                LoadLog(cboLogs.Text);
            }
        }

        private void BttnNotepad_Click(object sender, EventArgs e)
        {
            string filePath = Path.Combine(logsFolder, cboLogs.Text);
            System.Diagnostics.Process.Start("Notepad.exe", filePath);
        }

        /// <summary>A one-line warning across the top of the log, in the theme's warning colours.</summary>
        private sealed class WarningLabel : Label, IThemedControl
        {
            public WarningLabel()
            {
                AutoSize = false;
                AutoEllipsis = true;
                Padding = new Padding(6, 0, 6, 0);
                Height = TextRenderer.MeasureText("X", Font).Height + 8;
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            }

            public void ApplyTheme(BaseTheme theme)
            {
                BackColor = theme.WarningLowBackColor;
                ForeColor = theme.WarningLowForeColor;
            }
        }
    }
}
