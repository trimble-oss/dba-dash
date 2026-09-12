using DBADash;
using DBADash.Messaging;
using DBADashGUI.Interface;
using DBADashGUI.Messaging;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static DBADashGUI.SchemaCompare.CodeEditor;

namespace DBADashGUI.CustomReports
{
    /// <summary>
    /// Opens the plan of a query stats row's plan shape: the one the collection captured with the statistics, or,
    /// where it captured none - past its cap, or with plan capture switched off - one fetched from the plan cache on
    /// the monitored instance, through messaging, and kept so it is there the next time.  Without messaging, a script
    /// that fetches it.
    ///
    /// For an ad hoc shape the plan is an example: every variant under the plan shape runs its operators, but its
    /// statement text and literal values are one variant's, so it opens titled as one.
    ///
    /// Used as a synthetic column (no matching column in the result), so the link shows its own header text on every
    /// row.  A row that ran under several plan shapes, or a rollup, has none to open.
    /// </summary>
    public class QueryStatsPlanLinkColumnInfo : LinkColumnInfo
    {
        public string StatementIDColumn { get; set; } = "PlanStatementID";

        public string PlanHashColumn { get; set; } = "PlanHash";

        /// <summary>What dbo.QueryStatsPlan_Get says about the statement, and its plan where one was captured.</summary>
        private sealed class StatementPlan
        {
            public int InstanceID;
            public bool IsExample;
            public string DatabaseName;
            public byte[] SqlHandle;
            public int StartOffset;
            public int EndOffset;
            public byte[] QueryHash;
            public byte[] Plan;
        }

