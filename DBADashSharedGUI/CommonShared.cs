using DBADashGUI.CustomReports;
using System.Text;
using ClosedXML.Excel;
using DBADash;
using DBADashGUI;
using DBADashGUI.SchemaCompare;
using DBADashGUI.Theme;
using Microsoft.SqlServer.Management.Common;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Principal;
using System.Xml;

namespace DBADashSharedGUI
{
    public class CommonShared
    {
        public static readonly string TempFilePrefix = "DBADashGUITemp_";

        public static void OpenURL(string url)
        {
            if (!IsValidUrl(url))
            {
                throw new InvalidArgumentException("Invalid URL: " + url);
            }

            var psi = new ProcessStartInfo(url) { UseShellExecute = true };
            Process.Start(psi);
        }

        public static void OpenFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException($"The folder '{path}' does not exist.");
            }

            // Use Path.GetFullPath to get the absolute path, which also checks for invalid characters
            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(path);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException || ex is SecurityException)
            {
                throw new ArgumentException("The folder path is invalid.", nameof(path), ex);
            }

            // Start Windows Explorer at the given path
            try
            {
                Process.Start("explorer.exe", fullPath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to open the folder '{fullPath}' in Windows Explorer.", ex);
            }
        }

        public static bool IsValidUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;

            var result = Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
                         && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            return result;
        }

        [SupportedOSPlatform("windows")]
        public static void ShowAbout(IWin32Window owner, bool StartGUIOnUpgrade, bool includePreRelease = false)
        {
            using About frm = new()
            {
                DBVersion = new Version(),
                StartGUIOnUpgrade = StartGUIOnUpgrade,
                IncludePreRelease = includePreRelease
            };
            frm.ShowDialog(owner);
        }

        [SupportedOSPlatform("windows")]
        public static void ShowAbout(string connectionString, IWin32Window owner, bool StartGUIOnUpgrade, bool includePreRelease = false)
        {
            Version dbVersion = new();
            if (!string.IsNullOrEmpty(connectionString))
            {
                try
                {
                    dbVersion = DBValidations.GetDBVersion(connectionString).Version;
                }
                catch (Exception ex)
                {
                    ShowExceptionDialog(ex, @"Error getting repository version");
                }
            }
            using About frm = new()
            {
                DBVersion = dbVersion,
                StartGUIOnUpgrade = StartGUIOnUpgrade,
                IncludePreRelease = includePreRelease
            };
            frm.ShowDialog(owner);
        }

        [SupportedOSPlatform("windows")]
        public static void StyleGrid(ref DataGridView dgv)
        {
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                if (col.GetType() == typeof(DataGridViewLinkColumn))
                {
                    var linkCol = (DataGridViewLinkColumn)col;
                    linkCol.LinkColor = DashColors.LinkColor;
                }
            }
        }

        [SupportedOSPlatform("windows")]
        public static async Task CheckForIncompleteUpgrade()
        {
            if (!Upgrade.IsUpgradeIncomplete) return;

            MessageBox.Show(Upgrade.IncompleteUpgradeMessage, "Error", MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            if (MessageBox.Show("Retry upgrade?", "Retry", MessageBoxButtons.YesNo, MessageBoxIcon.Question) ==
                DialogResult.Yes)
            {
                await Upgrade.UpgradeDBADashAsync();
            }

            Application.Exit();
        }

        [SupportedOSPlatform("windows")]
        public static DialogResult ShowInputDialog(ref string input, string title, char passwordChar = '\0', string description = null)
        {
            var inputBox = new Form
            {
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                Text = title
            };

            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3, // One for the description, one for the text box, one for the button row
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(10)
            };
            inputBox.Controls.Add(panel);

            // If a description is provided, add a label for it
            if (!string.IsNullOrEmpty(description))
            {
                var descriptionLabel = new Label
                {
                    AutoSize = true,
                    Text = description,
                    Margin = new Padding(5),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                panel.Controls.Add(descriptionLabel, 0, 0);
            }
            else
            {
                // Reduce the row count if there is no description
                panel.RowCount = 2;
            }

            var textBox = new TextBox
            {
                Dock = DockStyle.Top,
                Text = input,
                MinimumSize = new Size(400, 30),
                PasswordChar = passwordChar
            };
            panel.Controls.Add(textBox, 0, 1);

            // Panel for buttons
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(5),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            panel.Controls.Add(buttonPanel, 0, 2);

            var okButton = new Button
            {
                DialogResult = DialogResult.OK,
                Text = "&OK",
                Width = 75,
                Height = 30,
                Margin = new Padding(5)
            };
            buttonPanel.Controls.Add(okButton);

            var cancelButton = new Button
            {
                DialogResult = DialogResult.Cancel,
                Text = "&Cancel",
                Width = 75,
                Height = 30,
                Margin = new Padding(5)
            };
            buttonPanel.Controls.Add(cancelButton);

            // Set the form's AcceptButton and CancelButton properties to handle the Enter and Escape keys
            inputBox.AcceptButton = okButton;
            inputBox.CancelButton = cancelButton;

            // Size the form to fit the contents with some padding
            inputBox.AutoSize = true;
            inputBox.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            inputBox.ApplyTheme();
            // Show the form as a modal dialog box
            var result = inputBox.ShowDialog();
            input = textBox.Text; // Update the input parameter with the text entered by the user
            return result; // Return the result of the dialog box
        }

        public static void ShowCodeViewer(string sql, string title = "", CodeEditor.CodeEditorModes Language = CodeEditor.CodeEditorModes.SQL)
        {
            if (Language != CodeEditor.CodeEditorModes.None && ShouldDisableSyntaxHighlighting(sql)) // Turn off syntax highlighting if it's likely to be problematic
            {
                Language = CodeEditor.CodeEditorModes.None;
            }

            CodeViewer frmCodeViewer = new()
            {
                Language = Language,
                Code = sql,
                Text = "Code Viewer" + (string.IsNullOrEmpty(title) ? "" : " - " + title)
            };
            if (frmCodeViewer.WindowState == FormWindowState.Minimized)
            {
                frmCodeViewer.WindowState = FormWindowState.Normal;
            }
            frmCodeViewer.ShowSingleInstance();
        }

        private static bool ShouldDisableSyntaxHighlighting(string txt)
        {
            if (string.IsNullOrEmpty(txt))
                return false;

            // Split on newline
            var lines = txt.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            // Disable syntax highlighting if any line of code is longer than 50K.
            // Long single lines of code with additional factors like a large number of punctuation or whitespace can cause the app to hang. #1561
            return lines.Any(line => line.Length > 50000);
        }

        public static string GetTempFilePath(string extension)
            => Path.Combine(Path.GetTempPath(), TempFilePrefix + Guid.NewGuid() + (extension.StartsWith(".") ? extension : "." + extension));

        public static Image Base64StringAsImage(string base64String)
        {
            var bytes = Convert.FromBase64String(base64String);

            using MemoryStream ms = new(bytes);
            return Image.FromStream(ms);
        }

        public static void DownloadFile(string localPath, string url)
        {
            using var client = new HttpClient();
            using var s = client.GetStreamAsync(url);
            using var fs = new FileStream(localPath, FileMode.OpenOrCreate);
            s.Result.CopyTo(fs);
        }

        /// <summary>
        /// Show exception dialog.  Includes OK and Copy Error Details buttons by default.
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <param name="heading">Defaults to exception message</param>
        /// <param name="caption">Title.  e.g. "Error"</param>
        /// <param name="icon">Defaults to error icon</param>
        /// <param name="text">Additional text.  If heading is set, defaults to exception message</param>
        /// <param name="buttons">Option to replace OK button.  Copy Error Details button is always included</param>
        /// <returns></returns>
        public static TaskDialogButton ShowExceptionDialog(Exception ex, string heading = null, string caption = "Error", TaskDialogIcon icon = null, string text = null, TaskDialogButtonCollection buttons = null)
        {
            heading ??= ex?.Message;
            text ??= heading == ex?.Message ? null : ex?.Message;
            return ShowExceptionDialog(heading, text, ex?.ToString(), caption, icon, buttons);
        }

        /// <summary>
        /// Show exception dialog.  Includes OK and Copy Error Details buttons by default.
        /// </summary>
        /// <param name="heading">Main exception message</param>
        /// <param name="text">Additional text.</param>
        /// <param name="expanderText">Exception details. Hidden by default</param>
        /// <param name="caption">Title.  e.g. "Error"</param>
        /// <param name="icon">Defaults to error icon</param>
        /// <param name="buttons">Option to replace OK button.  Copy Error Details button is always included</param>
        /// <returns></returns>
        public static TaskDialogButton ShowExceptionDialog(string heading, string text, string expanderText, string caption = "Error", TaskDialogIcon icon = null, TaskDialogButtonCollection buttons = null)
        {
            var copyButton = new TaskDialogButton("Copy Error Details") { AllowCloseDialog = false };
            buttons ??= new TaskDialogButtonCollection() { TaskDialogButton.OK };
            buttons.Add(copyButton);
            icon ??= TaskDialogIcon.Error;
            var page = new TaskDialogPage
            {
                Caption = caption,
                Heading = heading,
                Text = text,
                Icon = icon,
                Buttons = buttons,
                Expander = new TaskDialogExpander()
                {
                    Text = expanderText,
                    CollapsedButtonText = "Show error details",
                    ExpandedButtonText = "Hide error details"
                },
                SizeToContent = true
            };
            copyButton.Click += (sender, e) =>
            {
                var clipboardText = BuildClipboardText(caption, heading, text, expanderText);
                try
                {
                    Clipboard.SetText(clipboardText);
                }
                catch (ExternalException)
                {
                    // Clipboard access failed - silently continue
                    // Could optionally show a brief message or beep
                }
            };
            return TaskDialog.ShowDialog(page);
        }

        private static string BuildClipboardText(string caption, string heading, string text, string expanderText)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(caption) && caption != "Error")
                parts.Add(caption);

            if (!string.IsNullOrWhiteSpace(heading))
                parts.Add(heading);

            if (!string.IsNullOrWhiteSpace(text))
                parts.Add(text);

            if (!string.IsNullOrWhiteSpace(expanderText))
                parts.Add($"{Environment.NewLine}Details:{Environment.NewLine}{new string('-', 8)}{Environment.NewLine}{expanderText}");
            parts.Add($"${Environment.NewLine}{new string('-', 8)}{Environment.NewLine}{Application.ProductName} {Application.ProductVersion}");
            parts.Add($"Date: {DateTimeOffset.Now}");

            return string.Join(Environment.NewLine, parts);
        }

        public static bool IsRunningAsAdmin()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        public static void RestartAsAdmin()
        {
            try
            {
                if (Environment.ProcessPath == null)
                {
                    throw new Exception("Environment.ProcessPath returned null");
                }
                var processInfo = new ProcessStartInfo
                {
                    FileName = Environment.ProcessPath,
                    UseShellExecute = true,
                    Verb = "runas"
                };

                Process.Start(processInfo);
                Application.Exit();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to restart as administrator: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Checks if the current user has write access to a file
        /// </summary>
        /// <param name="filePath">Path to the file to check</param>
        /// <returns>True if user has write access, false otherwise</returns>
        public static bool HasWriteAccess(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    // Method 1: Try to open the file for writing (most reliable)
                    using (var fs = File.OpenWrite(filePath))
                    {
                        return true;
                    }
                }
                else
                {
                    // File doesn't exist - check if we can write to the directory
                    var directory = Path.GetDirectoryName(filePath);

                    if (!Directory.Exists(directory))
                        return false;

                    // Try to create a temporary file in the directory
                    var tempFile = Path.Combine(directory, Path.GetRandomFileName());
                    using (var fs = File.Create(tempFile))
                    {
                        // Successfully created temp file
                    }
                    File.Delete(tempFile); // Clean up
                    return true;
                }
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (IOException)
            {
                // File might be in use, but we have write permissions
                return true;
            }
            catch (Exception)
            {
                // Other exceptions (file not found, invalid path, etc.)
                return false;
            }
        }

        /// <summary>
        /// Supplies the time used in default export file names.  The GUI replaces this to apply the user's chosen time zone.
        /// </summary>
        public static Func<DateTime> AppNow { get; set; } = () => DateTime.Now;

        public static void CopyHtmlToClipBoard(string html)
        {
            var enc = Encoding.UTF8;

            var begin = "Version:0.9\r\nStartHTML:{0:000000}\r\nEndHTML:{1:000000}"
                        + "\r\nStartFragment:{2:000000}\r\nEndFragment:{3:000000}\r\n";

            var html_begin = "<html>\r\n<head>\r\n"
                             + "<meta http-equiv=\"Content-Type\""
                             + " content=\"text/html; charset=" + enc.WebName + "\">\r\n"
                             + "<title>HTML clipboard</title>\r\n</head>\r\n<body>\r\n"
                             + "<!--StartFragment-->";

            var html_end = "<!--EndFragment-->\r\n</body>\r\n</html>\r\n";

            var begin_sample = string.Format(begin, 0, 0, 0, 0);

            var count_begin = enc.GetByteCount(begin_sample);
            var count_html_begin = enc.GetByteCount(html_begin);
            var count_html = enc.GetByteCount(html);
            var count_html_end = enc.GetByteCount(html_end);

            var html_total = string.Format(
              begin
              , count_begin
              , count_begin + count_html_begin + count_html + count_html_end
              , count_begin + count_html_begin
              , count_begin + count_html_begin + count_html
              ) + html_begin + html + html_end;

            DataObject obj = new();
            obj.SetData(DataFormats.Html, new MemoryStream(
              enc.GetBytes(html_total)));

            obj.SetData(DataFormats.Text, html);

            Clipboard.SetDataObject(obj, true);
        }

        public static void CopyDataGridViewToClipboard(DataGridView dgv)
        {
            CopyDataGridViewToClipboard(dgv, DashColors.TrimbleBlue, Color.White);
        }

        public static void CopyDataGridViewToClipboard(DataGridView dgv, Color headerBGcolor, Color headerColor)
        {
            var DataGridView1Counts = dgv.Rows.Count;

            StringBuilder html = new();
            html.Append("<table>");

            if (DataGridView1Counts > 0)
            {
                html.Append("<tr>");
                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    if (col.Visible)
                    {
                        html.Append(string.Format("<th style=\"background-color:{1};color:{2};\">{0}</th>", col.HeaderText, headerBGcolor.ToHexString(), headerColor.ToHexString()));
                    }
                }
                html.Append("</tr>");

                foreach (DataGridViewRow row in dgv.Rows)
                {
                    html.Append("<tr>");
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        if (cell.Visible)
                        {
                            html.AppendFormat("<td style=\"background-color:{1}; color:{2};\">{0}</td>", cell.FormattedValue, ColorTranslator.ToHtml(cell.Style.BackColor), ColorTranslator.ToHtml(cell.Style.ForeColor));
                        }
                    }
                    html.Append("</tr>");
                }
            }
            html.Append("</table>");
            CopyHtmlToClipBoard(html.ToString());
        }

        public static string DataTableToHTML(DataTable dt, Color? headerBGcolor = null, Color? headerColor = null)
        {
            headerBGcolor ??= DashColors.TrimbleBlue;
            headerColor ??= DashColors.White;
            var DataGridView1Counts = dt.Rows.Count;

            StringBuilder html = new();
            html.Append("<table>");

            if (DataGridView1Counts > 0)
            {
                html.Append("<tr>");
                foreach (DataColumn col in dt.Columns)
                {
                    html.Append(string.Format("<th style=\"background-color:{1};color:{2};\">{0}</th>", col.Caption, headerBGcolor.Value.ToHexString(), headerColor.Value.ToHexString()));
                }
                html.Append("</tr>");

                foreach (DataRow row in dt.Rows)
                {
                    html.Append("<tr>");
                    foreach (DataColumn col in dt.Columns)
                    {
                        html.AppendFormat("<td>{0}</td>", row[col]);
                    }
                    html.Append("</tr>");
                }
            }
            html.Append("</table>");
            return html.ToString();
        }

        public static void CopyDataTableToClipboard(DataTable dt, Color? headerBGcolor = null, Color? headerColor = null)
        {
            var html = DataTableToHTML(dt, headerBGcolor, headerColor);
            CopyHtmlToClipBoard(html);
        }

        public static void PromptSaveDataGridView(ref DataGridView dgv)
        {
            PromptSaveDataGridView(new DataGridView[] { dgv });
        }

        public static void PromptSaveDataGridView(DataGridView dgv)
        {
            PromptSaveDataGridView(new DataGridView[] { dgv });
        }

        public static void PromptSaveDataGridView(DataGridView[] dgv)
        {
            var defaultFileName = "DBADash_" + AppNow().ToString("yyyyMMdd_HHmmss") + ".xlsx";
            using var ofd = new SaveFileDialog() { FileName = defaultFileName, AddExtension = true, DefaultExt = ".xlsx" };
            if (ofd.ShowDialog() != DialogResult.OK) return;
            if (File.Exists(ofd.FileName))
            {
                if (MessageBox.Show($"Are you sure you want to replace the existing file: {ofd.FileName}", "Confirm Replace", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    File.Delete(ofd.FileName);
                }
                else
                {
                    return;
                }
            }

            try
            {
                SaveDataGridViewsToXLSX(dgv, ofd.FileName); // Assume there will be no invalid XML characters
            }
            catch (XmlException ex)
            {
                Debug.WriteLine("XmlException exporting to Excel, retrying with invalid XML characters removed", ex.ToString());
                SaveDataGridViewsToXLSX(dgv, ofd.FileName, true); // Try again with invalid XML characters removed
                Debug.WriteLine("Invalid XML characters removed");
            }

            var psi = new ProcessStartInfo(ofd.FileName) { UseShellExecute = true };
            Process.Start(psi);
        }

        public static void PromptSaveDataTableToXLSX(DataTable dt)
        {
            var defaultFileName = "DBADash_" + AppNow().ToString("yyyyMMdd_HHmmss") + ".xlsx";
            using var ofd = new SaveFileDialog() { FileName = defaultFileName, AddExtension = true, DefaultExt = ".xlsx" };
            if (ofd.ShowDialog() != DialogResult.OK) return;
            if (File.Exists(ofd.FileName))
            {
                if (MessageBox.Show($"Are you sure you want to replace the existing file: {ofd.FileName}", "Confirm Replace", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    File.Delete(ofd.FileName);
                }
                else
                {
                    return;
                }
            }

            try
            {
                SaveDataTableToXLSX(dt, ofd.FileName); // Assume there will be no invalid XML characters
            }
            catch (XmlException ex)
            {
                Debug.WriteLine("XmlException exporting to Excel, retrying with invalid XML characters removed", ex.ToString());
                SaveDataTableToXLSX(dt, ofd.FileName, true); // Try again with invalid XML characters removed
                Debug.WriteLine("Invalid XML characters removed");
            }

            var psi = new ProcessStartInfo(ofd.FileName) { UseShellExecute = true };
            Process.Start(psi);
        }

        public static void SaveDataGridViewsToXLSX(DataGridView[] Grids, string path, bool replaceInvalidChars = false)
        {
            using var workbook = new XLWorkbook();
            var sheetIndex = 1;
            foreach (var dgv in Grids)
            {
                var sheetName = $"Sheet{sheetIndex}";
                if (dgv is DBADashDataGridView dbadashDgv)
                {
                    sheetName = string.IsNullOrEmpty(dbadashDgv.ResultSetName) ? sheetName : dbadashDgv.ResultSetName;
                }

                var sheet = workbook.Worksheets.Add(sheetName);

                // Respect display order and visibility
                var orderedVisibleColumns = dgv.Columns
                    .Cast<DataGridViewColumn>()
                    .Where(c => c.Visible)
                    .OrderBy(c => c.DisplayIndex)
                    .ToList();

                // Headers
                var headerColIndex = 1;
                foreach (var col in orderedVisibleColumns)
                {
                    sheet.Cell(1, headerColIndex).SetValue(col.HeaderText.Replace("\n", " "));
                    headerColIndex++;
                }

                // Rows
                var rowIndex = 1;
                foreach (DataGridViewRow row in dgv.Rows)
                {
                    rowIndex += 1;
                    var colIndex = 1;

                    foreach (var col in orderedVisibleColumns)
                    {
                        var cell = row.Cells[col.Index];
                        var cellType = cell.ValueType;

                        var format = string.IsNullOrEmpty(cell.Style.Format)
                            ? cell.InheritedStyle.Format
                            : cell.Style.Format;
                        format = format switch
                        {
                            "P1" => "0.0%",
                            "P:" or "P2" => "0.00%",
                            _ => "",
                        };
                        if (cellType == typeof(DateTime))
                        {
                            format = "yyyy-MM-dd HH:mm";
                        }

                        if (!cell.Style.ForeColor.IsEmpty || !cell.Style.BackColor.IsEmpty ||
                            !string.IsNullOrEmpty(format))
                        {
                            var xlCell = sheet.Cell(rowIndex, colIndex);
                            var backColor = cell.Style.BackColor.IsEmpty ? Color.Transparent : cell.Style.BackColor;
                            xlCell.Style.Fill.SetBackgroundColor(XLColor.FromColor(backColor));
                            xlCell.Style.Font.SetFontColor(XLColor.FromColor(cell.Style.ForeColor));
                            xlCell.Style.NumberFormat.Format = format;
                        }

                        try
                        {
                            if (cell.Value == DBNull.Value)
                            {
                                sheet.Cell(rowIndex, colIndex).SetValue(Convert.ToString(cell.FormattedValue));
                            }
                            else if (cellType == typeof(bool))
                            {
                                sheet.Cell(rowIndex, colIndex).SetValue((bool)cell.Value);
                            }
                            else if (cellType.IsNumericType())
                            {
                                if (!decimal.TryParse(cell.FormattedValue as string, out var decimalValue))
                                {
                                    decimalValue = Convert.ToDecimal(cell.Value);
                                }

                                sheet.Cell(rowIndex, colIndex).SetValue(decimalValue);
                            }
                            else if (cellType == typeof(DateTime))
                            {
                                sheet.Cell(rowIndex, colIndex).SetValue(Convert.ToDateTime(cell.Value));
                            }
                            else if (cellType == typeof(byte[]))
                            {
                                sheet.Cell(rowIndex, colIndex).SetValue(Convert.ToString(cell.Value));
                            }
                            else
                            {
                                sheet.Cell(rowIndex, colIndex).SetValue(replaceInvalidChars
                                    ? Convert.ToString(cell.FormattedValue).StripInvalidXmlChars().Truncate(32767, true)
                                    : Convert.ToString(cell.FormattedValue).Truncate(32767, true));
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.ToString());
                        }

                        colIndex += 1;
                    }
                }

                var finalColCount = orderedVisibleColumns.Count;
                var table = sheet.Range(sheet.Cell(1, 1).Address, sheet.Cell(rowIndex, finalColCount).Address).CreateTable();
                table.Theme = XLTableTheme.None;
                var header = sheet.Range(1, 1, 1, finalColCount);
                header.Style.Fill.SetBackgroundColor(XLColor.FromColor(DashColors.TrimbleBlue));
                header.Style.Font.SetFontColor(XLColor.White);
                header.Style.Font.SetBold();
                var maxColumnWidth = 150;
                sheet.Columns().AdjustToContents();
                for (var i = 1; i <= finalColCount; i++)
                {
                    sheet.Column(i).Width = Math.Min(sheet.Column(i).Width, maxColumnWidth);
                }
                sheetIndex += 1;
            }
            workbook.SaveAs(path);
        }

        public static void SaveDataTableToXLSX(DataTable dataTable, string excelFilePath, bool replaceInvalidChars = false)
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Sheet1");

            // Header
            for (var i = 0; i < dataTable.Columns.Count; i++)
            {
                sheet.Cell(1, i + 1).Value = dataTable.Columns[i].ColumnName;
            }

            // Add the DataTable's data to the Excel file, starting from the second row
            for (var rowIndex = 0; rowIndex < dataTable.Rows.Count; rowIndex++)
            {
                for (var colIndex = 0; colIndex < dataTable.Columns.Count; colIndex++)
                {
                    var value = dataTable.Rows[rowIndex][colIndex];
                    if (value == DBNull.Value)
                    {
                        continue;
                    }

                    var valueType = value.GetType();
                    if (valueType.IsNumericType())
                    {
                        sheet.Cell(rowIndex + 2, colIndex + 1).SetValue(Convert.ToDecimal(value));
                    }
                    else if (valueType == typeof(DateTime))
                    {
                        sheet.Cell(rowIndex + 2, colIndex + 1).SetValue(Convert.ToDateTime(value));
                    }
                    else if (valueType == typeof(bool))
                    {
                        sheet.Cell(rowIndex + 2, colIndex + 1).SetValue(Convert.ToBoolean(value));
                    }
                    else
                    {
                        var cellStr = replaceInvalidChars
                            ? Convert.ToString(value).StripInvalidXmlChars().Truncate(32767, true)
                            : Convert.ToString(value).Truncate(32767, true);

                        sheet.Cell(rowIndex + 2, colIndex + 1).SetValue(cellStr);
                    }
                }
            }

            var table = sheet.Range(sheet.Cell(1, 1).Address, sheet.Cell(dataTable.Rows.Count + 1, dataTable.Columns.Count).Address).CreateTable();
            table.Theme = XLTableTheme.None;
            var header = sheet.Range(1, 1, 1, dataTable.Columns.Count);
            header.Style.Fill.SetBackgroundColor(XLColor.FromColor(DashColors.TrimbleBlue));
            header.Style.Font.SetFontColor(XLColor.White);
            header.Style.Font.SetBold();
            var maxColumnWidth = 150;
            sheet.Columns().AdjustToContents();
            for (var i = 1; i <= dataTable.Columns.Count; i++)
            {
                sheet.Column(i).Width = Math.Min(sheet.Column(i).Width, maxColumnWidth);
            }

            // Save the workbook to the specified file path
            workbook.SaveAs(excelFilePath);
        }

        public static void SaveDataGridViewToXLSX(ref DataGridView dgv, string path, bool replaceInvalidChars = false)
        {
            SaveDataGridViewsToXLSX(new DataGridView[] { dgv }, path, replaceInvalidChars);
        }
    }
}