using DBADash;
using DBADashGUI.CustomReports;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using static DBADashGUI.DBADashStatus;

namespace DBADashGUI.ElasticJobs
{
    /// <summary>
    /// Execution history of an elastic job (dbo.ElasticJobHistory_Get) - the counterpart of the SQL Agent job history report.
    /// One row per job execution by default, with a link to its executions against each target.
    /// </summary>
    internal static class ElasticJobHistoryReport
    {
        private static CellHighlightingRuleSet RunStatusHighlight() => new("run_status_description")
        {
            Rules = new List<CellHighlightingRule>
            {
                new() { ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "Succeeded", Status = DBADashStatusEnum.OK },
                new() { ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "Failed", Status = DBADashStatusEnum.Critical },
                new() { ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "Retry", Status = DBADashStatusEnum.Warning },
                new() { ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "Canceled", Status = DBADashStatusEnum.Warning },
            }
        };

        private sealed class MessageLink : LinkColumnInfo
        {
            public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
            {
                if (row.DataBoundItem is not DataRowView drv) return;
                Convert.ToString(drv["last_message"]).OpenAsTextFile();
            }
        }

        /// <summary>The executions of the job execution's steps against each target</summary>
        private sealed class ViewTargetsLink : LinkColumnInfo
        {
            public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
            {
                if (row.DataBoundItem is not DataRowView drv) return;
                ShowExecution(context, (int)drv["InstanceID"], (Guid)drv["job_id"], (Guid)drv["job_execution_id"], sender);
            }
        }

        /// <summary>
        /// Show the executions of a job execution's steps against each target.  In place of the report in <paramref name="sender"/>
        /// where it's a report view (Ctrl+click for a new window) - otherwise in a new window.
        /// </summary>
        internal static void ShowExecution(DBADashContext context, int instanceId, Guid jobId, Guid jobExecutionId, ContainerControl sender)
        {
            var report = TargetsInstance;
            var newContext = (DBADashContext)context.Clone();
            newContext.Report = report;
            var customParams = report.GetCustomSqlParameters();
            SetParam(customParams, "@InstanceID", instanceId);
            SetParam(customParams, "@JobID", jobId);
            SetParam(customParams, "@job_execution_id", jobExecutionId);

            if (Control.ModifierKeys.HasFlag(Keys.Control) || sender is not CustomReportView view)
            {
                var viewer = new CustomReportViewer { Context = newContext, CustomParams = customParams };
                viewer.ShowSingleInstance();
            }
            else
            {
                view.PushNavigationState();
                view.Report = report;
                _ = view.SetContext(newContext, customParams);
            }
        }

        /// <summary>Show the execution history of a job in a new window</summary>
        internal static void ShowHistory(DBADashContext context, int instanceId, Guid jobId)
        {
            var report = Instance;
            var newContext = (DBADashContext)context.Clone();
            newContext.Report = report;
            var customParams = report.GetCustomSqlParameters();
            SetParam(customParams, "@InstanceID", instanceId);
            SetParam(customParams, "@JobID", jobId);
            var viewer = new CustomReportViewer { Context = newContext, CustomParams = customParams };
            viewer.ShowSingleInstance();
        }