        public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
        {
            if (row.DataGridView == null
                || !row.DataGridView.Columns.Contains(StatementIDColumn)
                || !row.DataGridView.Columns.Contains(PlanHashColumn)) return;
            if (row.Cells[StatementIDColumn].Value.DBNullToNull() is not long statementId) return;
            if (row.Cells[PlanHashColumn].Value.DBNullToNull() is not string planHashText) return;
            var planHash = planHashText.HexStringToByteArray();
            if (planHash is not { Length: 8 }) return;

            var status = sender as ISetStatus;
            StatementPlan statement;
            var cursor = sender?.Cursor;
            try
            {
                if (sender != null) sender.Cursor = Cursors.WaitCursor;
                statement = FetchStatementPlan(statementId, planHash);
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
                return;
            }
            finally
            {
                if (sender != null) sender.Cursor = cursor;
            }

            if (statement == null)
            {
                MessageBox.Show("This statement is no longer in the repository.", "Plan", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            // The row's own instance rather than the report's, which can span many.  Not found only for an instance
            // the GUI no longer lists, which can still show a captured plan but can't be asked for one.
            var instanceContext = CommonData.Instances?.Rows.Cast<DataRow>().Any(r => r.Field<int>("InstanceID") == statement.InstanceID) == true
                ? CommonData.GetDBADashContext(statement.InstanceID)
                : null;
            if (statement.Plan != null)
            {
                try
                {
                    ShowPlan(SMOBaseClass.Unzip(statement.Plan), statement, planHashText, instanceContext ?? context);
                }
                catch (Exception ex)
                {
                    CommonShared.ShowExceptionDialog(ex);
                }
                return;
            }

            if (instanceContext == null)
            {
                MessageBox.Show("No plan was captured for this plan shape, and its instance is no longer listed, so it can't be fetched.",
                    "Plan", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!instanceContext.CanMessage)
            {
                Common.ShowCodeViewer(GetFindPlanScript(statement, planHashText, instanceContext.InstanceName),
                    "Find Plan", CodeEditorModes.SQL);
                return;
            }

            var message = new QueryStatsPlanMessage
            {
                ConnectionID = instanceContext.ConnectionID,
                CollectAgent = instanceContext.CollectAgent,
                ImportAgent = instanceContext.ImportAgent,
                QueryPlanHash = planHash,
                // A shape has a handle per variant, so it is found by its query hash instead
                SqlHandle = statement.IsExample ? null : statement.SqlHandle,
                StatementStart = statement.StartOffset,
                StatementEnd = statement.EndOffset,
                QueryHash = statement.IsExample ? statement.QueryHash : null,
                DatabaseName = statement.DatabaseName
            };
            MessagingHelper.SetStatusDelegate setStatus = (msg, details, color) => status?.SetStatus(msg, details, color);
            setStatus("Fetching the plan from the plan cache...", string.Empty, DashColors.Information);

            _ = Task.Run(async () =>
            {
                try
                {
                    await MessagingHelper.SendMessageAndProcessReply(message, instanceContext, setStatus,
                        (reply, _, set) => ProcessReply(reply, set, statementId, planHash, planHashText, statement, instanceContext),
                        Guid.NewGuid());
                }
                catch (Exception ex)
                {
                    setStatus($"Error fetching the plan: {ex.Message}", ex.ToString(), DashColors.Fail);
                }
            });
        }

        private static Task ProcessReply(ResponseMessage reply, MessagingHelper.SetStatusDelegate setStatus,
            long statementId, byte[] planHash, string planHashText, StatementPlan statement, DBADashContext context)
        {
            if (reply.Type != ResponseMessage.ResponseTypes.Success)
            {
                setStatus(reply.Message, reply.Exception?.ToString(), DashColors.Fail);
                return Task.CompletedTask;
            }

            var dt = reply.Data?.Tables.Contains("QueryPlans") == true ? reply.Data.Tables["QueryPlans"] : null;
            var found = dt is { Rows.Count: > 0 } && dt.Columns.Contains("query_plan_compressed") ? dt.Rows[0] : null;
            if (found?["query_plan_compressed"] is not byte[] compressed)
            {
                setStatus("The plan is no longer in the plan cache.  Query Store may still have it, if it is enabled for the database: click the Plan Hash.",
                    string.Empty, DashColors.Warning);
                return Task.CompletedTask;
            }
            // Read from the plan itself, so a different hash is an entry that recompiled to another shape in between
            if (found["query_plan_hash"] is not byte[] hash || !hash.SequenceEqual(planHash))
            {
                setStatus("The plan in cache has changed to a different plan shape.  Query Store may still have this one: click the Plan Hash.",
                    string.Empty, DashColors.Warning);
                return Task.CompletedTask;
            }

            var message = "Plan fetched from the plan cache";
            var details = string.Empty;
            var color = DashColors.Success;
            try
            {
                SavePlan(statementId, planHash, compressed);
            }
            catch (Exception ex)
            {
                // A read only user can look at it, but can't keep it
                message = "Plan fetched from the plan cache, but it could not be saved to the repository database";
                details = ex.Message;
                color = DashColors.Warning;
            }

            try
            {
                ShowPlan(SMOBaseClass.Unzip(compressed), statement, planHashText, context);
                setStatus(message, details, color);
            }
            catch (Exception ex)
            {
                setStatus($"Failed to load the plan: {ex.Message}", ex.ToString(), DashColors.Fail);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// A shape's plan opens titled as an example, because its own title would be its statement text - one
        /// variant's, with that variant's literal values.
        /// </summary>
        private static void ShowPlan(string plan, StatementPlan statement, string planHashText, DBADashContext context) =>
            Common.ShowQueryPlan(plan, statement.IsExample ? $"Example plan {planHashText}.sqlplan" : null, context);

        private static StatementPlan FetchStatementPlan(long statementId, byte[] planHash)
        {
            using var connection = new SqlConnection(Common.ConnectionString);
            using var command = new SqlCommand("dbo.QueryStatsPlan_Get", connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 60
            };
            command.Parameters.Add("StatementID", SqlDbType.BigInt).Value = statementId;
            command.Parameters.Add("query_plan_hash", SqlDbType.Binary, 8).Value = planHash;
            connection.Open();
            using var reader = command.ExecuteReader();
            if (!reader.Read()) return null;
            return new StatementPlan
            {
                InstanceID = (int)reader["InstanceID"],
                IsExample = reader["IsExample"] is true,
                DatabaseName = reader["DatabaseName"] as string,
                SqlHandle = reader["sql_handle"] as byte[],
                StartOffset = (int)reader["statement_start_offset"],
                EndOffset = (int)reader["statement_end_offset"],
                QueryHash = reader["query_hash"] as byte[],
                Plan = reader["query_plan_compressed"] as byte[]
            };
        }

        private static void SavePlan(long statementId, byte[] planHash, byte[] compressed)
        {
            using var connection = new SqlConnection(Common.ConnectionString);
            using var command = new SqlCommand("dbo.QueryStatsPlan_Add", connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 60
            };
            command.Parameters.Add("StatementID", SqlDbType.BigInt).Value = statementId;
            command.Parameters.Add("query_plan_hash", SqlDbType.Binary, 8).Value = planHash;
            command.Parameters.Add("query_plan_compressed", SqlDbType.VarBinary, -1).Value = compressed;
            connection.Open();
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// A script that fetches the plan from the plan cache, for an instance DBA Dash can't send messages to: the
        /// lookup the messaging path makes - see SQLQueryStatsPlan.sql - with the plan itself on the same row.
        /// </summary>
        private static string GetFindPlanScript(StatementPlan statement, string planHashText, string instanceName)
        {
            // Every value is a hash, a handle or a number, apart from the names, which are quoted for where they go
            var instance = (instanceName ?? string.Empty).Replace("*", "+"); // Can't end the comment early
            var lookup = statement.IsExample
                ? "AND qs.query_hash = @QueryHash"
                : "AND qs.sql_handle = @SqlHandle\r\n\tAND qs.statement_start_offset = @StatementStart\r\n\tAND qs.statement_end_offset = @StatementEnd";
            var database = statement.DatabaseName == null
                ? string.Empty
                : "\r\n\tAND EXISTS (SELECT 1\r\n\t\t\t\tFROM sys.dm_exec_plan_attributes(qs.plan_handle) pa\r\n\t\t\t\tWHERE pa.attribute = 'dbid'\r\n\t\t\t\tAND CONVERT(INT, pa.value) = DB_ID())";
            var from = $@"FROM sys.dm_exec_query_stats qs
	CROSS APPLY sys.dm_exec_text_query_plan(qs.plan_handle, qs.statement_start_offset, qs.statement_end_offset) qp
	WHERE qs.query_plan_hash = @QueryPlanHash
	{lookup}{database}
	ORDER BY qs.last_execution_time DESC";

            var sb = new StringBuilder();
            sb.AppendLine("/*");
            sb.AppendLine($"\tRun on {instance} to get the plan of this query stats row from the plan cache.  Messaging isn't");
            sb.AppendLine("\tenabled for the instance, so DBA Dash can't fetch it for you.");
            if (statement.IsExample)
            {
                sb.AppendLine();
                sb.AppendLine("\tThe row is an ad hoc query shape, so this is the plan of whichever of its texts ran most recently under the");
                sb.AppendLine("\tplan shape: its operators are every variant's, its statement text and literal values only that one's.");
            }
            sb.AppendLine();
            sb.AppendLine("\tNo rows means the plan is no longer cached.  Query Store may still have it, if it is enabled for the");
            sb.AppendLine("\tdatabase: the Plan Hash link in the grid looks there.");
            sb.AppendLine("*/");
            if (statement.DatabaseName != null)
            {
                sb.AppendLine($"USE [{statement.DatabaseName.Replace("]", "]]")}];");
                sb.AppendLine();
            }
            sb.AppendLine($"DECLARE @QueryPlanHash BINARY(8) = {planHashText};");
            if (statement.IsExample)
            {
                sb.AppendLine($"DECLARE @QueryHash BINARY(8) = {statement.QueryHash?.ToHexString(true) ?? "NULL"};");
            }
            else
            {
                sb.AppendLine($"DECLARE @SqlHandle VARBINARY(64) = {statement.SqlHandle?.ToHexString(true) ?? "NULL"};");
                sb.AppendLine($"DECLARE @StatementStart INT = {statement.StartOffset};");
                sb.AppendLine($"DECLARE @StatementEnd INT = {statement.EndOffset};");
            }
            sb.AppendLine();
            sb.AppendLine("BEGIN TRY");
            sb.AppendLine("\tSELECT TOP (1) CONVERT(XML, qp.query_plan) AS query_plan, qs.last_execution_time");
            sb.AppendLine("\t" + from);
            sb.AppendLine("END TRY");
            sb.AppendLine("BEGIN CATCH");
            sb.AppendLine("\t/* A plan nested too deeply for the XML type: save the text as a .sqlplan file to open it */");
            sb.AppendLine("\tSELECT TOP (1) qp.query_plan, qs.last_execution_time");
            sb.AppendLine("\t" + from);
            sb.AppendLine("END CATCH");
            return sb.ToString();
        }
    }
}
