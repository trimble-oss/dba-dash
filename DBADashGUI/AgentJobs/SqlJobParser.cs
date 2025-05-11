using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DBADashGUI.AgentJobs
{
    public class SqlJobParser
    {
        public static List<SqlJobInfo> ParseJobScript(string scriptText)
        {
            // Setup the parser
            var parser = new TSql160Parser(true);

            using var reader = new StringReader(scriptText);
            // Parse the script
            var fragment = parser.Parse(reader, out var errors);

            if (errors.Count <= 0) return ExtractJobInfo(fragment);
            var sbErrors = new StringBuilder();
            sbErrors.AppendLine("Error parsing script");
            foreach (var error in errors)
            {
                sbErrors.AppendLine($"Line {error.Line}, Column {error.Column}: {error.Message}");
            }
            throw new Exception(sbErrors.ToString());
        }

        private static List<SqlJobInfo> ExtractJobInfo(TSqlFragment fragment)
        {
            var jobs = new Dictionary<string, SqlJobInfo>();

            // Find all ExecuteStatements in the script
            var ExecuteStatements = new TSqlFragmentVisitor<ExecuteStatement>().Visit(fragment);

            SqlJobInfo currentJob = null;

            foreach (var exec in ExecuteStatements)
            {
                // Get the procedure name
                var procedureName = GetProcedureName(exec);
                if (string.IsNullOrEmpty(procedureName))
                    continue;

                switch (procedureName.ToLower())
                {
                    case "sp_add_job":
                        var jobInfo = ExtractAddJobInfo(exec);
                        if (jobInfo != null)
                        {
                            currentJob = jobInfo;
                            var currentJobId = "@jobId";
                            jobs[currentJobId] = currentJob;
                        }
                        break;

                    case "sp_add_jobstep":
                        if (currentJob != null)
                        {
                            var step = ExtractJobStepInfo(exec);
                            if (step != null)
                            {
                                currentJob.Steps.Add(step);
                            }
                        }
                        break;

                    case "sp_add_jobschedule":
                        if (currentJob != null)
                        {
                            var schedule = ExtractJobScheduleInfo(exec);
                            if (schedule != null)
                            {
                                currentJob.Schedules.Add(schedule);
                            }
                        }
                        break;
                }
            }

            return jobs.Values.ToList();
        }

        private static string GetProcedureName(ExecuteStatement exec)
        {
            // Check if it's an ExecutableProcedureReference
            if (exec.ExecuteSpecification.ExecutableEntity is not ExecutableProcedureReference procRef) return null;
            // ProcedureReference can directly have a Name property
            var procedureName = procRef.ProcedureReference.ProcedureReference.Name;

            if (procedureName == null) return null;
            // If the procedure has a multi-part name, get the last part
            return procedureName.Identifiers.Count > 0 ? procedureName.Identifiers.Last().Value : null;
        }

        private static SqlJobInfo ExtractAddJobInfo(ExecuteStatement exec)
        {
            var jobInfo = new SqlJobInfo();

            if (exec.ExecuteSpecification.ExecutableEntity is not ExecutableProcedureReference procRef) return jobInfo;
            foreach (var param in procRef.Parameters)
            {
                var paramName = param.Variable.Name.ToLower();
                if (paramName == null) continue;

                switch (paramName)
                {
                    case "@job_name":
                        jobInfo.JobName = ExtractStringValue(param.ParameterValue);
                        break;

                    case "@enabled":
                        jobInfo.Enabled = ExtractIntValue(param.ParameterValue) == 1;
                        break;

                    case "@description":
                        jobInfo.Description = ExtractStringValue(param.ParameterValue);
                        break;

                    case "@category_name":
                        jobInfo.Category = ExtractStringValue(param.ParameterValue);
                        break;

                    case "@owner_login_name":
                        jobInfo.Owner = ExtractStringValue(param.ParameterValue);
                        break;

                    case "@notify_email_operator_name":
                        jobInfo.NotifyEmailOperator = ExtractStringValue(param.ParameterValue);
                        break;

                    case "@notify_page_operator_name":
                        jobInfo.NotifyPageOperator = ExtractStringValue(param.ParameterValue);
                        break;

                    case "@delete_level":
                        jobInfo.DeleteLevel = ExtractIntValue(param.ParameterValue);
                        break;

                    case "@notify_level_page":
                        jobInfo.NotifyLevelPage = ExtractIntValue(param.ParameterValue);
                        break;

                    case "@notify_level_email":
                        jobInfo.NotifyLevelEmail = ExtractIntValue(param.ParameterValue);
                        break;

                    case "@notify_level_eventlog":
                        jobInfo.NotifyLevelEventLog = ExtractIntValue(param.ParameterValue);
                        break;
                }
            }

            return jobInfo;
        }

        private static SqlJobStep ExtractJobStepInfo(ExecuteStatement exec)
        {
            var step = new SqlJobStep();

            if (exec.ExecuteSpecification.ExecutableEntity is not ExecutableProcedureReference procRef) return step;
            foreach (var param in procRef.Parameters)
            {
                var paramName = param?.Variable?.Name?.ToLower();
                if (paramName == null) continue;

                switch (paramName)
                {
                    case "@step_name":
                        step.StepName = ExtractStringValue(param.ParameterValue);
                        break;

                    case "@step_id":
                        step.StepId = ExtractIntValue(param.ParameterValue);
                        break;

                    case "@subsystem":
                        step.Subsystem = ExtractStringValue(param.ParameterValue);
                        break;

                    case "@command":
                        step.Command = ExtractStringValue(param.ParameterValue);
                        break;

                    case "@database_name":
                        step.DatabaseName = ExtractStringValue(param.ParameterValue);
                        break;

                    case "@on_success_action":
                        step.OnSuccessAction = ExtractIntValue(param.ParameterValue);
                        break;

                    case "@on_success_step_id":
                        step.OnSuccessStepId = ExtractIntValue(param.ParameterValue);
                        break;

                    case "@on_fail_action":
                        step.OnFailAction = ExtractIntValue(param.ParameterValue);
                        break;

                    case "@on_fail_step_id":
                        step.OnFailStepId = ExtractIntValue(param.ParameterValue);
                        break;

                    case "@retry_attempts":
                        step.RetryAttempts = ExtractIntValue(param.ParameterValue);
                        break;

                    case "@retry_interval":
                        step.RetryInterval = ExtractIntValue(param.ParameterValue);
                        break;
                }
            }

            return step;
        }

        private static SqlJobSchedule ExtractJobScheduleInfo(ExecuteStatement exec)
        {
            var schedule = new SqlJobSchedule();

            if (exec.ExecuteSpecification.ExecutableEntity is not ExecutableProcedureReference procRef) return schedule;
            foreach (var param in procRef.Parameters)
            {
                var paramName = param?.Variable?.Name?.ToLower();
                if (paramName == null) continue;

                switch (paramName)
                {
                    case "@name":
                        schedule.Name = ExtractStringValue(param.ParameterValue);
                        break;

                    case "@enabled":
                        schedule.Enabled = ExtractIntValue(param.ParameterValue) == 1;
                        break;

                    case "@freq_type":
                        schedule.FrequencyType = ExtractIntValue(param.ParameterValue);
                        break;

                    case "@freq_interval":
                        schedule.FrequencyInterval = ExtractIntValue(param.ParameterValue);
                        break;

                    case "@freq_subday_type":
                        schedule.FrequencySubdayType = ExtractIntValue(param.ParameterValue);
                        break;

                    case "@freq_subday_interval":
                        schedule.FrequencySubdayInterval = ExtractIntValue(param.ParameterValue);
                        break;

                    case "@freq_relative_interval":
                        schedule.FrequencyRelativeInterval = ExtractIntValue(param.ParameterValue);
                        break;

                    case "@freq_recurrence_factor":
                        schedule.FrequencyRecurrenceFactor = ExtractIntValue(param.ParameterValue);
                        break;

                    case "@active_start_date":
                        schedule.ActiveStartDate = ExtractIntValue(param.ParameterValue);
                        break;

                    case "@active_end_date":
                        schedule.ActiveEndDate = ExtractIntValue(param.ParameterValue);
                        break;

                    case "@active_start_time":
                        schedule.ActiveStartTime = ExtractIntValue(param.ParameterValue);
                        break;

                    case "@active_end_time":
                        schedule.ActiveEndTime = ExtractIntValue(param.ParameterValue);
                        break;

                    case "@schedule_uid":
                        schedule.ScheduleUid = ExtractStringValue(param.ParameterValue);
                        break;
                }
            }

            return schedule;
        }

        private static string ExtractStringValue(ScalarExpression expression)
        {
            if (expression is StringLiteral stringLiteral)
            {
                return stringLiteral.Value;
            }
            return null;
        }

        private static int ExtractIntValue(ScalarExpression expression)
        {
            if (expression is IntegerLiteral intLiteral)
            {
                return int.Parse(intLiteral.Value);
            }
            return 0;
        }
    }

    // Helper class to visit fragment hierarchies
    public class TSqlFragmentVisitor<T> where T : TSqlFragment
    {
        public IList<T> Visit(TSqlFragment fragment)
        {
            var results = new List<T>();
            VisitNode(fragment, results);
            return results;
        }

        private static void VisitNode(TSqlFragment node, List<T> results)
        {
            switch (node)
            {
                case null:
                    return;
                // If this node is of the type we're looking for, add it
                case T target:
                    results.Add(target);
                    break;
            }

            // Visit all child nodes by using a TSqlFragmentVisitor
            var visitor = new ScriptDomVisitor<T>(results);
            node.Accept(visitor);
        }
    }

    // Proper visitor implementation using the Accept method
    public class ScriptDomVisitor<T> : TSqlFragmentVisitor where T : TSqlFragment
    {
        private readonly List<T> _results;

        public ScriptDomVisitor(List<T> results)
        {
            _results = results;
        }

        public override void Visit(TSqlFragment fragment)
        {
            // If this node is of the type we're looking for, add it
            if (fragment is T target)
            {
                _results.Add(target);
            }

            // Continue traversal
            base.Visit(fragment);
        }
    }
}