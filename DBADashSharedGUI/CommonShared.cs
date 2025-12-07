using DBADash;
using DBADashGUI;
using DBADashGUI.SchemaCompare;
using DBADashGUI.Theme;
using Microsoft.SqlServer.Management.Common;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Principal;

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
        public static void ShowAbout(IWin32Window owner, bool StartGUIOnUpgrade)
        {
            using About frm = new()
            {
                DBVersion = new Version(),
                StartGUIOnUpgrade = StartGUIOnUpgrade
            };
            frm.ShowDialog(owner);
        }

        [SupportedOSPlatform("windows")]
        public static void ShowAbout(string connectionString, IWin32Window owner, bool StartGUIOnUpgrade)
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
                StartGUIOnUpgrade = StartGUIOnUpgrade
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
    }
}