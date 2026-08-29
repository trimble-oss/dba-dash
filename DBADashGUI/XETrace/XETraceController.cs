using DBADash.Messaging;
using DBADash.XE;
using DBADashGUI.Messaging;
using System;
using System.Collections.Concurrent;
using System.Data;
using System.Threading.Tasks;

namespace DBADashGUI.XETrace
{
    /// <summary>
    /// Orchestrates an ad-hoc XE trace from the GUI: opens the repo session, sends the <see cref="XETraceMessage"/> to
    /// the service, persists each streamed batch to the repo and reports it to the UI, then records completion.
    /// The service never touches the repo (relay-safe) - this is the GUI-side counterpart, modelled on
    /// <see cref="MessagingHelper.ForceQueryPlan"/>.
    /// </summary>
    internal static class XETraceController
    {
        // XE.XETraceSession.Status values.
        private const byte StatusCompleted = 1;
        private const byte StatusCancelled = 2;
        private const byte StatusError = 3;

        /// <summary>Outcome of a trace run so the caller can give clear, unmissable feedback.</summary>
        public sealed record XETraceOutcome(bool Ok, bool Cancelled, string Message, long? SessionID);

        // The XE catalog varies by version/edition, so cache it keyed on that rather than re-querying per instance.
        private static readonly ConcurrentDictionary<string, XEObjectCatalog> CatalogCache = new();

        /// <summary>Fetches the instance's XE object catalog (events/fields/actions), cached per version + edition.</summary>
        public static async Task<XEObjectCatalog> GetCatalogAsync(DBADashContext context,
            MessagingHelper.SetStatusDelegate setStatus)
        {
            if (context.ImportAgentID == null) return new XEObjectCatalog();

            var key = $"{context.ProductVersion}|{context.EngineEdition}";
            if (CatalogCache.TryGetValue(key, out var cached)) return cached;

            // Persistent file cache (survives app restarts) - the catalog is static per build + edition, so this
            // avoids the round-trip to the instance on the first use each session.
            var fromFile = XECatalogCache.TryLoad(key);
            if (fromFile != null)
            {
                CatalogCache[key] = fromFile;
                return fromFile;
            }

            var message = new XEObjectCatalogMessage
            {
                ConnectionID = context.ConnectionID,
                CollectAgent = context.CollectAgent,
                ImportAgent = context.ImportAgent,
                Lifetime = 60
            };

            XEObjectCatalog result = null;
            await MessagingHelper.SendMessageAndProcessReply(message, (int)context.ImportAgentID, setStatus,
                (reply, group, status) =>
                {
                    if (reply.Type == ResponseMessage.ResponseTypes.Success && reply.Data != null)
                    {
                        result = XEObjectCatalog.FromDataSet(reply.Data);
                    }
                    return Task.CompletedTask;
                }, Guid.NewGuid());

            result ??= new XEObjectCatalog();
            if (result.Events.Count > 0)
            {
                CatalogCache[key] = result;
                XECatalogCache.Save(key, result);
            }
            return result;
        }

        /// <summary>
        /// Runs the trace to completion.  <paramref name="messageGroup"/> is created by the caller so it can also
        /// be used to cancel the trace (see <see cref="CancelAsync"/>).  Returns the repo session id, or null if the
        /// session couldn't be opened (e.g. one already running for the instance).
        /// </summary>
        /// <summary>
        /// Grid column that identifies an event's source instance in a multi-instance run.  This is not persisted into
        /// the event JSON - the source instance is the session's own InstanceID (see <c>XE.XETraceSession</c>).  The GUI
        /// stamps it on live batches (from the trace's InstanceID) and resolves it from the session row on history
        /// reload (see <c>XEStoredEvents.Expand</c>), so both paths label instances identically.
        /// </summary>
        public const string InstanceColumn = "Instance";

