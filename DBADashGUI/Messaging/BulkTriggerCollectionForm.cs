using DBADash;
using DBADash.Messaging;
using DBADashGUI.CustomReports;
using DBADashGUI.Interface;
using DBADashGUI.Theme;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.Messaging
{
    /// <summary>
    /// Triggers one or more collections across many instances at once.  Instances are grouped by service
    /// (import &amp; collect agent) and sent as a single <see cref="MultiCollectionMessage"/> per group.
    /// Per-instance replies tick each instance off as it completes; the originating report is refreshed
    /// once when the dialog is closed rather than on every reply.
    /// </summary>
    public partial class BulkTriggerCollectionForm : Form
    {
        private readonly List<TriggerInstance> instances;
        private readonly IRefreshData refreshTarget;

        /// <summary>A single instance to trigger, with the collection types to run for it.</summary>
        private sealed record TriggerInstance(int InstanceID, string InstanceName, string ConnectionID,
            int ImportAgentID, DBADashAgent CollectAgent, DBADashAgent ImportAgent, List<string> CollectionTypes);

        private DataGridView grid;
        private DataTable gridData;
        private Label lblStatus;
        private ProgressBar progressBar;
        private Button btnClose;
        private Button btnCancel;

        private int completedCount;
        private volatile bool cancellationRequested;

        // The overall run (initial batch + any forced re-run).  Used on close to detect work still in flight.
        private Task runTask;

        // Instances skipped because the requested collection(s) aren't scheduled, mapped to the disabled
        // collection names.  Collected during the run so we can offer to re-run just those, forced, at the end.
        private readonly Dictionary<int, List<string>> disabledByInstance = new();
        private readonly object disabledLock = new();

        // The batches in flight, one per service, so we can target a cancellation at the running groups.
        private readonly List<ActiveGroup> activeGroups = new();

        /// <summary>A single service's batch (a <see cref="MultiCollectionMessage"/> conversation).</summary>
        private sealed class ActiveGroup
        {
            public Guid MessageGroup;
            public int ImportAgentID;
            public DBADashAgent CollectAgent;
            public DBADashAgent ImportAgent;
            public List<int> InstanceIDs;
            public bool Completed;

            /// <summary>Run disabled (unscheduled) collections anyway - set for the forced re-run pass.</summary>
            public bool Force;
        }

        private BulkTriggerCollectionForm(List<TriggerInstance> instances, IRefreshData refreshTarget)
        {
            this.instances = instances;
            this.refreshTarget = refreshTarget;
            InitializeComponentManual();
            this.ApplyTheme();
        }

        /// <summary>
        /// Triggers the same set of collection types across every supplied instance.  Resolves the
        /// messaging-enabled instances, confirms with the user and - if confirmed - shows the progress
        /// dialog.  Returns without showing anything if there are no eligible instances or the user cancels.
        /// </summary>
        public static void Trigger(IWin32Window owner, List<string> collectionTypes, IEnumerable<int> instanceIDs, IRefreshData refreshTarget)
        {
            if (collectionTypes == null || collectionTypes.Count == 0) return;
            var map = instanceIDs.Distinct().ToDictionary(id => id, _ => collectionTypes);
            var typesText = string.Join(", ", collectionTypes);
            Trigger(owner, map, refreshTarget,
                (_, instanceCount) => $"Trigger {collectionTypes.Count} collection(s) ({typesText}) on {instanceCount} instance(s)?");
        }

        /// <summary>
        /// Triggers a per-instance set of collection types (the collections may differ from one instance to
        /// the next, e.g. the Collection Dates view).  The key is the instance id; the value is the list of
        /// collection types to run for that instance.
        /// </summary>
        public static void Trigger(IWin32Window owner, Dictionary<int, List<string>> instanceCollectionTypes, IRefreshData refreshTarget)
        {
            Trigger(owner, instanceCollectionTypes, refreshTarget,
                (collectionCount, instanceCount) => $"Trigger {collectionCount} collection(s) across {instanceCount} instance(s)?");
        }

        private static void Trigger(IWin32Window owner, Dictionary<int, List<string>> instanceCollectionTypes,
            IRefreshData refreshTarget, Func<int, int, string> confirmationFactory)
        {
            if (instanceCollectionTypes == null || instanceCollectionTypes.Count == 0) return;

            var eligible = new List<TriggerInstance>();
            foreach (var (id, types) in instanceCollectionTypes)
            {
                if (types == null || types.Count == 0) continue;
                DBADashContext ctx;
                try
                {
                    ctx = CommonData.GetDBADashContext(id);
                }
                catch
                {
                    continue;
                }
                if (ctx is not { CanMessage: true } || ctx.ImportAgentID == null || ctx.CollectAgent == null) continue;

                var row = CommonData.Instances.AsEnumerable().FirstOrDefault(r => r.Field<int>("InstanceID") == id);

                // InstanceDisplayName (Alias or ConnectionID) is always populated, unlike the raw Instance
                // name which can be blank, so prefer it for the base name.
                var baseName = row?.Field<string>("InstanceDisplayName");
                if (string.IsNullOrEmpty(baseName))
                    baseName = string.IsNullOrEmpty(ctx.InstanceName) ? ctx.ConnectionID : ctx.InstanceName;

                // Azure databases share the instance/group name across a row per database, so include the
                // database name to disambiguate them.
                var azureDBName = row?.Field<string>("AzureDBName");
                var displayName = string.IsNullOrEmpty(azureDBName) ? baseName : $"{baseName} / {azureDBName}";

                eligible.Add(new TriggerInstance(id, displayName, ctx.ConnectionID, ctx.ImportAgentID.Value,
                    ctx.CollectAgent, ctx.ImportAgent, types));
            }

            if (eligible.Count == 0)
            {
                MessageBox.Show("None of the instances in the current context have messaging enabled.", "Trigger Collection",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var collectionCount = eligible.Sum(i => i.CollectionTypes.Count);
            if (MessageBox.Show(confirmationFactory(collectionCount, eligible.Count),
                    "Trigger Collection", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            var frm = new BulkTriggerCollectionForm(eligible, refreshTarget);
            frm.Show(owner);
        }

        private void InitializeComponentManual()
        {
            Text = "Trigger Collection";
            Width = 700;
            Height = 450;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = true;
            ShowIcon = false;

            gridData = new DataTable();
            gridData.Columns.Add("InstanceID", typeof(int));
            gridData.Columns.Add("Instance", typeof(string));
            gridData.Columns.Add("Collections", typeof(string));
            gridData.Columns.Add("Status", typeof(string));
            gridData.Columns.Add("Detail", typeof(string));

            foreach (var instance in instances.OrderBy(i => i.InstanceName))
            {
                gridData.Rows.Add(instance.InstanceID, instance.InstanceName,
                    string.Join(", ", instance.CollectionTypes), "Pending", string.Empty);
            }

            grid = new DBADashDataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false
            };
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Instance", HeaderText = "Instance", FillWeight = 35 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Collections", HeaderText = "Collections", FillWeight = 25 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "Status", FillWeight = 15 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Detail", HeaderText = "Detail", FillWeight = 25 });
            grid.CellFormatting += Grid_CellFormatting;
            grid.DataSource = new DataView(gridData) { AllowNew = false };

            // Status row: label (left, fills) and progress bar (right) sit side-by-side directly under the
            // grid.  The right column is the same width as the button area so the progress bar aligns.
            var statusRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            statusRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            statusRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 192F));
            lblStatus = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true, Padding = new Padding(6, 0, 6, 0) };
            progressBar = new ProgressBar { Dock = DockStyle.Fill, Maximum = Math.Max(1, instances.Count), Value = 0, Margin = new Padding(0, 2, 6, 2) };
            statusRow.Controls.Add(lblStatus, 0, 0);
            statusRow.Controls.Add(progressBar, 1, 0);

            // Button row: Close rightmost, Cancel to its left.  Padding gives 28px of content height
            // (exactly the button height) with 10px of breathing room at the bottom of the form.
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 6, 6, 10),
                WrapContents = false
            };
            btnClose = new Button { Text = "Close", Width = 90, Height = 28, Margin = new Padding(8, 0, 0, 0) };
            btnClose.Click += (_, _) => Close();
            btnCancel = new Button { Text = "Cancel", Width = 90, Height = 28, Enabled = false, Margin = new Padding(0, 0, 0, 0) };
            btnCancel.Click += BtnCancel_Click;
            buttonPanel.Controls.Add(btnClose);
            buttonPanel.Controls.Add(btnCancel);

            // Outer layout: grid fills, status row and button row are fixed height.
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            layout.Controls.Add(grid, 0, 0);
            layout.Controls.Add(statusRow, 0, 1);
            layout.Controls.Add(buttonPanel, 0, 2);

            Controls.Add(layout);

            Shown += (_, _) => runTask = StartAsync();
            FormClosing += Form_FormClosing;
            FormClosed += (_, _) => refreshTarget?.RefreshData();
        }

        /// <summary>
        /// When the user closes the dialog while collections are still in flight, asks what they want to do:
        /// keep waiting, close and let the collections finish in the background, or close and cancel the
        /// remaining collections.  The collections run on the service regardless of this dialog, so closing
        /// in the background is safe - the per-instance UI callbacks become no-ops once the form is disposed.
        /// </summary>
        private void Form_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Nothing in flight (or the run never started) - allow the close.
            if (runTask is not { IsCompleted: false }) return;

            var runInBackground = new TaskDialogButton("Close && let collections finish in the background");
            var cancelCollections = new TaskDialogButton("Close && cancel the remaining collections");
            var keepOpen = new TaskDialogButton("Keep this window open");

            var page = new TaskDialogPage
            {
                Caption = "Trigger Collection",
                Heading = "Collections are still running",
                Text = "One or more collections haven't finished yet.  What would you like to do?",
                Icon = TaskDialogIcon.Warning,
                AllowCancel = true,
                Buttons = { runInBackground, cancelCollections, keepOpen },
                DefaultButton = keepOpen
            };

            var result = TaskDialog.ShowDialog(this, page);
            if (result == runInBackground)
            {
                // Allow the close.  The batch keeps running on the service; its UI callbacks no-op once disposed.
            }
            else if (result == cancelCollections)
            {
                // Allow the close and fire the cancellation - we don't wait for it as the window is going away.
                _ = RequestCancellationAsync();
            }
            else
            {
                // Keep open, or the dialog was dismissed (Esc / the X) - cancel the close.
                e.Cancel = true;
            }
        }

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (grid.Columns[e.ColumnIndex].DataPropertyName != "Status") return;
            var status = Convert.ToString(e.Value);
            e.CellStyle.ForeColor = status switch
            {
                "Success" => DashColors.Success,
                "Failed" => DashColors.Fail,
                "Warning" => DashColors.Warning,
                "Cancelled" => DashColors.Warning,
                "Pending" => DashColors.Information,
                _ => e.CellStyle.ForeColor
            };
        }

        private async Task StartAsync()
        {
            await RunBatchAsync(instances, force: false);

            // Anything skipped because it isn't scheduled can be re-run, forced, if the user confirms.
            await OfferForcedRerunAsync();
        }

        /// <summary>
        /// Groups the supplied instances by service and sends a <see cref="MultiCollectionMessage"/> per group,
        /// tracking per-instance progress.  When <paramref name="force"/> is set, collections that aren't
        /// scheduled are run anyway rather than skipped.
        /// </summary>
        private async Task RunBatchAsync(List<TriggerInstance> batchInstances, bool force)
        {
            // Group instances by service (import agent + collect agent) so each service gets a single message.
            var groups = batchInstances
                .GroupBy(i => (i.ImportAgentID, i.CollectAgent.AgentIdentifier))
                .ToList();

            activeGroups.Clear();
            foreach (var group in groups)
            {
                var groupInstanceList = group.ToList();
                var first = groupInstanceList[0];
                activeGroups.Add(new ActiveGroup
                {
                    MessageGroup = Guid.NewGuid(),
                    ImportAgentID = first.ImportAgentID,
                    CollectAgent = first.CollectAgent,
                    ImportAgent = first.ImportAgent,
                    InstanceIDs = groupInstanceList.Select(i => i.InstanceID).ToList(),
                    Force = force
                });
            }

            UpdateSummary();
            SetCancelEnabled(true);

            try
            {
                await Task.WhenAll(activeGroups.Select(g => SendGroupAsync(g, batchInstances)));
            }
            catch (Exception ex)
            {
                SetStatus("Error: " + ex.Message, DashColors.Fail);
            }
            finally
            {
                SetCancelEnabled(false);
                HideProgressBar();
                UpdateSummary();
            }
        }

        /// <summary>
        /// If any collections were skipped because they aren't scheduled, prompts the user to run them anyway
        /// and - if confirmed - re-runs just those collections on just those instances with the disabled
        /// schedule ignored.
        /// </summary>
        private async Task OfferForcedRerunAsync()
        {
            if (cancellationRequested) return;

            Dictionary<int, List<string>> disabled;
            lock (disabledLock)
            {
                disabled = new Dictionary<int, List<string>>(disabledByInstance);
            }
            if (disabled.Count == 0) return;

            // Re-run each affected instance with only the collections that were skipped.
            var instanceLookup = instances.ToDictionary(i => i.InstanceID);
            var rerun = disabled
                .Where(kv => kv.Value is { Count: > 0 } && instanceLookup.ContainsKey(kv.Key))
                .Select(kv => instanceLookup[kv.Key] with { CollectionTypes = kv.Value })
                .ToList();
            if (rerun.Count == 0) return;

            var distinctTypes = string.Join(", ", rerun.SelectMany(i => i.CollectionTypes).Distinct());
            if (MessageBox.Show(
                    $"{rerun.Count} instance(s) had collection(s) that aren't scheduled ({distinctTypes}), so they were skipped.\n\nRun them anyway?",
                    "Collections Not Scheduled", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            lock (disabledLock)
            {
                disabledByInstance.Clear();
            }
            ResetInstancesToPending(rerun.Select(i => i.InstanceID));
            await RunBatchAsync(rerun, force: true);
        }

        private async Task SendGroupAsync(ActiveGroup group, List<TriggerInstance> batchInstances)
        {
            // Allow enough lifetime for the whole batch - each instance is collected sequentially.
            var lifetime = Math.Min(7200, 300 + (group.InstanceIDs.Count * 120));

            var instanceLookup = batchInstances.ToDictionary(i => i.InstanceID);
            var collectionInstances = group.InstanceIDs.Select(id => new CollectionInstance
            {
                InstanceID = id,
                ConnectionID = instanceLookup[id].ConnectionID,
                CollectionTypes = instanceLookup[id].CollectionTypes
            }).ToList();
            // Collection types are carried per-instance (they may differ); the message-level list is the
            // distinct union, used only for logging on the service side.
            var unionTypes = collectionInstances.SelectMany(i => i.CollectionTypes).Distinct().ToList();
            var message = new MultiCollectionMessage(unionTypes, collectionInstances)
            {
                CollectAgent = group.CollectAgent,
                ImportAgent = group.ImportAgent,
                Lifetime = lifetime,
                IgnoreDisabledSchedule = group.Force
            };

            // Per-instance status is shown in the grid and summarised in the status bar, so the raw service
            // messages are ignored here - we only capture a send-level error to report on the instances.
            string sendError = null;
            try
            {
                await MessagingHelper.SendMessageAndProcessReply(message, group.ImportAgentID,
                    (msg, _, color) => { if (color == DashColors.Fail) sendError = msg; },
                    ProcessCompleted, group.MessageGroup, OnProgress);
            }
            finally
            {
                group.Completed = true;
                // Safety net: the conversation ended; mark any instance in this group that never reported a
                // result (e.g. the message failed to send) so nothing is left stuck on "Pending".
                var status = cancellationRequested ? "Cancelled" : "Failed";
                foreach (var id in group.InstanceIDs)
                {
                    MarkPending(id, status, sendError ?? "No response from service");
                }
            }
        }

        private async void BtnCancel_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Cancel the remaining collections?  Instances already in progress may still complete.",
                    "Cancel Collection", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }
            await RequestCancellationAsync();
        }

        /// <summary>
        /// Flags the run as cancelled and sends a <see cref="CancellationMessage"/> to each service still
        /// running a batch.  Safe to call as the form is closing - the status updates become no-ops once the
        /// form is disposed (see <see cref="InvokeOnUI"/>).
        /// </summary>
        private async Task RequestCancellationAsync()
        {
            cancellationRequested = true;
            SetCancelEnabled(false);
            SetStatus("Cancellation requested...", DashColors.Warning);

            var running = activeGroups.Where(g => !g.Completed).ToList();
            try
            {
                await Task.WhenAll(running.Select(CancelGroupAsync));
            }
            catch (Exception ex)
            {
                SetStatus("Error requesting cancellation: " + ex.Message, DashColors.Fail);
            }
        }

        private static async Task CancelGroupAsync(ActiveGroup group)
        {
            var message = new CancellationMessage
            {
                CancelMessageId = group.MessageGroup,
                CollectAgent = group.CollectAgent,
                ImportAgent = group.ImportAgent,
                Lifetime = 60
            };
            // Fire the cancellation at the same service running the batch.  Reply is ignored - the batch's
            // own conversation reports the cancelled instances.
            await MessagingHelper.SendMessageAndProcessReply(message, group.ImportAgentID,
                (_, _, _) => { }, (_, _, _) => Task.CompletedTask, Guid.NewGuid());
        }

        /// <summary>
        /// Marshals <paramref name="action"/> onto the UI thread, or runs it inline when already there.
        /// No-op (rather than throwing) once the form has been closed - per-instance replies can still arrive
        /// after the user closes the dialog and lets the collections run on in the background.
        /// </summary>
        private void InvokeOnUI(Action action)
        {
            if (IsDisposed || Disposing || !IsHandleCreated) return;
            try
            {
                if (InvokeRequired)
                {
                    BeginInvoke(action);
                }
                else
                {
                    action();
                }
            }
            catch (Exception ex) when (ex is ObjectDisposedException or InvalidOperationException)
            {
                // The form was closed between the guard above and the invoke - the collections run on regardless.
            }
        }

        private void SetCancelEnabled(bool enabled) => InvokeOnUI(() =>
            // Don't re-enable once a cancellation has been requested.
            btnCancel.Enabled = enabled && !cancellationRequested);

        private Task OnProgress(ResponseMessage reply, Guid messageGroup)
        {
            var progress = reply.CollectionProgress;
            if (progress != null)
            {
                // Remember instances skipped because their collection(s) aren't scheduled so we can offer to
                // re-run just those, forced, once the batch finishes.
                if (progress.Status == "Warning" && progress.DisabledCollections is { Count: > 0 })
                {
                    lock (disabledLock)
                    {
                        disabledByInstance[progress.InstanceID] = progress.DisabledCollections;
                    }
                }
                UpdateInstanceStatus(progress.InstanceID, progress.Status, progress.Detail);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Resets the given instances' grid rows back to "Pending" ahead of a forced re-run, adjusting the
        /// completed count and re-showing the progress bar.  Runs on the UI thread.
        /// </summary>
        private void ResetInstancesToPending(IEnumerable<int> instanceIDs) => InvokeOnUI(() =>
        {
            var ids = instanceIDs.ToHashSet();
            foreach (var row in gridData.AsEnumerable().Where(r => ids.Contains(r.Field<int>("InstanceID"))))
            {
                if (Convert.ToString(row["Status"]) == "Pending") continue;
                row["Status"] = "Pending";
                row["Detail"] = string.Empty;
                if (completedCount > 0) completedCount--;
            }
            progressBar.Visible = true;
            progressBar.Value = Math.Min(progressBar.Maximum, Math.Max(0, completedCount));
            UpdateSummary();
        });

        private Task ProcessCompleted(ResponseMessage reply, Guid messageGroup, MessagingHelper.SetStatusDelegate setStatus)
        {
            if (reply.Type == ResponseMessage.ResponseTypes.Failure)
            {
                var group = activeGroups.FirstOrDefault(g => g.MessageGroup == messageGroup);
                if (group != null)
                {
                    // The whole conversation ended without a per-instance result (e.g. cancelled, service
                    // busy, message expired) - mark any instances in this group that haven't reported yet.
                    var status = cancellationRequested ? "Cancelled" : "Failed";
                    foreach (var id in group.InstanceIDs)
                    {
                        MarkPending(id, status, reply.Message);
                    }
                }
            }
            return Task.CompletedTask;
        }

        private void UpdateInstanceStatus(int instanceID, string status, string detail) => InvokeOnUI(() =>
        {
            var row = gridData.AsEnumerable().FirstOrDefault(r => r.Field<int>("InstanceID") == instanceID);
            if (row == null) return;
            var wasPending = Convert.ToString(row["Status"]) == "Pending";
            row["Status"] = status;
            row["Detail"] = detail ?? string.Empty;

            if (wasPending)
            {
                completedCount++;
                progressBar.Value = Math.Min(progressBar.Maximum, completedCount);
            }

            UpdateSummary();

            if (completedCount >= instances.Count)
            {
                HideProgressBar();
            }
        });

        /// <summary>
        /// Summarises the batch in the status bar (pending / succeeded / failed counts).  The per-instance
        /// detail lives in the grid, so the status bar tracks totals rather than the last message.
        /// </summary>
        private void UpdateSummary() => InvokeOnUI(() =>
        {
            var rows = gridData.AsEnumerable().ToList();
            var pending = rows.Count(r => Convert.ToString(r["Status"]) == "Pending");
            var succeeded = rows.Count(r => Convert.ToString(r["Status"]) == "Success");
            var failed = rows.Count(r => Convert.ToString(r["Status"]) == "Failed");
            var warning = rows.Count(r => Convert.ToString(r["Status"]) == "Warning");
            var cancelled = rows.Count(r => Convert.ToString(r["Status"]) == "Cancelled");

            var text = $"Succeeded: {succeeded}   Failed: {failed}";
            if (warning > 0) text += $"   Warning: {warning}";
            if (cancelled > 0) text += $"   Cancelled: {cancelled}";
            text += $"   Pending: {pending}";

            var done = pending == 0;
            Color color;
            if (!done)
            {
                // In progress - flag amber if failures/cancellations have already appeared, otherwise blue.
                color = (failed > 0 || cancelled > 0) ? DashColors.Warning : DashColors.Information;
            }
            else if (failed == 0 && cancelled == 0 && warning == 0)
            {
                color = DashColors.Success; // everything succeeded
            }
            else if (succeeded > 0 || warning > 0 || cancelled > 0)
            {
                color = DashColors.Warning; // partial - some succeeded / skipped / cancelled, but not all failed
            }
            else
            {
                color = DashColors.Fail; // everything failed
            }

            SetStatus((done ? "Completed.   " : string.Empty) + text, color);
        });

        private void HideProgressBar() => InvokeOnUI(() => progressBar.Visible = false);

        private void MarkPending(int instanceID, string status, string detail) => InvokeOnUI(() =>
        {
            var row = gridData.AsEnumerable().FirstOrDefault(r => r.Field<int>("InstanceID") == instanceID);
            if (row == null || Convert.ToString(row["Status"]) != "Pending") return;
            UpdateInstanceStatus(instanceID, status, detail);
        });

        private void SetStatus(string message, Color color) => InvokeOnUI(() =>
        {
            lblStatus.Text = message;
            lblStatus.ForeColor = color;
        });
    }
}