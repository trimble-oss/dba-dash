using DBADash;
using DBADashGUI.Theme;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashServiceConfig
{
    /// <summary>
    /// Lists the secrets in the config that can't be decrypted on this machine - after copying the config from another
    /// machine, or using a config exported without secrets - so they can be entered again.
    /// </summary>
    public class ReEnterPasswordsDialog : Form
    {
        /// <summary>The secrets to enter.  Those entered are set when the dialog closes with OK.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<ConfigSecret> Secrets { get; set; } = new();

        /// <summary>How many secrets were set</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int UpdatedCount { get; private set; }

        // A plain grid - the shared DBADashDataGridView has export and copy options that would write the passwords out
        private DataGridView dgv;
        private CheckBox chkSameUser;
        private Button bttnTest;
        private Button bttnOK;
        private Button bttnSkip;
        private bool isTesting;

        private const string PasswordColumn = "Password";
        private const string StatusColumn = "Status";
        private const string BlankPasswordWorks = "Connected with a blank password";

        // The password when the current cell started editing, to tell if it changed
        private string passwordBeforeEdit;

        // Server and login -> whether a blank password worked.  Each is tried once while the dialog is open.
        private readonly Dictionary<string, bool> blankPasswordResults = new(StringComparer.OrdinalIgnoreCase);

        private static string LoginKey(DataGridViewRow row) => SecretOf(row).Connection.DataSource() + "|" + SecretOf(row).UserName;

        public ReEnterPasswordsDialog()
        {
            Text = "Re-enter Passwords";
            AutoScaleMode = AutoScaleMode.None;
            Width = 900;
            Height = 560;
            MinimumSize = new Size(700, 420);
            StartPosition = FormStartPosition.CenterParent;
            BuildLayout();
            Load += (_, _) => LoadSecrets();
            // A test writes its result to the grid when it completes
            FormClosing += (_, e) => e.Cancel = isTesting;
        }

        private void BuildLayout()
        {
            var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(8) };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            tlp.Controls.Add(new Label
            {
                AutoSize = true,
                MaximumSize = new Size(860, 0),
                Margin = new Padding(3, 3, 3, 8),
                Text = "These passwords and secrets can't be decrypted on this machine.  Secrets in the config are protected so they can only be decrypted on the machine they were saved on, " +
                       "or with the config encryption password - so they need entering again if the config was copied from another machine.  " +
                       "Connections without a password won't be used until it's entered, and AWS (S3/SQS) access needs the AWS secret key.\n\n" +
                       "To move DBA Dash without re-entering passwords next time, use Export Config on the original machine and choose to include the passwords, protected with an export password."
            }, 0, 0);

            chkSameUser = new CheckBox { Text = "Use the password for every row with the same user name", AutoSize = true, Checked = true, Margin = new Padding(3, 3, 3, 6) };
            tlp.Controls.Add(chkSameUser, 0, 1);

            dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EditMode = DataGridViewEditMode.EditOnEnter,
                ClipboardCopyMode = DataGridViewClipboardCopyMode.Disable
            };
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Type", Name = "Type", ReadOnly = true, FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Connection", Name = "Connection", ReadOnly = true, FillWeight = 35 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "User", Name = "User", ReadOnly = true, FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Password / Secret", Name = PasswordColumn, FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Status", Name = StatusColumn, ReadOnly = true, FillWeight = 25 });
            dgv.EditingControlShowing += Dgv_EditingControlShowing;
            dgv.CellFormatting += Dgv_CellFormatting;
            dgv.CellBeginEdit += (_, e) => passwordBeforeEdit = PasswordOf(dgv.Rows[e.RowIndex]);
            dgv.CellEndEdit += Dgv_CellEndEdit;
            tlp.Controls.Add(dgv, 0, 2);

            var pnlButtons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.RightToLeft };
            bttnSkip = new Button { Text = "Skip", AutoSize = true, DialogResult = DialogResult.Cancel };
            pnlButtons.Controls.Add(bttnSkip);
            bttnOK = new Button { Text = "OK", AutoSize = true };
            bttnOK.Click += async (_, _) => await OKAsync();
            pnlButtons.Controls.Add(bttnOK);
            bttnTest = new Button { Text = "Test connections", AutoSize = true };
            bttnTest.Click += async (_, _) => await TestAsync();
            pnlButtons.Controls.Add(bttnTest);
            tlp.Controls.Add(pnlButtons, 0, 3);

            CancelButton = bttnSkip;
            Controls.Add(tlp);
            this.ApplyTheme();
        }

        private void LoadSecrets()
        {
            foreach (var secret in Secrets)
            {
                var i = dgv.Rows.Add(secret.TypeDescription, secret.Name, secret.UserName, "", RequiredStatus(secret));
                dgv.Rows[i].Tag = secret;
            }
        }

        private static ConfigSecret SecretOf(DataGridViewRow row) => (ConfigSecret)row.Tag;

        private static string PasswordOf(DataGridViewRow row) => row.Cells[PasswordColumn].Value as string ?? "";

        private static string Describe(ConfigSecret secret) =>
            secret.Connection == null ? secret.TypeDescription : $"{secret.TypeDescription} {secret.Name}";

        private static string RequiredStatus(ConfigSecret secret) => secret.Connection == null ? "Secret key required" : "Password required";

        private void Dgv_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (e.Control is TextBox txt)
            {
                var isPassword = dgv.CurrentCell?.OwningColumn.Name == PasswordColumn;
                txt.UseSystemPasswordChar = isPassword;
                if (isPassword)
                {
                    // The editor starts with the formatted value - the ● mask, not the password
                    txt.Text = PasswordOf(dgv.CurrentCell.OwningRow);
                    txt.SelectionStart = txt.Text.Length;
                }
            }
        }

        private void Dgv_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgv.Columns[e.ColumnIndex].Name == PasswordColumn && e.Value is string { Length: > 0 } value)
            {
                e.Value = new string('●', value.Length);
                e.FormattingApplied = true;
            }
        }

        private void Dgv_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (dgv.Columns[e.ColumnIndex].Name != PasswordColumn) return;
            var row = dgv.Rows[e.RowIndex];
            var password = PasswordOf(row);
            // EditOnEnter starts and ends editing without a change (e.g. after testing) - that mustn't reset a test result
            if (password == passwordBeforeEdit) return;
            row.Cells[StatusColumn].Value = password.Length > 0 ? "Entered" : RequiredStatus(SecretOf(row));
            var userName = SecretOf(row).UserName;
            if (!chkSameUser.Checked || password.Length == 0 || string.IsNullOrEmpty(userName)) return;
            foreach (DataGridViewRow other in dgv.Rows)
            {
                if (other.Index == row.Index || !string.Equals(SecretOf(other).UserName, userName, StringComparison.OrdinalIgnoreCase)) continue;
                other.Cells[PasswordColumn].Value = password;
                other.Cells[StatusColumn].Value = "Entered";
            }
        }

        /// <summary>Connects with each password entered.  Each test is a login attempt, so only rows with a password are tested.</summary>
        private async Task TestAsync()
        {
            dgv.EndEdit();
            var rows = dgv.Rows.Cast<DataGridViewRow>().Where(r => PasswordOf(r).Length > 0 && SecretOf(r).Connection != null).ToList();
            await TestRowsAsync(rows, "Testing...", "Connected");
        }

        /// <summary>
        /// Connects with each row's password, writing the result to its status.  The grid can't be changed until the tests
        /// finish, so a result always matches the password shown.  Returns the rows that connected.
        /// </summary>
        private async Task<List<DataGridViewRow>> TestRowsAsync(List<DataGridViewRow> rows, string testingStatus, string connectedStatus)
        {
            var connected = new List<DataGridViewRow>();
            if (rows.Count == 0) return connected;
            SetTesting(true);
            try
            {
                foreach (var row in rows) row.Cells[StatusColumn].Value = testingStatus;
                await Task.WhenAll(rows.Select(async row =>
                {
                    var builder = new SqlConnectionStringBuilder(SecretOf(row).GetConnectionString(PasswordOf(row))) { ConnectTimeout = 15 };
                    try
                    {
                        await using var cn = new SqlConnection(builder.ConnectionString);
                        await cn.OpenAsync();
                        row.Cells[StatusColumn].Value = connectedStatus;
                        connected.Add(row);
                    }
                    catch (Exception ex)
                    {
                        row.Cells[StatusColumn].Value = ex.Message;
                    }
                }));
            }
            finally
            {
                SetTesting(false);
            }
            return connected;
        }

        private void SetTesting(bool testing)
        {
            isTesting = testing;
            dgv.ReadOnly = testing;
            chkSameUser.Enabled = bttnTest.Enabled = bttnOK.Enabled = bttnSkip.Enabled = !testing;
            if (!testing)
            {
                // ReadOnly = false clears the columns' own read only setting
                foreach (DataGridViewColumn column in dgv.Columns) column.ReadOnly = column.Name != PasswordColumn;
            }
        }

        private async Task OKAsync()
        {
            dgv.EndEdit();
            var protectedValues = dgv.Rows.Cast<DataGridViewRow>().Where(r => SecretProtection.IsProtected(PasswordOf(r))).Select(r => Describe(SecretOf(r))).ToList();
            if (protectedValues.Count > 0)
            {
                MessageBox.Show("Enter the password or secret itself, not a protected value copied from a config file:\n\n" + string.Join("\n", protectedValues),
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            // A connection left empty might log in with a blank password (as it did before the passwords were removed by an
            // export without secrets).  Try it rather than offering a blank password as an option - but only once for each
            // server and login while the dialog is open, as failed logins count towards locking the login out.
            var empty = dgv.Rows.Cast<DataGridViewRow>().Where(r => PasswordOf(r).Length == 0 && SecretOf(r).Connection != null).ToList();
            var untried = empty.Where(r => !blankPasswordResults.ContainsKey(LoginKey(r)))
                .GroupBy(LoginKey, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();
            var connected = await TestRowsAsync(untried, "Trying a blank password...", BlankPasswordWorks);
            foreach (var row in untried) blankPasswordResults[LoginKey(row)] = connected.Contains(row);
            var blankWorks = empty.Where(r => blankPasswordResults[LoginKey(r)]).ToList();
            foreach (var row in empty.Except(untried))
            {
                row.Cells[StatusColumn].Value = blankWorks.Contains(row) ? BlankPasswordWorks : "Password required (a blank password didn't work)";
            }

            var missing = dgv.Rows.Cast<DataGridViewRow>().Where(r => PasswordOf(r).Length == 0 && !blankWorks.Contains(r)).Select(SecretOf).ToList();
            if (missing.Count > 0)
            {
                const int maxListed = 10;
                var list = string.Join("\n", missing.Take(maxListed).Select(Describe)) + (missing.Count > maxListed ? $"\n...and {missing.Count - maxListed} more" : "");
                var consequences = new List<string>();
                if (missing.Any(s => s.Connection != null)) consequences.Add("Connections without a password won't be used until it's entered.");
                if (missing.Any(s => s.Connection == null)) consequences.Add("AWS (S3/SQS) access won't work until the AWS secret key is entered.");
                if (MessageBox.Show($"{missing.Count} secret(s) haven't been entered:\n\n{list}\n\n{string.Join("  ", consequences)}  Continue?",
                        Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                {
                    return;
                }
            }
            UpdatedCount = 0;
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (PasswordOf(row).Length == 0 && !blankWorks.Contains(row)) continue;
                SecretOf(row).SetSecret(PasswordOf(row));
                UpdatedCount++;
            }
            DialogResult = DialogResult.OK;
        }
    }
}