        public static async Task<XETraceOutcome> RunTraceAsync(
            DBADashContext context,
            XETraceConfig config,
            Guid messageGroup,
            MessagingHelper.SetStatusDelegate setStatus,
            Func<DataTable, Task> onBatch,
            Action<DataRow> onSummary,
            Guid? runGroupID = null,
            Action onRunningConfirmed = null)
        {
            if (context.ImportAgentID == null)
            {
                const string noAgent = "No Import Agent is configured for this instance, so the trace request can't be sent.";
                setStatus("No Import Agent", string.Empty, DashColors.Fail);
                return new XETraceOutcome(false, false, noAgent, null);
            }

            long sessionID;
            try
            {
                sessionID = await XETraceRepo.StartAsync(context.InstanceID, messageGroup, config.EventTypesCsv,
                    config.MaxDurationSeconds, config.FiltersJson, runGroupID, config.Notes);
            }
            catch (Exception ex)
            {
                // e.g. the one-running-per-instance unique index rejected it, or the schema isn't deployed.
                setStatus(ex.Message, ex.ToString(), DashColors.Fail);
                return new XETraceOutcome(false, false, ex.Message, null);
            }

            var message = new XETraceMessage
            {
                ConnectionID = context.ConnectionID,
                Events = config.EventDefs,
                Filters = config.Filters,
                GlobalActions = config.GlobalActions,
                EventCustomizations = config.EventCustomizations,
                RequestedTarget = config.Target,
                MaxDurationSeconds = config.MaxDurationSeconds,
                BatchIntervalSeconds = config.BatchIntervalSeconds,
                SampleN = config.SampleN,
                CaptureXel = config.CaptureXel,
                CollectAgent = context.CollectAgent,
                ImportAgent = context.ImportAgent,
                Lifetime = config.MaxDurationSeconds + 60 // conversation must outlive the trace
            };

            var terminal = false;
            var ok = false;
            var cancelled = false;
            string outcomeMessage = null;

            async Task OnProgress(ResponseMessage reply, Guid group)
            {
                // The service sends a "Trace running on ..." progress reply once the session is actually created and
                // started - that's the confirmation to move the UI from "request sent" to "running".  (The generic
                // "Message Received" ack that precedes it carries no such text.)
                if (reply.Type == ResponseMessage.ResponseTypes.Progress &&
                    reply.Message?.Contains("running on", StringComparison.OrdinalIgnoreCase) == true)
                {
                    onRunningConfirmed?.Invoke();
                }

                // The "trace running" reply carries the service-generated DDL and resolved target (both known the
                // moment the session starts).  Persist them now, while the row is still Running - the completion
                // summary that also carries them can be lost to the Status guard when Stop force-cancels the row
                // first, or never arrive at all if the trace is abandoned.  Best-effort: the completion backfills as
                // a fallback, and a failed audit write must not disrupt the running trace.
                if (reply.XETraceStarted != null)
                {
                    try
                    {
                        await XETraceRepo.SetDefinitionAsync(sessionID, reply.XETraceStarted.GeneratedDDL,
                            reply.XETraceStarted.TargetType);
                    }
                    catch (Exception ex)
                    {
                        // Best-effort: the completion summary backfills the DDL/target, so a failure here isn't fatal -
                        // just log it (we lose the early-persist durability if the trace is later abandoned).
                        Serilog.Log.Warning(ex, "Failed to persist XE trace definition for session {sessionID}", sessionID);
                    }
                }

                var table = reply.Data?.Tables.Count > 0 ? reply.Data.Tables[0] : null;
                if (table == null || !table.Columns.Contains("event_type") || table.Rows.Count == 0) return;
                await XETraceRepo.AddEventsAsync(sessionID, table);
                await onBatch(table);
            }

            async Task OnCompleted(ResponseMessage reply, Guid group, MessagingHelper.SetStatusDelegate status)
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
                        var xel = row != null && row.Table.Columns.Contains("XelData") && row["XelData"] != DBNull.Value
                            ? (byte[])row["XelData"]
                            : null;
                        await XETraceRepo.CompleteAsync(sessionID, cancelled ? StatusCancelled : StatusCompleted,
                            TargetTypeByte(row), row?["GeneratedDDL"] as string, xel, null);
                        if (row != null) onSummary(row);
                        terminal = true;
                        ok = true;
                        outcomeMessage = heartbeatLost
                            ? $"Trace stopped - client heartbeat lost. {total} events captured."
                            : cancelled ? "Trace cancelled." : $"Trace complete - {total} events captured.";
                        break;

                    case ResponseMessage.ResponseTypes.Failure:
                    case ResponseMessage.ResponseTypes.Warning:
                        await XETraceRepo.CompleteAsync(sessionID, StatusError, null, null, null, reply.Message);
                        terminal = true;
                        ok = false;
                        outcomeMessage = reply.Message;
                        break;
                }
            }

