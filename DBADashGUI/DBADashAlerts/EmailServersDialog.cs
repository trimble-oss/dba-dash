using DBADash.Alert;
using DBADashGUI.Performance;
using DBADashGUI.Theme;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.DBADashAlerts
{
    /// <summary>
    /// Lists the shared SMTP settings (Email Servers).  Email channels pick one of these instead of repeating the settings.
    /// </summary>
    public class EmailServersDialog : Form
    {
        // FormattingEnabled: the Format handler that adds the usage counts only runs when it's on
        private readonly ListBox lstServers = new() { Dock = DockStyle.Fill, IntegralHeight = false, FormattingEnabled = true };
        private readonly Button bttnAdd = new() { Text = "Add...", AutoSize = true };
        private readonly Button bttnEdit = new() { Text = "Edit...", AutoSize = true };
        private readonly Button bttnDelete = new() { Text = "Delete", AutoSize = true };
        private readonly Button bttnClose = new() { Text = "Close", AutoSize = true, DialogResult = DialogResult.OK };

        public EmailServersDialog()
        {
            Text = "Email Servers";
            StartPosition = FormStartPosition.CenterParent;
            ShowIcon = false;
            ShowInTaskbar = false;
            MinimizeBox = false;
            Size = new Size(520, 360);
            MinimumSize = new Size(380, 240);
            AcceptButton = bttnClose;

            var info = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 56,
                Padding = new Padding(8),
                Text = "SMTP settings for email notification channels and daily reports. Set up a server once, then select it on each email channel and daily report, which only need their own recipients."
            };

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
            buttons.Controls.AddRange(new Control[] { bttnClose, bttnDelete, bttnEdit, bttnAdd });

            lstServers.Format += (_, e) =>
            {
                if (e.ListItem is EmailServer server)
                {
                    e.Value = $"{server.EmailServerName} ({Count(server.ChannelCount, "channel")}, {Count(server.ReportCount, "daily report")})";
                }
            };
            lstServers.SelectedIndexChanged += (_, _) => UpdateButtons();
            lstServers.DoubleClick += (_, _) => Edit();

            bttnAdd.Click += (_, _) => Add();
            bttnEdit.Click += (_, _) => Edit();
            bttnDelete.Click += (_, _) => Delete();

            Controls.Add(lstServers);
            Controls.Add(info);
            Controls.Add(buttons);

            this.ApplyTheme();
            LoadServers();
        }

        private static string Count(int count, string noun) => $"{count} {noun}{(count == 1 ? string.Empty : "s")}";

        private EmailServer SelectedServer => lstServers.SelectedItem as EmailServer;

        private void UpdateButtons()
        {
            bttnEdit.Enabled = SelectedServer != null;
            bttnDelete.Enabled = SelectedServer != null;
        }

        private void LoadServers()
        {
            try
            {
                var selectedID = SelectedServer?.EmailServerID;
                lstServers.BeginUpdate();
                lstServers.Items.Clear();
                lstServers.Items.AddRange(EmailServer.GetServerList(Common.ConnectionString).ToArray());
                lstServers.EndUpdate();
                for (var i = 0; i < lstServers.Items.Count && selectedID != null; i++)
                {
                    if (((EmailServer)lstServers.Items[i]).EmailServerID == selectedID) lstServers.SelectedIndex = i;
                }
                UpdateButtons();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }
        }

        private void Add() => EditServer(new EmailServer());

        private void Edit()
        {
            var selected = SelectedServer;
            if (selected == null) return;
            try
            {
                EditServer(EmailServer.GetServer(selected.EmailServerID, Common.ConnectionString));
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }
        }

        private void EditServer(EmailServer server)
        {
            if (EditServer(this, server)) LoadServers();
        }

        /// <summary>Edits and saves a server.  Returns true when it was saved.  Also used by the email server picker.</summary>
        internal static bool EditServer(IWin32Window owner, EmailServer server)
        {
            using var edit = new PropertyGridDialog();
            edit.Text = server.EmailServerID > 0 ? "Edit Email Server" : "Add Email Server";
            edit.SelectedObject = server;
            if (edit.ShowDialog(owner) != DialogResult.OK) return false;
            try
            {
                server.Save(Common.ConnectionString);
                EmailServerConverter.Invalidate();
                return true;
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
                return false;
            }
        }

        private void Delete()
        {
            var selected = SelectedServer;
            if (selected == null) return;
            if (MessageBox.Show($"Delete email server '{selected.EmailServerName}'?", "Delete Email Server",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                EmailServer.Delete(Common.ConnectionString, selected.EmailServerID);
                EmailServerConverter.Invalidate();
                LoadServers();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }
        }
    }
}
