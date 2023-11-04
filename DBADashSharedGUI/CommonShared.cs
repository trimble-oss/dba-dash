using DBADash;
using DBADashGUI;
using Microsoft.SqlServer.Management.Common;
using System.Diagnostics;
using System.Runtime.Versioning;

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

        public static DialogResult ShowInputDialog(ref string input, string title, char passwordChar = '\0')
        {
            System.Drawing.Size size = new(400, 80);
            Form inputBox = new()
            {
                FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog,
                ClientSize = size,
                Text = title,
                MaximizeBox = false,
                MinimizeBox = false,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = DashColors.TrimbleBlueDark
            };

            System.Windows.Forms.TextBox textBox = new()
            {
                Size = new System.Drawing.Size(size.Width - 10, 25),
                Location = new System.Drawing.Point(5, 5),
                Text = input,
                PasswordChar = passwordChar
            };
            inputBox.Controls.Add(textBox);

            Button okButton = new()
            {
                DialogResult = System.Windows.Forms.DialogResult.OK,
                Name = "okButton",
                Size = new System.Drawing.Size(75, 30),
                Text = "&OK",
                Location = new System.Drawing.Point(size.Width - 80 - 80, 39),
                BackColor = SystemColors.Control
            };
            inputBox.Controls.Add(okButton);

            Button cancelButton = new()
            {
                DialogResult = System.Windows.Forms.DialogResult.Cancel,
                Name = "cancelButton",
                Size = new System.Drawing.Size(75, 30),
                Text = "&Cancel",
                Location = new System.Drawing.Point(size.Width - 80, 39),
                BackColor = SystemColors.Control
            };
            inputBox.Controls.Add(cancelButton);

            inputBox.AcceptButton = okButton;
            inputBox.CancelButton = cancelButton;

            DialogResult result = inputBox.ShowDialog();
            input = textBox.Text;
            return result;
        }
    }
}