            try
            {
                await MessagingHelper.SendMessageAndProcessReply(message, (int)context.ImportAgentID, setStatus,
                    OnCompleted, messageGroup, OnProgress);
            }
            catch (Exception ex)
            {
                setStatus(ex.Message, ex.ToString(), DashColors.Fail);
                await SafeCompleteAsync(sessionID, ex.Message);
                return new XETraceOutcome(false, false, ex.Message, sessionID);
            }

            if (!terminal)
            {
                // The conversation ended without a Success/Failure reply - e.g. the service is running an older
                // version that can't process the message, or the dialog lifetime expired.  Surface it clearly
                // instead of silently re-enabling the button.
                outcomeMessage = "The trace ended without a result from the service.  The service may be running an " +
                                 "older version, ad-hoc XE may be disabled on it, or it may not be running.";
                setStatus(outcomeMessage, string.Empty, DashColors.Fail);
                await SafeCompleteAsync(sessionID, outcomeMessage);
                return new XETraceOutcome(false, false, outcomeMessage, sessionID);
            }

            return new XETraceOutcome(ok, cancelled, outcomeMessage, sessionID);
        }

        private static async Task SafeCompleteAsync(long sessionID, string errorMessage)
        {
            try { await XETraceRepo.CompleteAsync(sessionID, StatusError, null, null, null, errorMessage); }
            catch { /* best-effort */ }
        }

        /// <summary>
        /// Force-cleans up any trace on the instance: frees Running repo rows (releasing the one-per-instance lock)
        /// and tells the service to drop the abandoned <c>DBADash_AdHoc</c> session on the source.  Works even when
        /// no trace is being actively processed - the recovery path for an abandoned session blocking new traces.
        /// </summary>
        public static async Task CleanupAsync(DBADashContext context, MessagingHelper.SetStatusDelegate setStatus)
        {
            if (context.ImportAgentID == null)
            {
                setStatus("No Import Agent", string.Empty, DashColors.Fail);
                return;
            }

            // Free the repo lock first so a retry can start even if the service message fails.
            try { await XETraceRepo.CancelRunningAsync(context.InstanceID); }
            catch (Exception ex) { setStatus(ex.Message, ex.ToString(), DashColors.Fail); }

            var message = new XETraceStopMessage
            {
                ConnectionID = context.ConnectionID,
                CollectAgent = context.CollectAgent,
                ImportAgent = context.ImportAgent,
                Lifetime = 60
            };
            await MessagingHelper.SendMessageAndProcessReply(message, (int)context.ImportAgentID, setStatus,
                (reply, group, status) => Task.CompletedTask, Guid.NewGuid());
        }

        /// <summary>
        /// Requests cancellation of a running trace.  Fire-and-forget: it only <b>sends</b> the
        /// <see cref="CancellationMessage"/> (on its own fresh conversation) and returns - the running trace's own
        /// reply loop receives the resulting "Cancelled" terminal reply and updates the UI.  It must NOT start a
        /// second receive loop on the trace's conversation group, or the two loops fight over the same replies on
        /// the UI thread and can hang.
        /// </summary>
        public static async Task CancelAsync(DBADashContext context, Guid messageGroup,
            MessagingHelper.SetStatusDelegate setStatus)
        {
            if (messageGroup == Guid.Empty || context.ImportAgentID == null) return;
            // Use a FRESH conversation group for the cancellation, distinct from the trace's group, so the cancel's
            // reply loop and the trace's reply loop never contend for the same replies.  The cancellation targets
            // the trace by CancelMessageId, not by conversation, so a separate group is fine.
            var cancelGroup = Guid.NewGuid();
            var msg = new CancellationMessage
            {
                CollectAgent = context.CollectAgent,
                ImportAgent = context.ImportAgent,
                Lifetime = 60,
                CancelMessageId = messageGroup
            };
            setStatus("Cancellation requested...", string.Empty, DashColors.Warning);
            await MessagingHelper.SendMessageAndProcessReply(msg, context, setStatus,
                (reply, group, status) => Task.CompletedTask, cancelGroup);
        }

        private static byte? TargetTypeByte(DataRow row)
        {
            if (row == null || !row.Table.Columns.Contains("TargetType")) return null;
            return (row["TargetType"] as string) switch
            {
                "EventFile" => (byte?)1,
                "RingBuffer" => (byte?)2,
                _ => null
            };
        }
    }
}
