using DBADash;
using DBADash.Messaging;
using DBADashGUI.Theme;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.Connections
{
    /// <summary>
    /// Removes an instance's connection from the service that collects it (<see cref="ConnectionRemoveMessage"/>): it's
    /// removed from ServiceConfig.json and collection stops.  By default the extended events sessions DBA Dash created on
    /// the instance are dropped and the instance is marked deleted in the repository (it can be restored from the recycle
    /// bin).  Needs the ManageConnections role, and a service that allows connection changes.
    /// </summary>
    internal sealed class RemoveConnectionDialog : Form
    {
        private readonly DBADashContext context;
        private readonly CheckBox chkDropEventSessions = new() { Text = "Drop the extended events sessions DBA Dash created on the instance", AutoSize = true, Checked = true, Location = new Point(12, 92) };
        private readonly CheckBox chkMarkDeleted = new() { Text = "Mark the instance deleted in the repository (restore it from the recycle bin)", AutoSize = true, Checked = true, Location = new Point(12, 124) };
        private readonly Label lblStatus = new() { Location = new Point(12, 164), Size = new Size(470, 44) };
        private readonly Button bttnRemove = new() { Text = "Remove", Location = new Point(490, 166), Size = new Size(110, 36) };
        private readonly Button bttnCancel = new() { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(608, 166), Size = new Size(110, 36) };

        /// <summary>The instance was marked deleted, so its node should be removed from the tree</summary>
        public bool MarkedDeleted { get; private set; }

        private RemoveConnectionDialog(DBADashContext context)
        {
            this.context = context;
            Text = "Remove Connection";
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(730, 216);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            ShowIcon = false;
            ShowInTaskbar = false;
            MinimizeBox = false;
            MaximizeBox = false;
            CancelButton = bttnCancel;

            var lblPrompt = new Label
            {
                Location = new Point(12, 12),
                Size = new Size(706, 70),
                Text = $"Remove {context.InstanceName} ({context.ConnectionID}) from {context.CollectAgent.AgentServiceName} on {context.CollectAgent.AgentHostName}?  " +
                       "It's removed from the service's config (a backup is kept) and collection stops."
            };
            Controls.AddRange(new Control[] { lblPrompt, chkDropEventSessions, chkMarkDeleted, lblStatus, bttnRemove, bttnCancel });
            bttnRemove.Click += async (_, _) => await RemoveAsync();
            Load += (_, _) => this.ApplyTheme();
        }

        /// <summary>
        /// Asks to remove the instance's connection and removes it.  Returns true if the instance was marked deleted, so its
        /// node should be removed from the tree.
        /// </summary>
        public static bool Show(IWin32Window owner, DBADashContext context)
        {
            var agent = context.CollectAgent;
            if (agent is not { MessagingEnabled: true, ConnectionChangesEnabled: true } || context.ImportAgent is not { MessagingEnabled: true })
            {
                MessageBox.Show(owner,
                    $"The service that collects {context.InstanceName} doesn't accept connection changes from the GUI.  Enable messaging and \"Allow Connection Changes\" in its service configuration tool, or remove the connection there.",
                    "Remove Connection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            using var frm = new RemoveConnectionDialog(context);
            frm.ShowDialog(owner);
            return frm.MarkedDeleted;
        }

        private void SetStatus(string message, string details, Color color)
        {
            lblStatus.Text = message;
            lblStatus.ForeColor = color;
        }

        private async Task RemoveAsync()
        {
            bttnRemove.Enabled = false;
            UseWaitCursor = true;
            try
            {
                var message = new ConnectionRemoveMessage
                {
                    ConnectionIDs = new List<string> { context.ConnectionID },
                    DropEventSessions = chkDropEventSessions.Checked,
                    CollectAgent = context.CollectAgent,
                    ImportAgent = context.ImportAgent
                };
                var outcome = await UserActionLog.SendLoggedAsync(message, (int)context.ImportAgentID!, (int)context.CollectAgentID!,
                    new[] { context.InstanceID }, UserActionLog.ActionRemoveConnections,
                    new { context.ConnectionID, message.DropEventSessions, MarkDeleted = chkMarkDeleted.Checked }, SetStatus);
                if (!outcome.Succeeded)
                {
                    SetStatus("Removing the connection failed: " + outcome.Error, null, DashColors.Fail);
                    return;
                }

                var results = ConnectionChanges.FromDataSet<List<ConnectionChangeResult>>(outcome.Data) ?? new List<ConnectionChangeResult>();
                var removed = results.Any(r => r.Outcome == ConnectionChangeResult.Removed);
                if (removed && chkMarkDeleted.Checked)
                {
                    SharedData.SetInstanceActive(context.InstanceID, null, false, Common.ConnectionString);
                    MarkedDeleted = true;
                }

                var sb = new StringBuilder();
                foreach (var result in results)
                {
                    sb.Append($"{result.ConnectionID}: {result.Outcome}");
                    if (!string.IsNullOrEmpty(result.Detail)) sb.Append($" - {result.Detail}");
                    sb.AppendLine();
                }
                if (MarkedDeleted) sb.AppendLine().AppendLine("Marked deleted in the repository.");
                foreach (var warning in outcome.Warnings)
                {
                    sb.AppendLine().AppendLine("Warning: " + warning);
                }
                MessageBox.Show(this, sb.ToString(), "Remove Connection", MessageBoxButtons.OK,
                    removed && outcome.Warnings.Count == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                if (removed) DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, ex.ToString(), DashColors.Fail);
            }
            finally
            {
                UseWaitCursor = false;
                bttnRemove.Enabled = true;
            }
        }
    }
}
