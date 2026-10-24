using DBADash;
using DBADash.Messaging;
using DBADashGUI.Messaging;
using DBADashGUI.Theme;
using Newtonsoft.Json;
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
    /// Changes an instance's connection options - slow query capture, running query plans and live plans, deadlocks, query
    /// stats... - on the service that collects it.  The current options are read from the service
    /// (<see cref="ConnectionOptionsGetMessage"/>) and the change applied without a restart
    /// (<see cref="ConnectionOptionsUpdateMessage"/>).  Needs the ManageConnections role, and a service that allows
    /// connection changes.
    /// </summary>
    internal sealed class EditConnectionOptionsDialog : Form
    {
        private readonly DBADashContext context;
        private readonly SourceConnectionOptionsTabs tabs = new() { Location = new Point(12, 52), Size = new Size(1080, 240), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, Enabled = false };
        private readonly Label lblInfo = new() { Location = new Point(12, 10), Size = new Size(1080, 40), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        private readonly CheckBox chkDefaultDeadlockSchedule = new()
        {
            Text = "Use the default deadlock collection schedule (every 5 minutes) if deadlock collection has no schedule",
            AutoSize = true,
            Checked = true,
            Location = new Point(12, 298)
        };
        private readonly Label lblStatus = new() { Location = new Point(12, 330), Size = new Size(820, 40), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        private readonly Button bttnSave = new() { Text = "Save", Location = new Point(844, 330), Size = new Size(130, 36), Anchor = AnchorStyles.Top | AnchorStyles.Right, Enabled = false };
        private readonly Button bttnCancel = new() { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(982, 330), Size = new Size(110, 36), Anchor = AnchorStyles.Top | AnchorStyles.Right };

        private EditConnectionOptionsDialog(DBADashContext context)
        {
            this.context = context;
            Text = $"Connection Options - {context.InstanceName}";
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1104, 378);
            MinimumSize = new Size(900, 410);
            StartPosition = FormStartPosition.CenterParent;
            ShowIcon = false;
            ShowInTaskbar = false;
            MinimizeBox = false;
            CancelButton = bttnCancel;
            lblInfo.Text = $"{context.InstanceName} ({context.ConnectionID}), collected by {context.CollectAgent.AgentServiceName} on {context.CollectAgent.AgentHostName}.  Changes apply without a service restart.";

            Controls.AddRange(new Control[] { lblInfo, tabs, chkDefaultDeadlockSchedule, lblStatus, bttnSave, bttnCancel });
            bttnSave.Click += async (_, _) => await SaveAsync();
            Load += async (_, _) =>
            {
                this.ApplyTheme();
                await LoadOptionsAsync();
            };
        }

        /// <summary>Shows the instance's connection options for editing.</summary>
        public static void Show(IWin32Window owner, DBADashContext context)
        {
            if (context.CollectAgent is not { MessagingEnabled: true, ConnectionChangesEnabled: true } || context.ImportAgent is not { MessagingEnabled: true })
            {
                MessageBox.Show(owner,
                    $"The service that collects {context.InstanceName} doesn't accept connection changes from the GUI.  Enable messaging and \"Allow Connection Changes\" in its service configuration tool, or change the connection there.",
                    "Connection Options", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var frm = new EditConnectionOptionsDialog(context);
            frm.ShowDialog(owner);
        }

        private void SetStatus(string message, string details, Color color)
        {
            lblStatus.Text = message;
            lblStatus.ForeColor = color;
        }

        private async Task LoadOptionsAsync()
        {
            UseWaitCursor = true;
            try
            {
                var message = new ConnectionOptionsGetMessage
                {
                    ConnectionID = context.ConnectionID,
                    CollectAgent = context.CollectAgent,
                    ImportAgent = context.ImportAgent
                };
                var outcome = await MessagingHelper.SendAndWaitAsync(message, (int)context.ImportAgentID!, SetStatus);
                if (!outcome.Succeeded)
                {
                    SetStatus("Error getting the connection options: " + outcome.Error, null, DashColors.Fail);
                    return;
                }

                var info = ConnectionChanges.FromDataSet<ConnectionOptionsGetMessage.ConnectionOptionsInfo>(outcome.Data);
                if (info is not { IsInConfig: true })
                {
                    SetStatus($"{context.ConnectionID} isn't in the service's config - it's monitored automatically, via the \"Scan for AzureDBs\" or \"Monitor read replicas\" option.  Change the connection it was found from.", null, DashColors.Warning);
                    return;
                }

                tabs.Options = info.Options;
                tabs.SetSqlOptionsEnabled(true);
                tabs.Enabled = true;
                bttnSave.Enabled = true;
                SetStatus(info.CustomCollectionCount > 0
                    ? $"{info.CustomCollectionCount} custom collection(s) are kept as they are - they can only be changed in the service configuration tool."
                    : string.Empty, null, DashColors.Information);
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, ex.ToString(), DashColors.Fail);
            }
            finally
            {
                UseWaitCursor = false;
            }
        }

        private async Task SaveAsync()
        {
            if (!tabs.ValidateOptions()) return;
            bttnSave.Enabled = false;
            UseWaitCursor = true;
            try
            {
                var options = tabs.Options;
                var message = new ConnectionOptionsUpdateMessage
                {
                    ConnectionIDs = new List<string> { context.ConnectionID },
                    Options = options,
                    UseDefaultDeadlockSchedule = chkDefaultDeadlockSchedule.Checked,
                    CollectAgent = context.CollectAgent,
                    ImportAgent = context.ImportAgent
                };
                var outcome = await UserActionLog.SendLoggedAsync(message, (int)context.ImportAgentID!, (int)context.CollectAgentID!,
                    new[] { context.InstanceID }, UserActionLog.ActionEditConnection,
                    new { context.ConnectionID, Options = JsonConvert.DeserializeObject(options.Serialize()) }, SetStatus);
                if (!outcome.Succeeded)
                {
                    SetStatus("Saving the options failed: " + outcome.Error, null, DashColors.Fail);
                    return;
                }

                var results = ConnectionChanges.FromDataSet<List<ConnectionChangeResult>>(outcome.Data) ?? new List<ConnectionChangeResult>();
                var updated = results.Any(r => r.Outcome == ConnectionChangeResult.Updated);
                var sb = new StringBuilder();
                foreach (var result in results)
                {
                    sb.Append($"{result.ConnectionID}: {result.Outcome}");
                    if (!string.IsNullOrEmpty(result.Detail)) sb.Append($" - {result.Detail}");
                    sb.AppendLine();
                }
                foreach (var warning in outcome.Warnings)
                {
                    sb.AppendLine().AppendLine("Warning: " + warning);
                }
                MessageBox.Show(this, sb.ToString(), "Connection Options", MessageBoxButtons.OK,
                    updated && outcome.Warnings.Count == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                if (updated) DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, ex.ToString(), DashColors.Fail);
            }
            finally
            {
                UseWaitCursor = false;
                bttnSave.Enabled = true;
            }
        }
    }
}
