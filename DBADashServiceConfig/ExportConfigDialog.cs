using DBADash;
using DBADashGUI.Theme;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace DBADashServiceConfig
{
    /// <summary>
    /// Writes a copy of the config to move DBA Dash to another machine or keep as a backup: either with the secrets
    /// protected by an export password (<see cref="CollectionConfig.ExportEncrypted"/>), or without them
    /// (<see cref="CollectionConfig.ExportWithoutSecrets"/>).  The config itself isn't changed.
    /// </summary>
    public class ExportConfigDialog : Form
    {
        /// <summary>The config to export.  Exported as it is in the tool, including changes that haven't been saved.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public CollectionConfig Config { get; set; }

        private RadioButton rbWithPassword;
        private RadioButton rbWithoutSecrets;
        private TextBox txtPassword;
        private TextBox txtConfirm;
        private CheckBox chkShow;
        private TextBox txtPath;
        private Panel pnlPassword;

        public ExportConfigDialog()
        {
            Text = "Export Config";
            AutoScaleMode = AutoScaleMode.None;
            Width = 640;
            Height = 520;
            MinimumSize = new Size(560, 480);
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            BuildLayout();
        }

        private void BuildLayout()
        {
            var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, Padding = new Padding(10) };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (var i = 0; i < 5; i++) tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            tlp.Controls.Add(new Label
            {
                AutoSize = true,
                MaximumSize = new Size(590, 0),
                Margin = new Padding(3, 3, 3, 10),
                Text = "Passwords in the config can only be decrypted on this machine (or with the config encryption password).  " +
                       "Export a copy to move DBA Dash to another machine, or to keep as a backup."
            }, 0, 0);

            rbWithPassword = new RadioButton { Text = "Include passwords, protected with an export password", AutoSize = true, Checked = true };
            rbWithoutSecrets = new RadioButton { Text = "Leave passwords out - they're entered again when the config is used", AutoSize = true };
            rbWithPassword.CheckedChanged += (_, _) => pnlPassword.Enabled = rbWithPassword.Checked;
            var pnlMode = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, Dock = DockStyle.Fill };
            pnlMode.Controls.Add(rbWithPassword);
            pnlMode.Controls.Add(rbWithoutSecrets);
            tlp.Controls.Add(pnlMode, 0, 1);

            pnlPassword = new Panel { Dock = DockStyle.Fill, Height = 110, Margin = new Padding(20, 3, 3, 3) };
            pnlPassword.Controls.Add(new Label { Text = "Export password:", AutoSize = true, Location = new Point(0, 8) });
            txtPassword = new TextBox { Location = new Point(140, 5), Width = 300, UseSystemPasswordChar = true };
            pnlPassword.Controls.Add(txtPassword);
            pnlPassword.Controls.Add(new Label { Text = "Confirm:", AutoSize = true, Location = new Point(0, 43) });
            txtConfirm = new TextBox { Location = new Point(140, 40), Width = 300, UseSystemPasswordChar = true };
            pnlPassword.Controls.Add(txtConfirm);
            chkShow = new CheckBox { Text = "Show", AutoSize = true, Location = new Point(140, 75) };
            chkShow.CheckedChanged += (_, _) => txtPassword.UseSystemPasswordChar = txtConfirm.UseSystemPasswordChar = !chkShow.Checked;
            pnlPassword.Controls.Add(chkShow);
            var lnkGenerate = new LinkLabel { Text = "Generate", AutoSize = true, Location = new Point(220, 77) };
            lnkGenerate.LinkClicked += (_, _) =>
            {
                txtPassword.Text = txtConfirm.Text = PasswordGenerator.Generate();
                chkShow.Checked = true;
            };
            pnlPassword.Controls.Add(lnkGenerate);
            tlp.Controls.Add(pnlPassword, 0, 2);

            var pnlPath = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = false };
            pnlPath.Controls.Add(new Label { Text = "File:", AutoSize = true, Margin = new Padding(3, 8, 3, 3) });
            txtPath = new TextBox { Width = 440, Margin = new Padding(3, 5, 3, 3) };
            pnlPath.Controls.Add(txtPath);
            var bttnBrowse = new Button { Text = "Browse...", AutoSize = true };
            bttnBrowse.Click += Browse_Click;
            pnlPath.Controls.Add(bttnBrowse);
            tlp.Controls.Add(pnlPath, 0, 3);

            tlp.Controls.Add(new Label
            {
                AutoSize = true,
                MaximumSize = new Size(590, 0),
                Margin = new Padding(3, 10, 3, 3),
                Text = "To use it on another machine: copy the file to the DBA Dash folder as ServiceConfig.json and open the service config tool.  " +
                       "With an export password, enter it when prompted, then use Configure Encryption to either turn encryption off (the passwords are then protected for that machine) or keep it on.  " +
                       "Without passwords, you're prompted to enter them."
            }, 0, 4);

            var pnlButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, FlowDirection = FlowDirection.RightToLeft };
            var bttnCancel = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
            pnlButtons.Controls.Add(bttnCancel);
            var bttnExport = new Button { Text = "Export", AutoSize = true };
            bttnExport.Click += Export_Click;
            pnlButtons.Controls.Add(bttnExport);
            tlp.Controls.Add(pnlButtons, 0, 5);

            AcceptButton = bttnExport;
            CancelButton = bttnCancel;
            Controls.Add(tlp);
            this.ApplyTheme();
        }

        private void Browse_Click(object sender, EventArgs e)
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "Json File|*.json",
                Title = "Export Config",
                FileName = $"ServiceConfig_Export_{DateTime.Now:yyyyMMdd}.json"
            };
            if (sfd.ShowDialog(this) == DialogResult.OK)
            {
                txtPath.Text = sfd.FileName;
            }
        }

        private void Export_Click(object sender, EventArgs e)
        {
            var path = txtPath.Text.Trim();
            if (string.IsNullOrEmpty(path))
            {
                MessageBox.Show("Choose a file to export to.", Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if (string.Equals(Path.GetFullPath(path), Path.GetFullPath(BasicConfig.JsonConfigPath), StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Export to a different file - this is the config file itself.", Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if (rbWithPassword.Checked)
            {
                if (string.IsNullOrEmpty(txtPassword.Text))
                {
                    MessageBox.Show("Enter an export password.", Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
                if (txtPassword.Text != txtConfirm.Text)
                {
                    MessageBox.Show("The passwords don't match.", Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
            }

            try
            {
                var unreadable = Config.GetUnreadableSecrets();
                File.WriteAllText(path, rbWithPassword.Checked ? Config.ExportEncrypted(txtPassword.Text) : Config.ExportWithoutSecrets());
                var message = "Config exported to " + path;
                if (rbWithPassword.Checked && unreadable.Count > 0)
                {
                    message += $"\n\n{unreadable.Count} password(s) can't be decrypted on this machine, so they'll still need to be entered after importing.";
                }
                MessageBox.Show(message, Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error exporting config");
            }
        }
    }
}
