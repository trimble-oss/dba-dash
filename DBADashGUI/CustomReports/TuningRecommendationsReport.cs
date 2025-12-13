using DBADash.Messaging;
using DBADashGUI.CommunityTools;
using DBADashGUI.Messaging;
using DBADashGUI.SchemaCompare;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.CustomReports
{
    internal class TuningRecommendationsReport : CustomReportView
    {
        public TuningRecommendationsReport()
        {
            PostGridRefresh += TuningRecommendationsReport_PostGridRefresh;
            Report = Instance;
        }

        private const string FIX_PLAN_COLUMN_NAME = "colFixPlan";

        private void TuningRecommendationsReport_PostGridRefresh(object sender, EventArgs e)
        {
            SetGridSortOrderToMatchParam();
            AddPlanForcingColumn();
        }

        private void SetGridSortOrderToMatchParam()
        {
            if (Grids.Count == 0) return;
            var sort = Convert.ToString(customParams.FirstOrDefault(p => p.Param.ParameterName == "@Sort")?.Param?.Value);
            var sortDesc = Convert.ToBoolean(customParams.FirstOrDefault(p => p.Param.ParameterName == "@SortDesc")?.Param?.Value);
            var sortCol = Grids[0].Columns.Cast<DataGridViewColumn>().FirstOrDefault(c => c.DataPropertyName == sort);
            if (sortCol != null)
            {
                Grids[0].Sort(sortCol, sortDesc ? System.ComponentModel.ListSortDirection.Descending : System.ComponentModel.ListSortDirection.Ascending);
            }
        }

        private void AddPlanForcingColumn()
        {
            if (Grids.Count == 0) return;
            if (Grids[0].Columns[FIX_PLAN_COLUMN_NAME] != null) return;
            if (DBADashUser.AllowPlanForcing == false) return;
            Grids[0].Columns.Insert(0,
                new DataGridViewLinkColumn()
                {
                    Name = FIX_PLAN_COLUMN_NAME,
                    HeaderText = "Force Plan",
                    ToolTipText = "Click link to force or unforce recommended query plan",
                    Text = "Force Plan",
                    UseColumnTextForLinkValue = false
                });
            Grids[0].CellFormatting += TuningRecommendationsReport_CellFormatting;
            Grids[0].CellContentClick += TuningRecommendationsReport_CellContentClick;
        }

        private async void TuningRecommendationsReport_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            try
            {
                var row = Grids[0].Rows[e.RowIndex];
                var reason = Convert.ToString(row.Cells["reason"].Value);
                var name = Convert.ToString(row.Cells["name"].Value);
                var queryID = Convert.ToInt64(row.Cells["query_id"].Value);
                var planID = Convert.ToInt64(row.Cells["recommended_plan_id"].Value);
                var regressedPlanID = Convert.ToInt64(row.Cells["regressed_plan_id"].Value);
                var db = Convert.ToString(row.Cells["DB"].Value);
                var objectName = Convert.ToString(row.Cells["object_name"].Value);
                var text = Convert.ToString(row.Cells["query_sql_text"].Value);
                var queryHash = (byte[])(row.Cells["query_hash"].Value.DBNullToNull());
                var planHash = (byte[])(row.Cells["recommended_plan_hash"].Value.DBNullToNull());
                if (Grids[0].Columns[e.ColumnIndex] == Grids[0].Columns[FIX_PLAN_COLUMN_NAME])
                {
                    string notes;
                    QueryStorePlanForcingMessage.PlanForcingOperations forceOp;
                    if ((bool?)(row.Cells["is_revertable_action"].Value.DBNullToNull()) == true)
                    {
                        notes = $"Revert tuning recommendation {name}. Regressed Plan: {regressedPlanID}.  Recommended plan {planID}. {reason}";
                        forceOp = QueryStorePlanForcingMessage.PlanForcingOperations.Unforce;
                    }
                    else
                    {
                        notes = $"Apply tuning recommendation {name}. Regressed Plan: {regressedPlanID}.  Recommended plan {planID}. {reason}";
                        forceOp = QueryStorePlanForcingMessage.PlanForcingOperations.Force;
                    }
                    CommonShared.ShowInputDialog(ref notes, "Enter notes");
                    await MessagingHelper.ForceQueryPlan(context, db, forceOp, queryID, planID, objectName, text, queryHash, planHash,
                        notes, StatusLabel, ForcePlanReply);
                }
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }
        }

        private async Task ForcePlanReply(ResponseMessage reply, Guid messageGroup)
        {
            if (reply.Type == ResponseMessage.ResponseTypes.Success)
            {
                await MessagingHelper.UpdatePlanForcingLog(messageGroup, "SUCCEEDED");
                StatusLabel.InvokeSetStatus("Plan Forcing Operation Completed", string.Empty, DashColors.Success);
                if (MessageBox.Show("Plan forcing operation succeeded. Do you want to refresh the report now?", "Refresh Report", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    RefreshData();
                }
            }
            else
            {
                StatusLabel.InvokeSetStatus("Plan Forcing Operation Failed", reply.Message, DashColors.Fail);
                await MessagingHelper.UpdatePlanForcingLog(messageGroup, "FAIL:" + reply.Message);
            }
        }

        private void TuningRecommendationsReport_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex < 0 || e.RowIndex < 0) return;

            var grid = (DataGridView)sender;
            if (grid.Columns[e.ColumnIndex].Name != FIX_PLAN_COLUMN_NAME) return;

            var row = grid.Rows[e.RowIndex];

            if (!string.Equals(Convert.ToString(row.Cells["type"].Value), "FORCE_LAST_GOOD_PLAN", StringComparison.OrdinalIgnoreCase))
            {
                e.Value = "";
            }
            else if ((bool?)(row.Cells["is_revertable_action"].Value.DBNullToNull()) == true)
            {
                e.Value = "Unforce Plan";
            }
            else
            {
                e.Value = "Force Plan";
            }

            e.FormattingApplied = true;
        }

        public static SystemDirectExecutionReport Instance => new()
        {
            EmbeddedScript = ProcedureExecutionMessage.EmbeddedScripts.TuningRecommendations,
            ReportName = "Tuning Recommendations",
            ProcedureName = ProcedureExecutionMessage.EmbeddedScripts.TuningRecommendations.ToString(),
            DatabaseNameParameter = "@DB",
            TriggerCollectionTypes = new List<string>(),
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                {
                    0, new CustomReportResult
                    {
                        ResultName = "Result1",
                        Columns = new Dictionary<string, ColumnMetadata>
                        {
                            { "DB", new ColumnMetadata() { Alias = "DB", Description = "Database name", Visible = true } },
                            { "query_id", new ColumnMetadata
                                {
                                    Visible = true,
                                    Alias = "Query\nID",
                                    Description = "Query store Query ID",
                                    Link = new QueryStoreLinkColumnInfo()
                                    {
                                        TargetColumn = "query_id",
                                        TargetColumnLinkType = QueryStoreLinkColumnInfo.QueryStoreLinkColumnType.QueryID,
                                        DatabaseNameColumn = "DB"
                                    }
                                }
                            },
                            { "query_sql_text", new ColumnMetadata
                                {
                                    Alias = "Text",
                                    Visible = true,
                                    Description = "Query Text",
                                    Link = new TextLinkColumnInfo
                                    {
                                        TargetColumn = "query_sql_text",
                                        TextHandling = CodeEditor.CodeEditorModes.SQL
                                    },
                                }
                            },
                            { "name", new ColumnMetadata { Visible = true, Alias = "Name", Description = "Unique name of recommendation" } },
                            { "type", new ColumnMetadata { Visible = true, Alias = "Type", Description = "The name of the automatic tuning option that produced the recommendation. e.g. FORCE_LAST_GOOD_PLAN" } },
                            { "object_name", new ColumnMetadata
                                {
                                    Visible = true,
                                    Alias = "Object\nName",
                                    Description = "Name of the associated database object (stored procedure name)",
                                    Link = new QueryStoreLinkColumnInfo()
                                    {
                                        TargetColumn = "object_name",
                                        TargetColumnLinkType = QueryStoreLinkColumnInfo.QueryStoreLinkColumnType.ObjectName,
                                        DatabaseNameColumn = "DB"
                                    }
                                }
                            },
                            { "reason", new ColumnMetadata { Visible = true, Alias = "Reason", Description = "Reason why this recommendation was provided" } },
                            { "score", new ColumnMetadata { Visible = true, Alias = "Score", Description = "Estimated value for this recommendation. (0-100 scale)" } },
                            { "regressed_plan_id", new ColumnMetadata
                                {
                                    Visible = true,
                                    Alias = "Regressed\nPlan\nID",
                                    Description = "Query store plan ID for the regressed plan",
                                    Link = new QueryStoreLinkColumnInfo()
                                    {
                                        TargetColumn = "regressed_plan_id",
                                        TargetColumnLinkType = QueryStoreLinkColumnInfo.QueryStoreLinkColumnType.PlanID,
                                        DatabaseNameColumn = "DB"
                                    }
                                }
                            },
                            { "recommended_plan_id", new ColumnMetadata
                                {
                                    Visible = true,
                                    Alias = "Recommended\nPlan\nID",
                                    Description = "Query store plan ID for the recommended plan",
                                    Link = new QueryStoreLinkColumnInfo()
                                    {
                                        TargetColumn = "recommended_plan_id",
                                        TargetColumnLinkType = QueryStoreLinkColumnInfo.QueryStoreLinkColumnType.PlanID,
                                        DatabaseNameColumn = "DB"
                                    }
                                }
                            },
                            { "last_execution_time", new ColumnMetadata { Visible = true, Alias = "Last\nExecution\nTime", Description = "Last execution time for the query (from sys.query_store_query)" } },
                            { "regressed_plan_last_execution_time", new ColumnMetadata { Visible = true, Alias = "Regressed\nPlan\nLast\nExecution\nTime", Description = "Last execution time for the regressed plan (from sys.query_store_plan)" }  },
                            { "recommended_plan_last_execution_time", new ColumnMetadata { Visible = true, Alias = "Recommended\nPlan\nLast\nExecution\nTime", Description = "Last execution time for the recommended plan (from sys.query_store_plan)" } },
                            { "current_state", new ColumnMetadata { Visible = true, Alias = "Current\nState", Description = "Current state of the recommendation" } },
                            { "current_state_reason", new ColumnMetadata { Visible = true, Alias = "Current\nState\nReason", Description = "Description of why the recommendation is in the current state" } },
                            { "script", new ColumnMetadata
                                {
                                    Visible = true,
                                    Alias = "Script",
                                    Link = new TextLinkColumnInfo { TargetColumn = "script", TextHandling = CodeEditor.CodeEditorModes.SQL }
                                }
                            },
                            { "estimated_gain", new ColumnMetadata { Visible = true, Alias = "Estimated\nGain", FormatString = "N3", Description = "The difference in execution times between the regressed plan and recommended plan, multiplied by the sum of executions and converted to seconds" } },
                            { "error_prone", new ColumnMetadata { Visible = true, Alias = "Error\nProne", Description = "'Yes' if regressed_plan_error_count > recommended_plan_error_count" } },
                            { "regressed_plan_execution_count", new ColumnMetadata { Visible = true, Alias = "Regressed\nPlan\nExecution\nCount", Description = "Number of executions of the query with regressed plan before the regression is detected" } },
                            { "recommended_plan_execution_count", new ColumnMetadata { Visible = true, Alias = "Recommended\nPlan\nExecution\nCount", Description = "Number of executions of the query with the recommended plan before the regression is detected." } },
                            { "regressed_plan_cpu_time_average", new ColumnMetadata { Visible = true, Alias = "Regressed\nPlan\nAvg\nCPU\nTime", Description = "Average CPU time (in microseconds µs) consumed by the regressed query plan (calculated before the regression is detected)" } },
                            { "recommended_plan_cpu_time_average", new ColumnMetadata { Visible = true, Alias = "Recommended\nPlan\nAvg\nCPU\nTime", Description = "Average CPU time (in microseconds µs) consumed by the recommended query plan (calculated before the regression is detected)."  } },
                            { "is_executable_action", new ColumnMetadata { Visible = true, Alias = "Is\nExecutable\nAction", Description = "Indicates if the recommendation can be executed via a T-SQL script" } },
                            { "is_revertable_action", new ColumnMetadata { Visible = true, Alias = "Is\nRevertable\nAction", Description = "Indicates if the recommendation can be automatically monitored and reverted by the DB engine." } },
                            { "valid_since", new ColumnMetadata { Visible = true, Alias = "Valid\nSince", Description = "The first time this recommendation was generated" } },
                            { "last_refresh", new ColumnMetadata { Visible = true, Alias = "Last\nRefresh", Description = "The last time this recommendation was generated" } },
                            { "execute_action_start_time", new ColumnMetadata { Visible = true, Alias = "Execute\nAction\nStart\nTime", Description = "Date the recommendation is applied" } },
                            { "execute_action_duration", new ColumnMetadata { Visible = true, Alias = "Execute\nAction\nDuration", Description = "Duration of the execute action." } },
                            { "execute_action_initiated_by", new ColumnMetadata { Visible = true, Alias = "Execute\nAction\nInitiated\nBy", Description = "User = User manually applied the recommendation.\nSystem = System automatically applied the recommendation" } },
                            { "execute_action_initiated_time", new ColumnMetadata { Visible = true, Alias = "Execute\nAction\nInitiated\nTime", Description = "Date the recommendation was applied" } },
                            { "revert_action_start_time", new ColumnMetadata { Visible = true, Alias = "Revert\nAction\nStart\nTime", Description = "Date the recommendation was reverted" } },
                            { "revert_action_duration", new ColumnMetadata { Visible = true, Alias = "Revert\nAction\nDuration", Description = "Duration of the revert action" } },
                            { "revert_action_initiated_by", new ColumnMetadata { Visible = true, Alias = "Revert\nAction\nInitiated\nBy",Description = "User = User manually reverted the recommendation.\nSystem = System automatically reverted the recommendation" } },
                            { "revert_action_initiated_time", new ColumnMetadata { Visible = true, Alias = "Revert\nAction\nInitiated\nTime", Description ="Date the recommendation was reverted" } },
                            { "query_hash", new ColumnMetadata
                                {
                                    Visible=true,
                                    Alias = "Query\nHash",
                                    Description = "Query store query hash",
                                    Link = new QueryStoreLinkColumnInfo()
                                    {
                                        TargetColumn = "query_hash",
                                        TargetColumnLinkType = QueryStoreLinkColumnInfo.QueryStoreLinkColumnType.QueryHash,
                                        DatabaseNameColumn = "DB"
                                    }
                                }
                            },
                            { "recommended_plan_hash", new ColumnMetadata
                                {
                                    Visible=true,
                                    Alias = "Recommended\nPlan\nHash",
                                    Description = "Query store plan hash for the recommended plan",
                                    Link = new QueryStoreLinkColumnInfo()
                                    {
                                        TargetColumn = "recommended_plan_hash",
                                        TargetColumnLinkType = QueryStoreLinkColumnInfo.QueryStoreLinkColumnType.PlanHash,
                                        DatabaseNameColumn = "DB"
                                    }
                                }
                            },
                            { "regressed_plan_hash", new ColumnMetadata
                                {
                                    Visible=true,
                                    Alias = "Regressed\nPlan\nHash",
                                    Description = "Query store plan hash for the regressed plan",
                                    Link = new QueryStoreLinkColumnInfo()
                                    {
                                        TargetColumn = "regressed_plan_hash",
                                        TargetColumnLinkType = QueryStoreLinkColumnInfo.QueryStoreLinkColumnType.PlanHash,
                                        DatabaseNameColumn = "DB"
                                    }
                                }
                            }
                        }
                    }
                }
            },
            Pickers = new List<Picker>
            {
                new()
                {
                    ParameterName = "@TOP",
                    Name = "TOP",
                    PickerItems = new Dictionary<object, string>
                    {
                        { 10, "10" },
                        { 20, "20" },
                        { 50, "50" },
                        { 100, "100" },
                        { 200, "200" },
                        { 500, "500" }
                    },
                    DefaultValue = 20,
                    MenuBar = true,
                    DataType = typeof(int)
                },
                new()
                {
                    ParameterName = "@Sort",
                    Name = "Sort",
                    PickerItems = new Dictionary<object, string>
                    {
                        { "estimated_gain", "Estimated Gain" },
                        { "score", "Score" },
                        { "regressed_plan_cpu_time_average", "Regressed Plan Avg CPU Time" },
                        { "recommended_plan_cpu_time_average", "Recommended Plan Avg CPU Time" },
                        { "regressed_plan_execution_count", "Regressed Plan Executions" },
                        { "recommended_plan_execution_count", "Recommended Plan Executions" },
                        { "last_execution_time", "Last Execution Time" },
                        { "regressed_plan_last_execution_time", "Regressed Plan Last Execution Time" },
                        { "recommended_plan_last_execution_time","Recommended Plan Last Execution Time" }
                    },
                    DefaultValue = "estimated_gain",
                    MenuBar = true,
                    DataType = typeof(string)
                },
                new()
                {
                    ParameterName = "@SortDesc",
                    Name = "Sort Direction",
                    PickerItems = new Dictionary<object, string>
                    {
                        { false, "Ascending" },
                        { true, "Descending" },
                    },
                    DefaultValue = true,
                    MenuBar = true,
                    DataType = typeof(bool)
                },
                new()
                {
                    ParameterName = "@CurrentState",
                    Name = "State",
                    PickerItems = new Dictionary<object, string>
                    {
                        {DBNull.Value, "ALL" },
                        { "Active", "Active" },
                        { "Verifying", "Verifying" },
                        { "Success", "Success" },
                        { "Reverted", "Reverted" },
                        { "Expired", "Expired" },
                    },
                    DefaultValue = DBNull.Value,
                    MenuBar = true,
                    DataType = typeof(string)
                },
                new()
                {
                    ParameterName = "@CurrentStateReason",
                    Name = "State Reason",
                    PickerItems = new Dictionary<object, string>
                    {
                        { DBNull.Value, "ALL" },
                        { "SchemaChanged", "SchemaChanged" },
                        { "StatisticsChanged", "StatisticsChanged" },
                        { "AutomaticTuningOptionDisabled", "AutomaticTuningOptionDisabled" },
                        { "UnsupportedStatementType", "UnsupportedStatementType" },
                        { "LastGoodPlanForced", "LastGoodPlanForced" },
                        { "AutomaticTuningOptionNotEnabled", "AutomaticTuningOptionNotEnabled" },
                        { "VerificationAborted", "VerificationAborted" },
                        { "VerificationForcedQueryRecompile", "VerificationForcedQueryRecompile" },
                        { "PlanForcedByUser", "PlanForcedByUser" },
                        { "PlanUnforcedByUser", "PlanUnforcedByUser" },
                    },
                    DefaultValue = DBNull.Value,
                    MenuBar = true,
                    DataType = typeof(string)
                },
                new()
                {
                    ParameterName = "@MinScore",
                    Name = "Min Score",
                    PickerItems = new Dictionary<object, string>
                    {
                        {0, "0" },
                        { 10, "10" },
                        { 20, "20" },
                        { 30, "30" },
                        { 40, "40" },
                        { 50, "50" },
                        { 60, "60" },
                        { 70, "70" },
                        { 80, "80" },
                        { 90, "90" },
                        { 100,"100" }
                    },
                    DefaultValue = 0,
                    MenuBar = true,
                    DataType = typeof(int)
                },
            },
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new()
                    {
                        ParamName = "@Sort",
                        ParamType = "VARCHAR"
                    },
                    new()
                    {
                        ParamName = "@TOP",
                        ParamType = "INT",
                    },
                    new()
                    {
                        ParamName = "@SortDesc",
                        ParamType = "BIT"
                    },
                    new()
                    {
                        ParamName = "@DB",
                        ParamType = "NVARCHAR",
                    },
                    new()
                    {
                        ParamName = "@CurrentState",
                        ParamType = "VARCHAR",
                    },
                    new()
                    {
                        ParamName = "@CurrentStateReason",
                        ParamType = "VARCHAR",
                    },
                    new()
                    {
                        ParamName = "@MinScore",
                        ParamType = "INT",
                    }
                },
            },
        };
    }
}