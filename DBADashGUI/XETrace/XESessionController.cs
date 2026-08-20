using DBADash.Messaging;
using DBADashGUI.Messaging;
using System;
using System.Data;
using System.Threading.Tasks;

namespace DBADashGUI.XETrace
{
    /// <summary>
    /// GUI-side orchestration for managing the <b>existing</b> XE sessions on a monitored instance: list them,
    /// start/stop one, and watch one live.  Sends the corresponding messages via
    /// <see cref="MessagingHelper.SendMessageAndProcessReply"/> and returns the results.  Nothing is persisted to the
    /// repo (watching is transient viewing) - unlike the ad-hoc trace (<see cref="XETraceController"/>).
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

        /// <summary>Lists the instance's existing XE sessions (name, running state, targets, event count).</summary>
        public static async Task<DataTable> ListSessionsAsync(DBADashContext context,
            MessagingHelper.SetStatusDelegate setStatus)
        {
            if (context.ImportAgentID == null)
            {
                setStatus("No Import Agent", string.Empty, DashColors.Fail);
                return null;
            }
            var message = new XESessionListMessage
            {
                ConnectionID = context.ConnectionID,
                CollectAgent = context.CollectAgent,
                ImportAgent = context.ImportAgent,
                Lifetime = 60
            };

            DataTable result = null;
            await MessagingHelper.SendMessageAndProcessReply(message, (int)context.ImportAgentID, setStatus,
                (reply, group, status) =>
                {
                    if (reply.Type == ResponseMessage.ResponseTypes.Success && reply.Data?.Tables.Count > 0)
                    {
                        result = reply.Data.Tables[0];
                    }
                    return Task.CompletedTask;
                }, Guid.NewGuid());
            return result;
        }

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
