using DBADash;
using DBADash.Messaging;
using DBADashGUI.Messaging;
using DBADashGUI.Theme;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.Connections
{
    /// <summary>
    /// Adds source connections from the GUI.  The connections are sent to the chosen service, which checks them (connecting
    /// as the service account - this computer might not be able to reach the instances at all), shows what it found in
    /// <see cref="AddConnectionsReviewDialog"/>, then adds the ones chosen.  The service must allow connection changes
    /// (<see cref="CollectionConfig.AllowConnectionChanges"/>) and the user needs the ManageConnections role.  Connection
    /// strings are encrypted for the service (<see cref="MessageEncryption"/>).
    /// </summary>
    internal sealed class AddConnectionsDialog : Form
    {
        /// <summary>A service that accepts connection changes (dbo.ConnectionManagementAgents_Get)</summary>
        private sealed record AgentItem(int CollectAgentID, int ImportAgentID, string HostName, string ServiceName, string Version, int InstanceCount, bool HasPublicKey)
        {
            public string DisplayName => $"{ServiceName} on {HostName}";

            public override string ToString() => $"{DisplayName}  (version {Version}, {InstanceCount} instance{(InstanceCount == 1 ? "" : "s")})";
        }

        private readonly ComboBox cboService = new() { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(90, 12), Size = new Size(640, 28) };
        private readonly SourceConnectionOptionsTabs tabs = new() { Location = new Point(12, 52), Size = new Size(1080, 240), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        private readonly TextBox txtConnections = new() { Multiline = true, ScrollBars = ScrollBars.Both, WordWrap = false, Location = new Point(6, 36), Size = new Size(900, 130), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        private readonly Label lblStatus = new() { Location = new Point(12, 302), Size = new Size(820, 40), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        private readonly Button bttnAdd = new() { Text = "Check && Add...", Location = new Point(844, 302), Size = new Size(130, 36), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        private readonly Button bttnCancel = new() { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(982, 302), Size = new Size(110, 36), Anchor = AnchorStyles.Top | AnchorStyles.Right };

        /// <summary>Something was added or updated - the tree needs refreshing</summary>
        public bool ConnectionsChanged { get; private set; }

        public AddConnectionsDialog()
        {
            Text = "Add Connections";
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1104, 350);
            MinimumSize = new Size(900, 380);
            StartPosition = FormStartPosition.CenterParent;
            ShowIcon = false;
            ShowInTaskbar = false;
            MinimizeBox = false;
            CancelButton = bttnCancel;

            var tabConnections = new TabPage("Connections") { Padding = new Padding(3), UseVisualStyleBackColor = true, Size = new Size(1059, 178) };
            var lblConnections = new Label
            {
                Text = "Instance names or connection strings, one per line.  The service connects to them - this computer doesn't need access.",
                Location = new Point(6, 8),
                AutoSize = true
            };
            var bttnBuild = new Button { Text = "Build...", Location = new Point(914, 36), Size = new Size(110, 34), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            bttnBuild.Click += (_, _) => BuildConnectionString();
            tabConnections.Controls.AddRange(new Control[] { lblConnections, txtConnections, bttnBuild });
            tabs.TabPages.Insert(0, tabConnections);
            tabs.SelectedTab = tabConnections;

            Controls.AddRange(new Control[] { new Label { Text = "Service:", Location = new Point(12, 15), AutoSize = true }, cboService, tabs, lblStatus, bttnAdd, bttnCancel });

            bttnAdd.Click += async (_, _) => await CheckAndAddAsync();
            Load += (_, _) =>
            {
                this.ApplyTheme();
                LoadServices();
            };
        }

        private void LoadServices()
        {
            try
            {
                var agents = GetAgents();
                cboService.Items.AddRange(agents.Cast<object>().ToArray());
                if (agents.Count == 0)
                {
                    SetStatus("No DBA Dash service accepts connections from the GUI.  Enable messaging and \"Allow Connection Changes\" in the service configuration tool.", null, DashColors.Warning);
                    bttnAdd.Enabled = false;
                    return;
                }
                // Default to the service that collects the most instances
                cboService.SelectedItem = agents.MaxBy(a => a.InstanceCount);
                cboService.Enabled = agents.Count > 1;
            }
            catch (Exception ex)
            {
                SetStatus("Error getting the services: " + ex.Message, ex.ToString(), DashColors.Fail);
                bttnAdd.Enabled = false;
            }
        }

        private static List<AgentItem> GetAgents()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.ConnectionManagementAgents_Get", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
            using var rdr = cmd.ExecuteReader();
            var agents = new List<AgentItem>();
            while (rdr.Read())
            {
                agents.Add(new AgentItem((int)rdr["CollectAgentID"], (int)rdr["ImportAgentID"], (string)rdr["AgentHostName"],
                    (string)rdr["AgentServiceName"], (string)rdr["AgentVersion"], (int)rdr["InstanceCount"], (bool)rdr["HasPublicKey"]));
            }
            return agents;
        }

        private void BuildConnectionString()
        {
            using var frm = new DBConnection { SkipConnectionTest = true };
            if (frm.ShowDialog(this) != DialogResult.OK) return;
            var text = txtConnections.Text.TrimEnd();
            txtConnections.Text = (text.Length == 0 ? string.Empty : text + Environment.NewLine) + frm.ConnectionString;
        }

        private void SetStatus(string message, string details, Color color)
        {
            lblStatus.Text = message;
            lblStatus.ForeColor = color;
            lblStatus.Tag = details;
        }

        private List<string> ConnectionLines =>
            txtConnections.Text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                .Select(l => l.Trim())
                .Where(l => l.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

        private async Task CheckAndAddAsync()
        {
            if (cboService.SelectedItem is not AgentItem agent)
            {
                SetStatus("Select the service that will collect the connections", null, DashColors.Warning);
                return;
            }

            var lines = ConnectionLines;
            if (lines.Count == 0)
            {
                tabs.SelectedIndex = 0;
                SetStatus("Enter the instances to add", null, DashColors.Warning);
                return;
            }

            if (lines.Count > ConnectionChanges.MaxConnectionsPerMessage)
            {
                SetStatus($"At most {ConnectionChanges.MaxConnectionsPerMessage} connections can be added at once", null, DashColors.Warning);
                return;
            }

            if (!tabs.ValidateOptions())
            {
                return;
            }

            bttnAdd.Enabled = false;
            UseWaitCursor = true;
            try
            {
                await CheckAndAddAsync(agent, lines, tabs.Options);
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, ex.ToString(), DashColors.Fail);
            }
            finally
            {
                UseWaitCursor = false;
                bttnAdd.Enabled = true;
            }
        }

        private async Task CheckAndAddAsync(AgentItem agent, List<string> lines, SourceConnectionOptions options)
        {
            // Read the agents now rather than from the cache - the key changes every time the service starts
            var collectAgent = DBADashAgent.GetDBADashAgent(Common.ConnectionString, agent.CollectAgentID);
            var importAgent = agent.ImportAgentID == agent.CollectAgentID ? collectAgent : DBADashAgent.GetDBADashAgent(Common.ConnectionString, agent.ImportAgentID);
            if (!collectAgent.ConnectionChangesEnabled || string.IsNullOrEmpty(collectAgent.PublicKey))
            {
                SetStatus($"{agent.DisplayName} isn't accepting connection changes.  Check it's running a version that supports them, with \"Allow Connection Changes\" enabled.", null, DashColors.Fail);
                return;
            }

            var encrypted = lines.Select(l => MessageEncryption.Encrypt(l, collectAgent.PublicKey)).ToList();
            var test = new ConnectionTestMessage
            {
                ConnectionStrings = encrypted,
                Options = options,
                CollectAgent = collectAgent,
                ImportAgent = importAgent
            };
            var outcome = await MessagingHelper.SendAndWaitAsync(test, agent.ImportAgentID, SetStatus);
            if (!outcome.Succeeded)
            {
                SetStatus("Checking the connections failed: " + outcome.Error, null, DashColors.Fail);
                return;
            }

            var checks = ConnectionChanges.FromDataSet<List<SourceConnectionCheck>>(outcome.Data);
            if (checks == null || checks.Count != lines.Count)
            {
                SetStatus("The service didn't return a check for each connection", null, DashColors.Fail);
                return;
            }
            AddRepositoryStatus(checks, agent.CollectAgentID);
            SetStatus($"Checked {checks.Count} connection(s)", null, DashColors.Success);

            using var review = new AddConnectionsReviewDialog(checks, $"{agent.DisplayName}");
            if (review.ShowDialog(this) != DialogResult.OK) return;

            var requests = new List<ConnectionAddMessage.ConnectionRequest>();
            var requested = new List<SourceConnectionCheck>();
            for (var i = 0; i < checks.Count; i++)
            {
                if (review.Decisions[i] == null) continue;
                requests.Add(new ConnectionAddMessage.ConnectionRequest { ConnectionString = encrypted[i], Decisions = review.Decisions[i] });
                requested.Add(checks[i]);
            }
            if (requests.Count == 0) return;

            var add = new ConnectionAddMessage
            {
                Connections = requests,
                Options = options,
                CollectAgent = collectAgent,
                ImportAgent = importAgent
            };
            var details = new
            {
                Service = agent.DisplayName,
                Connections = requested.Select((c, i) => new
                {
                    c.ConnectionID,
                    Connection = c.ConnectionForPrint,
                    UpdateExisting = requests[i].Decisions.UpdateExisting ? true : (bool?)null,
                    AddUnvalidated = !c.Validated ? true : (bool?)null
                }),
                Options = JsonConvert.DeserializeObject(options.Serialize())
            };
            var addOutcome = await UserActionLog.SendLoggedAsync(add, agent.ImportAgentID, agent.CollectAgentID, null,
                UserActionLog.ActionAddConnections, details, SetStatus);
            if (!addOutcome.Succeeded)
            {
                SetStatus("Adding the connections failed: " + addOutcome.Error, null, DashColors.Fail);
                return;
            }

            var results = ConnectionChanges.FromDataSet<List<ConnectionChangeResult>>(addOutcome.Data) ?? new List<ConnectionChangeResult>();
            ConnectionsChanged = results.Any(r => r.IsChange);
            if (review.RestoreDeleted)
            {
                RestoreDeleted(results, requested);
            }

            ShowResults(results, addOutcome.Warnings);
            if (results.All(r => r.IsChange) && addOutcome.Warnings.Count == 0)
            {
                DialogResult = DialogResult.OK;
            }
        }

        /// <summary>What the repository knows about each instance: marked deleted, or collected by another service.</summary>
        private static void AddRepositoryStatus(List<SourceConnectionCheck> checks, int collectAgentID)
        {
            var ids = checks.Where(c => !string.IsNullOrEmpty(c.ConnectionID)).Select(c => c.ConnectionID).Distinct().ToList();
            if (ids.Count == 0) return;
            try
            {
                using var cn = new SqlConnection(Common.ConnectionString);
                using var cmd = new SqlCommand("dbo.InstanceConnectionStatus_Get", cn) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@ConnectionIDs", JsonConvert.SerializeObject(ids));
                cn.Open();
                using var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    var connectionID = (string)rdr["ConnectionID"];
                    foreach (var check in checks.Where(c => string.Equals(c.ConnectionID, connectionID, StringComparison.OrdinalIgnoreCase)))
                    {
                        var isActive = (bool)rdr["IsActive"];
                        check.IsDeletedInRepository = !isActive;
                        if (isActive && rdr["CollectAgentID"] is int agentID && agentID != collectAgentID && check.Existing == ExistingConnectionStates.None)
                        {
                            check.CollectedByOtherService = $"{rdr["AgentServiceName"]} on {rdr["AgentHostName"]}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Only for the notes - adding doesn't depend on it
                Serilog.Log.Warning(ex, "Error getting the repository status of new connections");
            }
        }

        private void RestoreDeleted(IEnumerable<ConnectionChangeResult> results, IReadOnlyCollection<SourceConnectionCheck> checks)
        {
            foreach (var result in results.Where(r => r.IsChange))
            {
                if (!checks.Any(c => c.IsDeletedInRepository && string.Equals(c.ConnectionID, result.ConnectionID, StringComparison.OrdinalIgnoreCase))) continue;
                try
                {
                    SharedData.SetInstanceActive(null, result.ConnectionID, true, Common.ConnectionString);
                }
                catch (Exception ex)
                {
                    SetStatus($"Error restoring {result.ConnectionID}: {ex.Message}", ex.ToString(), DashColors.Fail);
                }
            }
        }

        private void ShowResults(IReadOnlyCollection<ConnectionChangeResult> results, IReadOnlyCollection<string> warnings)
        {
            var sb = new StringBuilder();
            foreach (var result in results)
            {
                sb.Append($"{result.ConnectionID ?? result.ConnectionForPrint ?? "(unknown)"}: {result.Outcome}");
                if (!string.IsNullOrEmpty(result.Detail)) sb.Append($" - {result.Detail}");
                sb.AppendLine();
            }
            foreach (var warning in warnings)
            {
                sb.AppendLine().AppendLine("Warning: " + warning);
            }
            if (results.Any(r => r.IsChange))
            {
                sb.AppendLine().Append("New instances appear in the tree once their first collections have been imported - refresh the tree in a minute or so.");
            }

            var icon = warnings.Count > 0 || results.Any(r => r.Outcome == ConnectionChangeResult.Failed) ? MessageBoxIcon.Warning : MessageBoxIcon.Information;
            SetStatus($"{results.Count(r => r.IsChange)} connection(s) added or updated", null, icon == MessageBoxIcon.Warning ? DashColors.Warning : DashColors.Success);
            MessageBox.Show(this, sb.ToString(), "Add Connections", MessageBoxButtons.OK, icon);
        }
    }
}
