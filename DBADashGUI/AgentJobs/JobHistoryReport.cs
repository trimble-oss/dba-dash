using DBADashGUI.CustomReports;
using DBADashGUI.Performance;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using static DBADashGUI.DBADashStatus;

namespace DBADashGUI.AgentJobs
{
    internal static class JobHistoryReport
    {
        private static CellHighlightingRuleSet RunStatusHighlight() => new("run_status_description")
        {
            Rules = new List<CellHighlightingRule>
            {
                new() { ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "Succeeded", Status = DBADashStatusEnum.OK },
                new() { ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "Failed", Status = DBADashStatusEnum.Critical },
            }
        };

        private sealed class RunDurationLink : LinkColumnInfo
        {
            public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
            {
                if (row.DataBoundItem is not DataRowView drv) return;
                var from = ((DateTime)drv["RunDateTime"]).ToUniversalTime();
                var to = from.AddSeconds(Convert.ToDouble(drv["RunDurationSec"]));
                var instanceId = (int)drv["InstanceID"];
                var jobId = (Guid)drv["job_id"];
                var viewer = new RunningQueriesViewer { SnapshotDateFrom = from, SnapshotDateTo = to, InstanceID = instanceId, JobId = jobId };
                viewer.ShowSingleInstance();
            }
        }

        private sealed class MessageLink : LinkColumnInfo
        {
            public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
            {
                if (row.DataBoundItem is not DataRowView drv) return;
                Convert.ToString(drv["message"]).OpenAsTextFile();
            }
        }

        private sealed class ViewStepsLink : LinkColumnInfo
        {
            public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
            {
                if (row.DataBoundItem is not DataRowView drv) return;

                var report = StepsInstance;
                var newContext = (DBADashContext)context.Clone();
                newContext.Report = report;
                var customParams = report.GetCustomSqlParameters();

                // Set @instance_id to filter to this specific execution
                var pInstanceId = customParams.FirstOrDefault(p => p.Param.ParameterName == "@instance_id");
                if (pInstanceId != null)
                {
                    pInstanceId.Param.Value = (int)drv["instance_id"];
                    pInstanceId.UseDefaultValue = false;
                }

                // Set @StepID to NULL to show all steps
                var pStepId = customParams.FirstOrDefault(p => p.Param.ParameterName == "@StepID");
                if (pStepId != null)
                {
                    pStepId.Param.Value = DBNull.Value;
                    pStepId.UseDefaultValue = false;
                }

                // Carry forward @InstanceID and @JobID
                var pInstId = customParams.FirstOrDefault(p => p.Param.ParameterName == "@InstanceID");
                if (pInstId != null)
                {
                    pInstId.Param.Value = (int)drv["InstanceID"];
                    pInstId.UseDefaultValue = false;
                }
                var pJobId = customParams.FirstOrDefault(p => p.Param.ParameterName == "@JobID");
                if (pJobId != null)
                {
                    pJobId.Param.Value = (Guid)drv["job_id"];
                    pJobId.UseDefaultValue = false;
                }

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
        }

        private static ColumnMetadata Hidden(int displayIndex) =>
            new() { Visible = false, DisplayIndex = displayIndex };

        private static Dictionary<string, ColumnMetadata> GetColumns(bool includeViewSteps)
        {
            var columns = new Dictionary<string, ColumnMetadata>();
            var i = 0;
            if (includeViewSteps)
            {
                columns["View Steps"] = new() { DisplayIndex = i++, Alias = "View Steps", Link = new ViewStepsLink(), Description = "View steps for this job execution" };
            }
            columns["RunDateTime"] = new() { DisplayIndex = i++, Alias = "Start" };
            columns["RunEndDateTime"] = new() { DisplayIndex = i++, Alias = "Finish" };
            columns["step_id"] = new() { DisplayIndex = i++, Alias = "Step ID" };
            columns["step_name"] = new() { DisplayIndex = i++, Alias = "Step Name" };
            columns["sql_message_id"] = new() { DisplayIndex = i++, Alias = "Message ID" };
            columns["sql_severity"] = new() { DisplayIndex = i++, Alias = "Severity" };
            columns["run_status_description"] = new() { DisplayIndex = i++, Alias = "Status", Highlighting = RunStatusHighlight() };
            columns["RunDurationSec"] = new() { DisplayIndex = i++, Alias = "Duration (sec)", Link = new RunDurationLink() };
            columns["RunDuration"] = new() { DisplayIndex = i++, Alias = "Run Duration", Link = new RunDurationLink() };
            columns["retries_attempted"] = new() { DisplayIndex = i++, Alias = "Retries" };
            columns["message"] = new() { DisplayIndex = i++, Alias = "Message", Link = new MessageLink() };
            columns["InstanceID"] = Hidden(i++);
            columns["job_id"] = Hidden(i++);
            columns["instance_id"] = Hidden(i++);
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
                new() { ParamName = "@instance_id", ParamType = "INT" },
                new() { ParamName = "@FailedOnly", ParamType = "BIT" },
            }
        };

        public static SystemReport Instance => new()
        {
            ReportName = "Job History",
            SchemaName = "dbo",
            ProcedureName = "JobHistory_Get",
            QualifiedProcedureName = "dbo.JobHistory_Get",
            CanEditReport = false,
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                [0] = new CustomReportResult
                {
                    ResultName = "Job History",
                    Columns = GetColumns(includeViewSteps: true),
                }
            },
            Params = ReportParams,
        };

        private static SystemReport StepsInstance => new()
        {
            ReportName = "Job Steps",
            SchemaName = "dbo",
            ProcedureName = "JobHistory_Get",
            QualifiedProcedureName = "dbo.JobHistory_Get",
            CanEditReport = false,
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                [0] = new CustomReportResult
                {
                    ResultName = "Job Steps",
                    Columns = GetColumns(includeViewSteps: false),
                }
            },
            Params = ReportParams,
        };
    }
}
