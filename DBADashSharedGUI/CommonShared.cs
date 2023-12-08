using DBADash;
using DBADashGUI;
using Microsoft.SqlServer.Management.Common;
using System.Diagnostics;
using System.Runtime.Versioning;
using DBADashGUI.Theme;

namespace DBADashSharedGUI
{
    public class CommonShared
    {
        public static void OpenURL(string url)
        {
            if (!IsValidUrl(url))
            {
                throw new InvalidArgumentException("Invalid URL: " + url);
            };
            var psi = new ProcessStartInfo(url) { UseShellExecute = true };
            Process.Start(psi);
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
                    dbVersion = DBADash.DBValidations.GetDBVersion(connectionString);
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
            if (!DBADash.Upgrade.IsUpgradeIncomplete) return;

            MessageBox.Show(DBADash.Upgrade.IncompleteUpgradeMessage, "Error", MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            if (MessageBox.Show("Retry upgrade?", "Retry", MessageBoxButtons.YesNo, MessageBoxIcon.Question) ==
                DialogResult.Yes)
            {
                await Upgrade.UpgradeDBADashAsync();
            }

            Application.Exit();
        }

        [SupportedOSPlatform("windows")]
        public static DialogResult ShowInputDialog(ref string input, string title, char passwordChar = '\0', string? description = null)
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
                RowCount = 3, // One for the description, one for the textbox, one for the button row
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
    }
}