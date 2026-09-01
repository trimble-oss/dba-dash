using DBADash.Messaging;
using DBADashGUI.Messaging;
using Microsoft.Data.SqlClient;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DBADashGUI.XETrace
{
    /// <summary>
    /// GUI-side orchestration for managing the <b>existing</b> XE sessions on a monitored instance: list them,
    /// start/stop one, and watch one live.  Sends the corresponding messages via
    /// <see cref="MessagingHelper.SendMessageAndProcessReply"/> and returns the results.  Nothing is persisted to the
    /// repo (watching is transient viewing) - unlike the ad-hoc XE trace (<see cref="XETraceController"/>).
    /// </summary>
    internal static class XESessionController
    {
        /// <summary>Outcome of a watch run so the caller can give clear feedback.</summary>
        public sealed record XEWatchOutcome(bool Ok, bool Cancelled, string Message);

        /// <summary>
        /// Result of a one-shot "view existing target data" read.  <paramref name="Events"/> is the shredded batch to
        /// display; <paramref name="Capped"/> is true when more events exist than the <paramref name="MaxEvents"/> cap.
        /// </summary>
        public sealed record XEViewDataOutcome(bool Ok, DataTable Events, string TargetType, int TotalEvents,
            bool Capped, int MaxEvents, string Message);

        /// <summary>
        /// Outcome of a start/stop request.  <paramref name="Ok"/> is true only when the service replied Success;
        /// <paramref name="Running"/> is the session's actual running state afterwards (null if unknown), so the caller
        /// can confirm the operation really took effect rather than assuming success.
        /// </summary>
        public sealed record XEControlOutcome(bool Ok, bool? Running, string Message);

        /// <summary>
        /// Outcome of a session-list request.  <paramref name="Ok"/> is true only when the service replied Success;
        /// <paramref name="Sessions"/> carries the rows in that case, and <paramref name="Message"/> the reason it
        /// couldn't be listed otherwise (e.g. the service has viewing disabled) so the caller can show it instead of a
        /// generic "no response".
        /// </summary>
        public sealed record XEListOutcome(bool Ok, DataTable Sessions, string Message);

        /// <summary>Lists the instance's existing XE sessions (name, running state, targets, event count).</summary>
        public static async Task<XEListOutcome> ListSessionsAsync(DBADashContext context,
            MessagingHelper.SetStatusDelegate setStatus, CancellationToken cancellationToken = default)
        {
            if (context.ImportAgentID == null)
            {
                const string noAgent = "No Import Agent is configured for this instance, so the request can't be sent.";
                setStatus("No Import Agent", string.Empty, DashColors.Fail);
                return new XEListOutcome(false, null, noAgent);
            }
            var message = new XESessionListMessage
            {
                ConnectionID = context.ConnectionID,
                CollectAgent = context.CollectAgent,
                ImportAgent = context.ImportAgent,
                Lifetime = 60
            };

            DataTable result = null;
            var terminal = false;
            var ok = false;
            string outcomeMessage = null;
            await MessagingHelper.SendMessageAndProcessReply(message, (int)context.ImportAgentID, setStatus,
                (reply, group, status) =>
                {
                    switch (reply.Type)
                    {
                        case ResponseMessage.ResponseTypes.Success:
                            terminal = true;
                            ok = true;
                            if (reply.Data?.Tables.Count > 0) result = reply.Data.Tables[0];
                            break;

                        case ResponseMessage.ResponseTypes.Failure:
                        case ResponseMessage.ResponseTypes.Warning:
                            terminal = true;
                            ok = false;
                            outcomeMessage = reply.Message;
                            break;
                    }
                    return Task.CompletedTask;
                }, Guid.NewGuid(), cancellationToken: cancellationToken);

            if (!terminal)
            {
                outcomeMessage = "The request ended without a result from the service.  The service may be running an " +
                                 "older version, extended events may be disabled on it, or it may not be running.";
                return new XEListOutcome(false, null, outcomeMessage);
            }
            return new XEListOutcome(ok, result, outcomeMessage);
        }

        /// <summary>
        /// Per-instance outcome of a multi-instance session list (see <see cref="ListSessionsMultiAsync"/>), so the view
        /// can show a footer of what did / didn't respond.  We never message an instance that is <paramref name="Offline"/>
        /// (currently flagged offline in the repo - it would just make the fan-out wait on a connection timeout) or
        /// <paramref name="Skipped"/> (unsupported SQL version / messaging unavailable); anything else that returned no
        /// sessions was messaged but failed or timed out.
        /// </summary>
        public sealed record XEInstanceListResult(int InstanceID, string Label, bool Ok, bool Skipped, bool Offline,
            string Message, int SessionCount);

        /// <summary>
        /// Lists the existing XE sessions across many instances at once by fanning <see cref="ListSessionsAsync"/> out to
        /// each, with bounded concurrency so a large estate doesn't message every agent simultaneously.  As each instance
        /// completes - whether it returned sessions, failed / timed out, or was skipped (unsupported / no messaging) -
        /// <paramref name="onInstanceResult"/> is invoked with its outcome and (for a success) its rows, stamped with the
        /// <c>Instance</c> label + <c>InstanceID</c>.  Reporting every instance (not just successes) lets the caller show
        /// live progress and keep an instance's last-known rows when a single refresh misses it, rather than blanking it -
        /// offline / no-permission / older / slow-to-respond instances are the normal case at scale.
        ///
        /// Everything is UI-thread affine via the WinForms synchronization context - the per-instance requests run their
        /// I/O off-thread but their continuations (and therefore <paramref name="onInstanceResult"/>) resume on the
        /// caller's thread, so the callback can safely touch the grid without marshalling.  The returned list is the full
        /// set of per-instance outcomes.
        /// </summary>
        public static async Task<IReadOnlyList<XEInstanceListResult>> ListSessionsMultiAsync(
            IEnumerable<int> instanceIDs, int maxConcurrency, Func<XEInstanceListResult, DataTable, Task> onInstanceResult,
            CancellationToken cancellationToken = default)
        {
            if (onInstanceResult == null) throw new ArgumentNullException(nameof(onInstanceResult));
            var ids = (instanceIDs ?? Enumerable.Empty<int>()).Where(id => id > 0).Distinct().ToList();
            var results = new ConcurrentBag<XEInstanceListResult>();
            using var throttle = new SemaphoreSlim(Math.Max(1, maxConcurrency));
            // ListSessionsAsync manages its own status; the multi-view owns the aggregate status, so swallow per-instance.
            void NoStatus(string message, string details, System.Drawing.Color color) { }

            // Instances the collector currently can't reach: messaging one just makes the fan-out wait on a source
            // connection timeout before failing, so short-circuit them (best-effort - on lookup failure we message all).
            var offline = await GetOfflineInstanceIDsAsync();

            async Task RunOne(int id)
            {
                var label = XEInstanceLabels.Resolve(id, id.ToString());
                XEInstanceListResult result;
                DataTable rows = null;
                try
                {
                    if (offline.Contains(id))
                    {
                        result = new XEInstanceListResult(id, label, false, false, true,
                            "Instance is currently offline - not contacted.", 0);
                    }
                    else
                    {
                        var context = new DBADashContext { InstanceID = id, InstanceName = label };
                        if (!context.IsXESupported)
                        {
                            result = new XEInstanceListResult(id, label, false, true, false,
                                "Extended events aren't supported on this SQL Server version.", 0);
                        }
                        else if (!context.CanMessage)
                        {
                            result = new XEInstanceListResult(id, label, false, true, false,
                                "Messaging isn't available for this instance.", 0);
                        }
                        else
                        {
                            await throttle.WaitAsync(cancellationToken);
                            try
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                var outcome = await ListSessionsAsync(context, NoStatus, cancellationToken);
                                if (outcome.Ok && outcome.Sessions != null)
                                {
                                    StampInstance(outcome.Sessions, id, label, context);
                                    rows = outcome.Sessions;
                                    result = new XEInstanceListResult(id, label, true, false, false, null, rows.Rows.Count);
                                }
                                else
                                {
                                    result = new XEInstanceListResult(id, label, false, false, false,
                                        outcome.Message ?? "No response from the service.", 0);
                                }
                            }
                            finally
                            {
                                throttle.Release();
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    throw; // superseded by a newer refresh - drop this instance's result silently
                }
                catch (Exception ex)
                {
                    result = new XEInstanceListResult(id, label, false, false, false, ex.Message, 0);
                }

                results.Add(result);
                if (!cancellationToken.IsCancellationRequested) await onInstanceResult(result, rows);
            }

            await Task.WhenAll(ids.Select(RunOne));
            return results.OrderBy(r => r.Label, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// The InstanceIDs currently flagged offline in the repo (<c>dbo.OfflineInstances</c> where <c>IsCurrent = 1</c> -
        /// set by the collector when it can't reach an instance).  Best-effort: any failure returns an empty set so the
        /// fan-out simply messages every instance (its previous behaviour) rather than hiding instances on a lookup error.
        /// </summary>
        private static async Task<HashSet<int>> GetOfflineInstanceIDsAsync()
        {
            var offline = new HashSet<int>();
            try
            {
                await using var cn = new SqlConnection(Common.ConnectionString);
                await using var cmd = new SqlCommand(
                    "SELECT InstanceID FROM dbo.OfflineInstances WHERE IsCurrent = 1", cn);
                await cn.OpenAsync();
                await using var rdr = await cmd.ExecuteReaderAsync();
                while (await rdr.ReadAsync()) offline.Add(rdr.GetInt32(0));
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Multi-instance XE: couldn't read the offline instance list; messaging all instances.");
            }
            return offline;
        }

        /// <summary>
        /// Adds the left-most <c>Instance</c> label column (+ hidden <c>InstanceID</c>) to a per-instance session table so
        /// the aggregated grid can tell rows from different instances apart, plus per-row <c>CanManage</c> / <c>CanWatch</c>
        /// policy flags (resolved from the instance's collect-agent policy + the caller's role for each session name) so
        /// the grid can gate the start/stop, watch and view-data action links exactly like the per-instance viewer does.
        /// </summary>
        private static void StampInstance(DataTable dt, int instanceID, string label, DBADashContext context)
        {
            if (!dt.Columns.Contains("Instance")) dt.Columns.Add("Instance", typeof(string)).SetOrdinal(0);
            if (!dt.Columns.Contains("InstanceID")) dt.Columns.Add("InstanceID", typeof(int));
            if (!dt.Columns.Contains("CanManage")) dt.Columns.Add("CanManage", typeof(bool));
            if (!dt.Columns.Contains("CanWatch")) dt.Columns.Add("CanWatch", typeof(bool));
            // The action-link text is precomputed here (rather than in the grid's CellFormatting) so the link columns can
            // bind to real columns - reading sibling-row data during formatting is unsafe while the bound view is being
            // merged/pruned and can throw from DataGridViewRow.DataBoundItem.  Empty text = a blank, non-clickable cell.
            if (!dt.Columns.Contains("ActionStartStop")) dt.Columns.Add("ActionStartStop", typeof(string));
            if (!dt.Columns.Contains("ActionWatch")) dt.Columns.Add("ActionWatch", typeof(string));
            if (!dt.Columns.Contains("ActionViewData")) dt.Columns.Add("ActionViewData", typeof(string));
            var hasStartTime = dt.Columns.Contains("StartTime");
            foreach (DataRow r in dt.Rows)
            {
                var name = dt.Columns.Contains("Name") ? r["Name"] as string : null;
                var running = dt.Columns.Contains("IsRunning") && r["IsRunning"] != DBNull.Value &&
                              Convert.ToBoolean(r["IsRunning"]);
                var canManage = !string.IsNullOrEmpty(name) && context.CanManageXESession(name);
                var canWatch = !string.IsNullOrEmpty(name) && context.CanWatchXESession(name);
                var readable = HasReadableTarget(dt.Columns.Contains("TargetTypes") ? r["TargetTypes"] as string : null);

                r["Instance"] = label;
                r["InstanceID"] = instanceID;
                r["CanManage"] = canManage;
                r["CanWatch"] = canWatch;
                r["ActionStartStop"] = canManage ? (running ? "Stop" : "Start") : string.Empty;
                r["ActionWatch"] = running && canWatch ? "Watch" : string.Empty;
                r["ActionViewData"] = running && canWatch && readable ? "View Data" : string.Empty;
                // dm_xe_sessions.create_time is server-LOCAL;
                // so the aggregated grid shows every instance's start time in one consistent (the app's) time zone.
                if (hasStartTime && r["StartTime"] != DBNull.Value)
                {
                    r["StartTime"] = Convert.ToDateTime(r["StartTime"]).AddMinutes(context.UTCOffset).ToAppTimeZone();
                }
            }
        }

        /// <summary>The target must carry an event stream to view/watch - only event_file and ring_buffer qualify.</summary>
        private static bool HasReadableTarget(string targetTypes) =>
            !string.IsNullOrEmpty(targetTypes) &&
            (targetTypes.IndexOf("event_file", StringComparison.OrdinalIgnoreCase) >= 0 ||
             targetTypes.IndexOf("ring_buffer", StringComparison.OrdinalIgnoreCase) >= 0);

        /// <summary>
        /// Starts or stops an existing session.  Returns an <see cref="XEControlOutcome"/> carrying whether the service
        /// accepted the request (Success vs Failure/Warning) and the session's actual running state afterwards, so the
        /// caller never reports success for a rejected or ineffective operation.
        /// </summary>
        public static async Task<XEControlOutcome> ControlSessionAsync(DBADashContext context, string sessionName,
            XESessionOperation operation, MessagingHelper.SetStatusDelegate setStatus)
        {
            if (context.ImportAgentID == null)
            {
                const string noAgent = "No Import Agent is configured for this instance, so the request can't be sent.";
                setStatus("No Import Agent", string.Empty, DashColors.Fail);
                return new XEControlOutcome(false, null, noAgent);
            }
            var message = new XESessionControlMessage
            {
                ConnectionID = context.ConnectionID,
                SessionName = sessionName,
                Operation = operation,
                CollectAgent = context.CollectAgent,
                ImportAgent = context.ImportAgent,
                Lifetime = 60
            };

            bool? running = null;
            var terminal = false;
            var ok = false;
            string outcomeMessage = null;
            await MessagingHelper.SendMessageAndProcessReply(message, (int)context.ImportAgentID, setStatus,
                (reply, group, status) =>
                {
                    switch (reply.Type)
                    {
                        case ResponseMessage.ResponseTypes.Success:
                            terminal = true;
                            ok = true;
                            if (reply.Data?.Tables.Count > 0 && reply.Data.Tables[0].Rows.Count > 0)
                            {
                                var row = reply.Data.Tables[0].Rows[0];
                                if (row.Table.Columns.Contains("IsRunning") && row["IsRunning"] != DBNull.Value)
                                {
                                    running = Convert.ToBoolean(row["IsRunning"]);
                                }
                            }
                            break;

                        case ResponseMessage.ResponseTypes.Failure:
                        case ResponseMessage.ResponseTypes.Warning:
                            terminal = true;
                            ok = false;
                            outcomeMessage = reply.Message;
                            break;
                    }
                    return Task.CompletedTask;
                }, Guid.NewGuid());

            if (!terminal)
            {
                outcomeMessage = "The request ended without a result from the service.  The service may be running an " +
                                 "older version, managing extended events may be disabled on it, or it may not be running.";
                return new XEControlOutcome(false, running, outcomeMessage);
            }
            return new XEControlOutcome(ok, running, outcomeMessage);
        }

        /// <summary>
        /// Reads the current contents of an existing session's target (event_file preferred, ring_buffer fallback) and
        /// returns them as a single batch to display.  Non-destructive one-shot read - unlike <see cref="WatchAsync"/>
        /// it doesn't tail, so no message group / heartbeat / cancellation is involved.
        /// </summary>
        public static async Task<XEViewDataOutcome> ViewTargetDataAsync(DBADashContext context, string sessionName,
            int maxEvents, DateTime? startUtc, MessagingHelper.SetStatusDelegate setStatus)
        {
            if (context.ImportAgentID == null)
            {
                const string noAgent = "No Import Agent is configured for this instance, so the request can't be sent.";
                setStatus("No Import Agent", string.Empty, DashColors.Fail);
                return new XEViewDataOutcome(false, null, null, 0, false, maxEvents, noAgent);
            }
            var message = new XEViewTargetDataMessage
            {
                ConnectionID = context.ConnectionID,
                SessionName = sessionName,
                MaxEvents = maxEvents,
                StartUtc = startUtc,
                CollectAgent = context.CollectAgent,
                ImportAgent = context.ImportAgent,
                Lifetime = 120
            };

            DataTable events = null;
            string targetType = null;
            var capped = false;
            var terminal = false;
            var ok = false;
            string outcomeMessage = null;

            await MessagingHelper.SendMessageAndProcessReply(message, (int)context.ImportAgentID, setStatus,
                (reply, group, status) =>
                {
                    switch (reply.Type)
                    {
                        case ResponseMessage.ResponseTypes.Success:
                            terminal = true;
                            ok = true;
                            var ds = reply.Data;
                            if (ds != null)
                            {
                                events = ds.Tables.Contains("XE") ? ds.Tables["XE"]
                                    : ds.Tables.Count > 0 ? ds.Tables[0] : null;
                                if (ds.Tables.Contains("ViewSummary") && ds.Tables["ViewSummary"].Rows.Count > 0)
                                {
                                    var row = ds.Tables["ViewSummary"].Rows[0];
                                    targetType = row["TargetType"] as string;
                                    capped = row["Capped"] != DBNull.Value && Convert.ToBoolean(row["Capped"]);
                                }
                            }
                            break;

                        case ResponseMessage.ResponseTypes.Failure:
                        case ResponseMessage.ResponseTypes.Warning:
                            terminal = true;
                            ok = false;
                            outcomeMessage = reply.Message;
                            break;
                    }
                    return Task.CompletedTask;
                }, Guid.NewGuid());

            if (!terminal)
            {
                outcomeMessage = "The request ended without a result from the service.  The service may be running an " +
                                 "older version, managing extended events may be disabled on it, or it may not be running.";
                return new XEViewDataOutcome(false, null, null, 0, false, maxEvents, outcomeMessage);
            }
            var total = events?.Rows.Count ?? 0;
            return new XEViewDataOutcome(ok, events, targetType, total, capped, maxEvents, outcomeMessage);
        }

        /// <summary>Reconstructs and returns the <c>CREATE EVENT SESSION</c> DDL for an existing session.</summary>
        public static async Task<string> ScriptSessionAsync(DBADashContext context, string sessionName,
            MessagingHelper.SetStatusDelegate setStatus)
        {
            if (context.ImportAgentID == null)
            {
                setStatus("No Import Agent", string.Empty, DashColors.Fail);
                return null;
            }
            var message = new XESessionScriptMessage
            {
                ConnectionID = context.ConnectionID,
                SessionName = sessionName,
                CollectAgent = context.CollectAgent,
                ImportAgent = context.ImportAgent,
                Lifetime = 60
            };

            string ddl = null;
            await MessagingHelper.SendMessageAndProcessReply(message, (int)context.ImportAgentID, setStatus,
                (reply, group, status) =>
                {
                    if (reply.Type == ResponseMessage.ResponseTypes.Success && reply.Data?.Tables.Count > 0 &&
                        reply.Data.Tables[0].Rows.Count > 0)
                    {
                        ddl = reply.Data.Tables[0].Rows[0][0] as string;
                    }
                    return Task.CompletedTask;
                }, Guid.NewGuid());
            return ddl;
        }

        /// <summary>
        /// Watches a session, streaming each batch of events to <paramref name="onBatch"/> until the duration
        /// elapses or the watch is cancelled.  <paramref name="messageGroup"/> is created by the caller so it can be
        /// used to cancel (see <see cref="CancelWatchAsync"/>).
        /// </summary>
        public static async Task<XEWatchOutcome> WatchAsync(DBADashContext context, string sessionName,
            int durationSeconds, Guid messageGroup, MessagingHelper.SetStatusDelegate setStatus,
            Func<DataTable, Task> onBatch, Action<DataRow> onSummary)
        {
            if (context.ImportAgentID == null)
            {
                const string noAgent = "No Import Agent is configured for this instance, so the watch request can't be sent.";
                setStatus("No Import Agent", string.Empty, DashColors.Fail);
                return new XEWatchOutcome(false, false, noAgent);
            }

            var message = new XEWatchSessionMessage
            {
                ConnectionID = context.ConnectionID,
                SessionName = sessionName,
                MaxDurationSeconds = durationSeconds,
                CollectAgent = context.CollectAgent,
                ImportAgent = context.ImportAgent,
                Lifetime = durationSeconds + 60 // conversation must outlive the watch
            };

            var terminal = false;
            var ok = false;
            var cancelled = false;
            string outcomeMessage = null;

            async Task OnProgress(ResponseMessage reply, Guid group)
            {
                var table = reply.Data?.Tables.Count > 0 ? reply.Data.Tables[0] : null;
                if (table == null || !table.Columns.Contains("event_type") || table.Rows.Count == 0) return;
                await onBatch(table);
            }

            Task OnCompleted(ResponseMessage reply, Guid group, MessagingHelper.SetStatusDelegate status)
            {
                switch (reply.Type)
                {
                    case ResponseMessage.ResponseTypes.Success:
                        var row = reply.Data?.Tables.Count > 0 && reply.Data.Tables[0].Rows.Count > 0
                            ? reply.Data.Tables[0].Rows[0]
                            : null;
                        cancelled = row != null && row.Table.Columns.Contains("Cancelled") &&
                                    row["Cancelled"] != DBNull.Value && (bool)row["Cancelled"];
                        var heartbeatLost = row != null && row.Table.Columns.Contains("HeartbeatLost") &&
                                    row["HeartbeatLost"] != DBNull.Value && (bool)row["HeartbeatLost"];
                        var total = row != null && row.Table.Columns.Contains("TotalEvents") ? row["TotalEvents"] : 0;
                        if (row != null) onSummary(row);
                        terminal = true;
                        ok = true;
                        outcomeMessage = heartbeatLost
                            ? $"Watch stopped - client heartbeat lost. {total} events captured."
                            : cancelled ? "Watch stopped." : $"Watch complete - {total} events captured.";
                        break;

                    case ResponseMessage.ResponseTypes.Failure:
                    case ResponseMessage.ResponseTypes.Warning:
                        terminal = true;
                        ok = false;
                        outcomeMessage = reply.Message;
                        break;
                }
                return Task.CompletedTask;
            }

            try
            {
                await MessagingHelper.SendMessageAndProcessReply(message, (int)context.ImportAgentID, setStatus,
                    OnCompleted, messageGroup, OnProgress);
            }
            catch (Exception ex)
            {
                setStatus(ex.Message, ex.ToString(), DashColors.Fail);
                return new XEWatchOutcome(false, false, ex.Message);
            }

            if (!terminal)
            {
                outcomeMessage = "The watch ended without a result from the service.  The service may be running an " +
                                 "older version, managing extended events may be disabled on it, or it may not be running.";
                setStatus(outcomeMessage, string.Empty, DashColors.Fail);
                return new XEWatchOutcome(false, false, outcomeMessage);
            }

            return new XEWatchOutcome(ok, cancelled, outcomeMessage);
        }

        /// <summary>
        /// Requests cancellation of a running watch.  Fire-and-forget on a FRESH conversation group so the cancel's
        /// reply loop and the watch's reply loop never contend (mirrors <see cref="XETraceController.CancelAsync"/>).
        /// </summary>
        public static async Task CancelWatchAsync(DBADashContext context, Guid messageGroup,
            MessagingHelper.SetStatusDelegate setStatus)
        {
            if (messageGroup == Guid.Empty || context.ImportAgentID == null) return;
            var cancelGroup = Guid.NewGuid();
            var msg = new CancellationMessage
            {
                CollectAgent = context.CollectAgent,
                ImportAgent = context.ImportAgent,
                Lifetime = 60,
                CancelMessageId = messageGroup
            };
            setStatus("Stop requested...", string.Empty, DashColors.Warning);
            await MessagingHelper.SendMessageAndProcessReply(msg, context, setStatus,
                (reply, group, status) => Task.CompletedTask, cancelGroup);
        }
    }
}
