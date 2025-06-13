using DBADash;
using DBADashGUI;
using DBADashGUI.SchemaCompare;
using DBADashGUI.Theme;
using Microsoft.SqlServer.Management.Common;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.Versioning;
using System.Security;

namespace DBADashSharedGUI
{
    public class CommonShared
    {
        private static CodeViewer FrmCodeViewer;
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
                    dbVersion = DBValidations.GetDBVersion(connectionString);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(@"Error getting repository version: " + ex.Message, "Error", MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
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
            FrmCodeViewer?.Close();
            FrmCodeViewer = new CodeViewer
            {
                Language = Language,
                Code = sql,
                Text = "Code Viewer" + (string.IsNullOrEmpty(title) ? "" : " - " + title)
            };
            if (FrmCodeViewer.WindowState == FormWindowState.Minimized)
            {
                FrmCodeViewer.WindowState = FormWindowState.Normal;
            }
            FrmCodeViewer.FormClosed += (s, e) => FrmCodeViewer = null;
            FrmCodeViewer.Show();
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
    }
}