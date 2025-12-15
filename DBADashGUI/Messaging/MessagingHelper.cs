using DBADash.Messaging;
using DBADashGUI.Interface;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.Messaging
{
    public class MessagingHelper
    {
        public delegate Task MessageCompletedDelegate(ResponseMessage reply, Guid messageGroup, SetStatusDelegate setStatus);

        public delegate Task MessageResponseDelegate(ResponseMessage reply, Guid messageGroup);

        public delegate void SetStatusDelegate(string message, string details, Color color);

        public static async Task SendMessageAndProcessReply(MessageBase message, DBADashContext context,
            ToolStripStatusLabel lblStatus, MessageCompletedDelegate processCompleted, Guid messageGroup)
        {
            await SendMessageAndProcessReply(message, context, lblStatus.InvokeSetStatus, processCompleted, messageGroup);
        }

        public static async Task SendMessageAndProcessReply(MessageBase message, DBADashContext context,
            SetStatusDelegate setStatus, MessageCompletedDelegate processCompleted, Guid messageGroup)
        {
            message.Id = messageGroup;
            if (context.ImportAgentID == null)
            {
                setStatus("No Import Agent", string.Empty, DashColors.Fail);
                return;
            }

            try
            {
                await MessageProcessing.SendMessageToService(message.Serialize(), (int)context.ImportAgentID,
                    messageGroup,
                    Common.ConnectionString, message.Lifetime);
            }
            catch (Exception ex)
            {
                setStatus(ex.Message, ex.ToString(), DashColors.Fail);
                return;
            }

            var completed = false;
            while (!completed)
            {
                ResponseMessage reply;
                try
                {
                    reply = await CollectionMessaging.ReceiveReply(messageGroup, message.Lifetime * 1000);
                }
                catch (Exception ex)
                {
                    setStatus(ex.Message, string.Empty, DashColors.Fail);
                    completed = true;
                    return;
                }

                switch (reply.Type)
                {
                    case ResponseMessage.ResponseTypes.Progress:
                        completed = false;
                        setStatus(reply.Message, string.Empty, DashColors.Information);
                        break;

                    case ResponseMessage.ResponseTypes.Failure:
                        completed = true;
                        setStatus(reply.Message, reply.Exception?.ToString(), DashColors.Fail);
                        await processCompleted(reply, messageGroup, setStatus);
                        break;

                    case ResponseMessage.ResponseTypes.Success:
                        completed = false; // It's done but wait for end dialog
                        await processCompleted(reply, messageGroup, setStatus);
                        break;

                    case ResponseMessage.ResponseTypes.EndConversation:
                        completed = true;
                        break;
                }
            }
        }

        public static async Task ForceQueryPlan(DBADashContext context, string db, QueryStorePlanForcingMessage.PlanForcingOperations operation, long queryId, long planId, string objectName, string text, byte[] queryHash, byte[] planHash, string notes, ToolStripStatusLabel lbl, MessagingHelper.MessageCompletedDelegate dCompletedDelegate)
        {
            if (!DBADashUser.AllowPlanForcing)
            {
                throw new Exception("User does not have permission to force/unforce plans");
            }
            var messageGroup = Guid.NewGuid();
            await LogPlanForcingOperation(context.InstanceID, db, operation.ToString(), queryId, planId, objectName, text, queryHash, planHash, notes, messageGroup);
            var message = new QueryStorePlanForcingMessage()
            {
                ConnectionID = context.ConnectionID,
                DatabaseName = db,
                QueryID = queryId,
                PlanID = planId,
                PlanForcingOperation = operation,
                CollectAgent = context.CollectAgent,
                ImportAgent = context.ImportAgent,
            };
            await MessagingHelper.SendMessageAndProcessReply(message, context, lbl, dCompletedDelegate, messageGroup);
        }

        private static async Task LogPlanForcingOperation(int InstanceID, string db, string type, long queryId, long planId, string objectName, string text, byte[] query_hash, byte[] plan_hash, string notes, Guid messageGroup)
        {
            await using var cn = new SqlConnection(Common.ConnectionString);
            var cmd = new SqlCommand("dbo.PlanForcingLog_Add", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@InstanceID", InstanceID);
            cmd.Parameters.AddWithValue("@log_type", type);
            cmd.Parameters.AddWithValue("@query_id", queryId);
            cmd.Parameters.AddWithValue("@plan_id", planId);
            cmd.Parameters.AddWithValue("@object_name", objectName);
            cmd.Parameters.AddWithValue("@query_sql_text", text);
            cmd.Parameters.AddWithValue("@query_hash", query_hash);
            cmd.Parameters.AddWithValue("@query_plan_hash", plan_hash);
            cmd.Parameters.AddWithValue("@user_name", Environment.UserName);
            cmd.Parameters.AddWithValue("@notes", notes);
            cmd.Parameters.AddWithValue("@database_name", db);
            cmd.Parameters.AddWithValue("@MessageGroupID", messageGroup);
            await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task UpdatePlanForcingLog(Guid messageGroup, string status)
        {
            await using var cn = new SqlConnection(Common.ConnectionString);
            var cmd = new SqlCommand("dbo.PlanForcingLog_Upd", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@MessageGroupID", messageGroup);
            cmd.Parameters.AddWithValue("@Status", status);
            await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task ForcePlanDrillDown(DataGridView _dgv, DataGridViewCellEventArgs e, DBADashContext context, ToolStripStatusLabel lbl, MessagingHelper.MessageCompletedDelegate dCompletedDelegate)
        {
            if (!DBADashUser.AllowPlanForcing)
            {
                MessageBox.Show(@"You do not have permission to force/unforce plans", @"Permission Denied", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }
            var queryID = Convert.ToInt64(_dgv.Rows[e.RowIndex].Cells["query_id"].Value);
            var planID = Convert.ToInt64(_dgv.Rows[e.RowIndex].Cells["plan_id"].Value);
            var db = Convert.ToString(_dgv.Rows[e.RowIndex].Cells["DB"].Value);
            var objectName = Convert.ToString(_dgv.Rows[e.RowIndex].Cells["object_name"].Value);
            var text = Convert.ToString(_dgv.Rows[e.RowIndex].Cells["query_sql_text"].Value);
            var queryHash = (byte[])_dgv.Rows[e.RowIndex].Cells["query_hash"].Value;
            var planHash = (byte[])_dgv.Rows[e.RowIndex].Cells["query_plan_hash"].Value;
            var forceOp =
                QueryStorePlanForcingMessage.PlanForcingOperations.Unforce;
            var notes = string.Empty;

            if (e.ColumnIndex == _dgv.Columns["Undo"]?.Index)
            {
                var logType = Convert.ToString(_dgv.Rows[e.RowIndex].Cells["log_type"].Value);
                forceOp = logType switch
                {
                    "Force" => QueryStorePlanForcingMessage.PlanForcingOperations.Unforce,
                    "Unforce" => QueryStorePlanForcingMessage.PlanForcingOperations.Force,
                    _ => throw new ArgumentOutOfRangeException($"Undo operation not supported for {logType}")
                };
                notes = forceOp switch
                {
                    QueryStorePlanForcingMessage.PlanForcingOperations.Force => "Undo (Re-Force plan)",
                    QueryStorePlanForcingMessage.PlanForcingOperations.Unforce => "Undo (Unforce plan)",
                    _ => "Undo"
                };
            }
            else if (e.ColumnIndex == _dgv.Columns["ForceUnForce"]?.Index)
            {
                forceOp = (string)_dgv.Rows[e.RowIndex].Cells["plan_forcing_type_desc"].Value == "NONE" ? QueryStorePlanForcingMessage.PlanForcingOperations.Force : QueryStorePlanForcingMessage.PlanForcingOperations.Unforce;
            }

            if (MessageBox.Show(@$"Are you sure you want to {forceOp} this plan?", @$"{forceOp} Plan", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            CommonShared.ShowInputDialog(ref notes, "Enter notes");

            await MessagingHelper.ForceQueryPlan(context, db, forceOp, queryID, planID, objectName, text, queryHash, planHash,
                notes, lbl, dCompletedDelegate);
        }
    }
}