        private static void SetParam(List<CustomSqlParameter> customParams, string name, object value)
        {
            var p = customParams.FirstOrDefault(p => p.Param.ParameterName.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (p == null) return;
            p.Param.Value = value;
            p.UseDefaultValue = false;
        }

        private static ColumnMetadata Hidden(int displayIndex) => new() { Visible = false, DisplayIndex = displayIndex };

        private static Dictionary<string, ColumnMetadata> GetColumns(bool includeViewTargets)
        {
            var columns = new Dictionary<string, ColumnMetadata>();
            var i = 0;
            if (includeViewTargets)
            {
                columns["Targets"] = new() { DisplayIndex = i++, Alias = "Targets", Link = new ViewTargetsLink(), Description = "View the executions of the job's steps against each target" };
            }
            columns["create_time"] = new() { DisplayIndex = i++, Alias = "Created" };
            columns["start_time"] = new() { DisplayIndex = i++, Alias = "Start" };
            columns["end_time"] = new() { DisplayIndex = i++, Alias = "End" };
            columns["step_id"] = new() { DisplayIndex = i++, Alias = "Step ID" };
            columns["step_name"] = new() { DisplayIndex = i++, Alias = "Step Name" };
            columns["target_type"] = new() { DisplayIndex = i++, Alias = "Target Type" };
            columns["target_group_name"] = new() { DisplayIndex = i++, Alias = "Target Group" };
            columns["target_server_name"] = new() { DisplayIndex = i++, Alias = "Server" };
            columns["target_elastic_pool_name"] = new() { DisplayIndex = i++, Alias = "Elastic Pool" };
            columns["target_database_name"] = new() { DisplayIndex = i++, Alias = "Database" };
            columns["run_status_description"] = new() { DisplayIndex = i++, Alias = "Status", Highlighting = RunStatusHighlight() };
            columns["lifecycle"] = new() { DisplayIndex = i++, Alias = "Lifecycle" };
            columns["DurationSec"] = new() { DisplayIndex = i++, Alias = "Duration (sec)", FormatString = "N0" };
            columns["Duration"] = new() { DisplayIndex = i++, Alias = "Duration" };
            columns["current_attempts"] = new() { DisplayIndex = i++, Alias = "Attempts" };
            columns["next_attempt_start_time"] = new() { DisplayIndex = i++, Alias = "Next Attempt", Visible = false };
            columns["last_message"] = new() { DisplayIndex = i++, Alias = "Message", Link = new MessageLink() };
            columns["job_version"] = new() { DisplayIndex = i++, Alias = "Job Version", Visible = false };
            columns["is_active"] = new() { DisplayIndex = i++, Alias = "Active", Visible = false };
            columns["InstanceID"] = Hidden(i++);
            columns["job_id"] = Hidden(i++);
            columns["job_execution_id"] = new() { DisplayIndex = i++, Alias = "Job Execution ID", Visible = false };
            columns["run_status"] = Hidden(i++);
            columns["name"] = Hidden(i++);
            return columns;
        }

        private static readonly Params ReportParams = new()
        {
            ParamList = new List<Param>
            {
                new() { ParamName = "@InstanceID", ParamType = "INT" },
                new() { ParamName = "@JobID", ParamType = "UNIQUEIDENTIFIER" },
                new() { ParamName = "@StepID", ParamType = "INT" },
                new() { ParamName = "@job_execution_id", ParamType = "UNIQUEIDENTIFIER" },
                new() { ParamName = "@FailedOnly", ParamType = "BIT" },
            }
        };

        public static SystemReport Instance => new()
        {
            ReportName = "Elastic Job History",
            SchemaName = "dbo",
            ProcedureName = "ElasticJobHistory_Get",
            QualifiedProcedureName = "dbo.ElasticJobHistory_Get",
            CanEditReport = false,
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                [0] = new CustomReportResult
                {
                    ResultName = "Elastic Job History",
                    Columns = GetColumns(includeViewTargets: true),
                }
            },
            Params = ReportParams,
            Pickers = ToolbarPickers,
        };

        /// <summary>
        /// "Executions" toggles @StepID between the job's executions only (the default - one row per execution) and NULL (every row,
        /// including the executions of each step against each target).  "Failed Only" toggles @FailedOnly.
        /// </summary>
        private static List<Picker> ToolbarPickers => new()
        {
            new()
            {
                ParameterName = "@StepID",
                Name = "Executions",
                DefaultValue = 0,
                DataType = typeof(int),
                PickerItems = new Dictionary<object, string>
                {
                    { 0, "Job Only" },
                    { DBNull.Value, "Job & Targets" },
                },
                MenuBar = true,
                Image = Properties.Resources.List_NumberedHS
            },
            Picker.CreateBooleanPicker("@FailedOnly", "Failed Only", defaultValue: false, trueString: "Yes", falseString: "No", menuBar: true, image: Properties.Resources.Filter_16x)
        };

        private static SystemReport TargetsInstance => new()
        {
            ReportName = "Elastic Job Execution",
            SchemaName = "dbo",
            ProcedureName = "ElasticJobHistory_Get",
            QualifiedProcedureName = "dbo.ElasticJobHistory_Get",
            CanEditReport = false,
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                [0] = new CustomReportResult
                {
                    ResultName = "Elastic Job Execution",
                    Columns = GetColumns(includeViewTargets: false),
                }
            },
            Params = ReportParams,
            Pickers = new List<Picker>
            {
                Picker.CreateBooleanPicker("@FailedOnly", "Failed Only", defaultValue: false, trueString: "Yes", falseString: "No", menuBar: true, image: Properties.Resources.Filter_16x)
            }
        };
    }
}
