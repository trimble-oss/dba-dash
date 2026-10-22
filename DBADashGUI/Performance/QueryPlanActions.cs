using DBADash;
using DBADash.Messaging;
using DBADash.QueryPlan.Live;
using DBADashGUI.Messaging;
using DBADashGUI.Theme;
using System;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    /// <summary>
    /// Shared query plan actions for a running queries row.  Depending on what is available we either:
    ///   * View the plan we already captured,
    ///   * Collect the plan on demand via messaging (if the user has access), or
    ///   * Show a script to find the plan on the source instance.
    /// </summary>
    internal static class QueryPlanActions
    {
        internal enum PlanAction
        {
            None,
            View,
            Collect,
            FindScript
        }

        /// <summary>Work out which plan action is available for the row.</summary>
        internal static PlanAction Determine(DataRowView row, out DBADashContext context)
        {
            context = null;
            var hasPlan = row["has_plan"] != DBNull.Value && Convert.ToBoolean(row["has_plan"]);
            if (hasPlan) return PlanAction.View;

            var canFindPlan = row["plan_handle"] != DBNull.Value && row["query_plan_hash"] != DBNull.Value;
            if (!canFindPlan) return PlanAction.None;

            context = PlanContext(row);
            return context.CanMessage ? PlanAction.Collect : PlanAction.FindScript;
        }

        /// <summary>
        /// The context to open the row's plan with: its instance, and the database the session was in where the row has
        /// one, which the plan viewer's Query Store links search.
        /// </summary>
        internal static DBADashContext PlanContext(DataRowView row)
        {
            var context = CommonData.GetDBADashContext(Convert.ToInt32(row["InstanceID"]));
            if (row.Row.Table.Columns.Contains("database_name"))
            {
                context.DatabaseName = Convert.ToString(row["database_name"].DBNullToNull());
            }
            return context;
        }

        /// <summary>The button caption for the available action.</summary>
        internal static string ActionText(PlanAction action) => action switch
        {
            PlanAction.View => "View Plan",
            PlanAction.Collect => "Collect Plan",
            PlanAction.FindScript => "Find Plan",
            _ => "Plan"
        };

        internal static async Task Execute(DataRowView row, ToolStripStatusLabel lblStatus)
        {
            var action = Determine(row, out var context);
            switch (action)
            {
                case PlanAction.View:
                    ViewPlan(row, PlanContext(row));
                    break;

                case PlanAction.Collect:
                    await CollectPlan(row, context, lblStatus);
                    break;

                case PlanAction.FindScript:
                    RunningQueries.FindPlanScript(row);
                    break;
            }
        }

        private static void ViewPlan(DataRowView row, DBADashContext context)
        {
            if (row["query_plan_text"] == DBNull.Value ||
                string.IsNullOrEmpty(Convert.ToString(row["query_plan_text"])))
            {
                row["query_plan_text"] = RunningQueries.GetPlan(row);
            }
            Common.ShowQueryPlan((string)row["query_plan_text"], context: context);
        }

        private static async Task CollectPlan(DataRowView row, DBADashContext context, ToolStripStatusLabel lblStatus)
        {
            QueryPlanCollectionMessage message;
            try
            {
                message = RunningQueries.GetPlanCollectionMessage(row, context);
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Plan collection error", "Warning", TaskDialogIcon.Warning);
                return;
            }

            var messageGroup = Guid.NewGuid();

            Task ProcessReply(ResponseMessage reply, Guid group, MessagingHelper.SetStatusDelegate setStatus)
                => ProcessPlanReply(reply, row, context, setStatus);

            lblStatus.InvokeSetStatus("Collecting query plan...", string.Empty, DashColors.Information);
            await MessagingHelper.SendMessageAndProcessReply((MessageBase)message, context, lblStatus, ProcessReply,
                messageGroup);
        }

        /// <summary>
        /// True if a live plan can be requested for the row: a running request (not a sleeping session) on an instance the
        /// user can message.  Whether the request is still running is checked by the service.
        /// </summary>
        internal static bool CanGetLivePlan(DataRowView row)
        {
            var columns = row.Row.Table.Columns;
            if (!columns.Contains("session_id") || !columns.Contains("start_time") || !columns.Contains("InstanceID"))
            {
                return false;
            }
            if (row["start_time"] == DBNull.Value || row["session_id"] == DBNull.Value || row["InstanceID"] == DBNull.Value)
            {
                return false;
            }
            return CommonData.GetDBADashContext(Convert.ToInt32(row["InstanceID"])).CanMessage;
        }

        internal const string LivePlanToolTip =
            "Get the live query plan for this request from the source instance (via messaging).  Shows actual rows, time etc. so far for a query that is still running.  Requires lightweight query profiling (on by default from SQL 2019).";

        /// <summary>Get the live plan for a running request via messaging and open it in the plan viewer.  It isn't saved to the repository.</summary>
        internal static async Task GetLivePlan(DataRowView row, ToolStripStatusLabel lblStatus)
        {
            var context = PlanContext(row);
            if (!context.CanMessage)
            {
                MessageBox.Show("Messaging is not available for this instance.", "Live Plan", MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var sessionId = Convert.ToInt32(row["session_id"]);
            var message = new LivePlanMessage
            {
                ConnectionID = context.ConnectionID,
                CollectAgent = context.CollectAgent,
                ImportAgent = context.ImportAgent,
                SessionID = sessionId,
                StartTimeUtc = Convert.ToDateTime(row["start_time"]).AppTimeZoneToUtc(),
                SnapshotUtcOffset = context.UTCOffset,
                Lifetime = 60
            };

            Task ProcessReply(ResponseMessage reply, Guid group, MessagingHelper.SetStatusDelegate setStatus)
                => ProcessLivePlanReply(reply, sessionId, message.StartTimeUtc, message.SnapshotUtcOffset, context, setStatus);

            lblStatus.InvokeSetStatus("Getting live query plan...", string.Empty, DashColors.Information);
            await MessagingHelper.SendMessageAndProcessReply(message, context, FittedStatus(lblStatus), ProcessReply, Guid.NewGuid());
        }

        // About as much as a status bar shows beside its other items.
        private const int MaxStatusLength = 80;

        /// <summary>
        /// Sets <paramref name="label"/>, cutting a long message down to its first sentence.  A status strip hides an item
        /// that doesn't fit rather than cutting it short, so the explanation of why a query has no live plan - a few
        /// sentences - showed nothing at all.  The whole message goes in the tooltip, which a click shows.
        /// </summary>
        private static MessagingHelper.SetStatusDelegate FittedStatus(ToolStripStatusLabel label) => (text, details, colour) =>
        {
            text ??= string.Empty;
            if (text.Length <= MaxStatusLength)
            {
                label.InvokeSetStatus(text, details, colour);
                return;
            }

            var end = text.IndexOf(".  ", StringComparison.Ordinal);
            var shortText = end > 0 && end < MaxStatusLength ? text[..(end + 1)] : text[..MaxStatusLength].TrimEnd() + "...";
            var fullText = string.IsNullOrEmpty(details) ? text : text + Environment.NewLine + Environment.NewLine + details;
            label.InvokeSetStatus(shortText + "  (click for details)", fullText, colour);
        };

        private static Task ProcessLivePlanReply(ResponseMessage reply, int sessionId, DateTime startTimeUtc, int snapshotUtcOffset, DBADashContext context,
            MessagingHelper.SetStatusDelegate setStatus)
        {
            try
            {
                if (reply.Type != ResponseMessage.ResponseTypes.Success)
                {
                    setStatus(reply.Message, reply.Exception?.ToString(), DashColors.Fail);
                    return Task.CompletedTask;
                }

                var dt = reply.Data?.Tables["LivePlan"];
                if (dt == null || dt.Rows.Count == 0 || dt.Rows[0]["query_plan"] == DBNull.Value)
                {
                    var reason = dt is { Rows.Count: > 0 } && dt.Columns.Contains("unavailable_reason") && dt.Rows[0]["unavailable_reason"] != DBNull.Value
                        ? Convert.ToString(dt.Rows[0]["unavailable_reason"])
                        : "Live query plan was not found.  The query may have completed.";
                    setStatus(reason, string.Empty, DashColors.Fail);
                    return Task.CompletedTask;
                }

                var row = dt.Rows[0];
                var plan = PlanLiveStatementText.Apply(PlanLiveMark.Apply((string)row["query_plan"]),
                    row.Table.Columns.Contains("statement_text") && row["statement_text"] != DBNull.Value
                        ? Convert.ToString(row["statement_text"])
                        : null);
                var captured = Convert.ToDateTime(row["captured_utc"]).ToAppTimeZone();
                var fileName = $"Live Plan SPID {sessionId} {captured:yyyy-MM-dd HHmmss}.sqlplan";

                // The viewer refreshes it from here - see LivePlanSource.
                var live = new LivePlanSource(context, sessionId, startTimeUtc, snapshotUtcOffset, row);
                Common.ShowQueryPlan(plan, fileName, context, live);
                setStatus($"Live plan for session {sessionId} captured at {captured:T}", string.Empty, DashColors.Green);
            }
            catch (Exception ex)
            {
                setStatus(ex.Message, ex.ToString(), DashColors.Red);
            }
            return Task.CompletedTask;
        }

        private static async Task ProcessPlanReply(ResponseMessage reply, DataRowView row, DBADashContext context,
            MessagingHelper.SetStatusDelegate setStatus)
        {
            try
            {
                if (reply.Type != ResponseMessage.ResponseTypes.Success)
                {
                    setStatus(reply.Message, reply.Exception?.ToString(), DashColors.Fail);
                    return;
                }
                setStatus("Loading Plan...", string.Empty, DashColors.Information);

                var dtPlan = reply.Data.Tables["QueryPlans"];
                if (dtPlan == null || dtPlan.Rows.Count == 0)
                {
                    setStatus("Query plan was not found", string.Empty, DashColors.Fail);
                    MessageBox.Show("Query plan was not found", "Warning", MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                var status = "Plan collected.  Loading in default app.";
                var tooltip = string.Empty;
                var statusColor = DashColors.Green;
                try
                {
                    await DBImporter.UpdateCollectionAsync(dtPlan, context.InstanceID, DateTime.UtcNow,
                        Common.ConnectionString);
                }
                catch (Exception ex)
                {
                    status = "Plan collected, but error saving to repository database.";
                    tooltip = ex.Message;
                    statusColor = DashColors.Warning;
                }

                var planBin = dtPlan.Rows[0].Field<byte[]>("query_plan_compressed");
                var planText = SMOBaseClass.Unzip(planBin);
                row["has_plan"] = true;
                row["query_plan_text"] = planText;

                Common.ShowQueryPlan(planText, context: context);
                setStatus(status, tooltip, statusColor);
            }
            catch (Exception ex)
            {
                setStatus(ex.Message, ex.ToString(), DashColors.Red);
            }
        }
    }
}
