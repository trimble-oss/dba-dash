using DBADash.Messaging;
using DBADashGUI.Interface;
using DBADashGUI.Messaging;
using System;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.CustomReports
{
    internal class PlanIdLinkColumnInfo : LinkColumnInfo
    {
        public string PlanIdColumn { get; set; }
        public string DatabaseNameColumn { get; set; }

        public override async void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
        {
            var status = sender as ISetStatus;

            if (row.DataGridView?.Columns[PlanIdColumn] == null)
            {
                status?.SetStatus($"Column '{PlanIdColumn}' not found", string.Empty, DashColors.Fail);
                return;
            }

            var planId = row.Cells[PlanIdColumn].Value.DBNullToNull() as long?;
            if (planId == null)
            {
                status?.SetStatus("Plan ID is null", string.Empty, DashColors.Fail);
                return;
            }

            var db = context.DatabaseName;
            if (!string.IsNullOrEmpty(DatabaseNameColumn))
            {
                if (row.DataGridView?.Columns[DatabaseNameColumn] == null)
                {
                    status?.SetStatus($"Column '{DatabaseNameColumn}' not found", string.Empty, DashColors.Fail);
                    return;
                }
                db = row.Cells[DatabaseNameColumn].Value.DBNullToNull() as string;
            }

            if (db == null)
            {
                status?.SetStatus("Database name is null", string.Empty, DashColors.Fail);
                return;
            }

            var message = new PlanCollectionMessage()
            {
                CollectAgent = context.CollectAgent,
                ImportAgent = context.ImportAgent,
                ConnectionID = context.ConnectionID,
                DatabaseName = db,
                PlanID = planId.Value,
            };

            MessagingHelper.SetStatusDelegate setStatusDelegate = (msg, details, color) => status?.SetStatus(msg, details, color);

            _ = Task.Run(async () =>
            {
                try
                {
                    await MessagingHelper.SendMessageAndProcessReply(message, context, setStatusDelegate, ProcessCompletedPlanCollectionMessage, Guid.NewGuid());
                }
                catch (Exception ex)
                {
                    status?.SetStatus($"Error collecting plan: {ex.Message}", ex.ToString(), DashColors.Fail);
                }
            });
        }

        private static Task ProcessCompletedPlanCollectionMessage(ResponseMessage reply, Guid messageGroup, MessagingHelper.SetStatusDelegate setStatus)
        {
            var ds = reply.Data;
            if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Columns.Count == 0)
            {
                setStatus?.Invoke("No data returned", string.Empty, DashColors.Warning);
                return Task.CompletedTask;
            }

            var dt = ds.Tables[0];
            LoadQueryPlan(dt, setStatus);
            return Task.CompletedTask;
        }

        private static void LoadQueryPlan(DataTable dt, MessagingHelper.SetStatusDelegate setStatus)
        {
            if (dt.Rows.Count == 0)
            {
                setStatus?.Invoke("No query plan", string.Empty, DashColors.Fail);
                return;
            }

            if (!dt.Columns.Contains("query_plan"))
            {
                setStatus?.Invoke("Response missing 'query_plan' column", string.Empty, DashColors.Fail);
                return;
            }

            var plan = dt.Rows[0]["query_plan"]?.ToString();
            if (string.IsNullOrWhiteSpace(plan))
            {
                setStatus?.Invoke("Query plan is empty", string.Empty, DashColors.Fail);
                return;
            }

            string fileName = null;
            if (dt.Columns.Contains("plan_id") && dt.Rows[0]["plan_id"] != DBNull.Value)
            {
                fileName = $"Plan_{dt.Rows[0]["plan_id"]}_{DateTime.Now:yyyyMMddHHmmss}.sqlplan";
            }
            else
            {
                fileName = $"Plan_{DateTime.Now:yyyyMMddHHmmss}.sqlplan";
            }

            setStatus?.Invoke("Loading Query Plan...", string.Empty, DashColors.Success);

            try
            {
                Common.ShowQueryPlan(plan, fileName);
                setStatus?.Invoke("Query plan loaded in associated app", string.Empty, DashColors.Success);
            }
            catch (Exception ex)
            {
                setStatus?.Invoke($"Failed to load query plan: {ex.Message}", ex.ToString(), DashColors.Fail);
            }
        }
    }
}