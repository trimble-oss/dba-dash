using DBADash;
using DBADash.Messaging;
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

            context = CommonData.GetDBADashContext(Convert.ToInt32(row["InstanceID"]));
            return context.CanMessage ? PlanAction.Collect : PlanAction.FindScript;
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
                    ViewPlan(row);
                    break;

                case PlanAction.Collect:
                    await CollectPlan(row, context, lblStatus);
                    break;

                case PlanAction.FindScript:
                    RunningQueries.FindPlanScript(row);
                    break;
            }
        }

        private static void ViewPlan(DataRowView row)
        {
            if (row["query_plan_text"] == DBNull.Value ||
                string.IsNullOrEmpty(Convert.ToString(row["query_plan_text"])))
            {
                row["query_plan_text"] = RunningQueries.GetPlan(row);
            }
            Common.ShowQueryPlan((string)row["query_plan_text"]);
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

                Common.ShowQueryPlan(planText);
                setStatus(status, tooltip, statusColor);
            }
            catch (Exception ex)
            {
                setStatus(ex.Message, ex.ToString(), DashColors.Red);
            }
        }
    }
}
