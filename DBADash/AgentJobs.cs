using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Serilog;
using SerilogTimings;
using System;
using System.Collections.Generic;
using System.Data;

namespace DBADash
{
    internal class AgentJobs : SMOBaseClass
    {
        public AgentJobs(DBADashConnection source, SchemaSnapshotDBOptions options) : base(source, options)
        {
        }

        public AgentJobs(DBADashConnection source) : base(source)
        {
        }

        private static DataTable JobDataTableSchema()
        {
            DataTable dtSchema = new("Jobs");
            dtSchema.Columns.Add("job_id", typeof(Guid));
            dtSchema.Columns.Add("originating_server");
            dtSchema.Columns.Add("name");
            dtSchema.Columns.Add("enabled", typeof(bool));
            dtSchema.Columns.Add("description");
            dtSchema.Columns.Add("start_step_id", typeof(int));
            dtSchema.Columns.Add("category_id", typeof(int));
            dtSchema.Columns.Add("category");
            dtSchema.Columns.Add("owner");
            dtSchema.Columns.Add("notify_level_eventlog", typeof(int));
            dtSchema.Columns.Add("notify_level_email", typeof(int));
            dtSchema.Columns.Add("notify_level_netsend", typeof(int));
            dtSchema.Columns.Add("notify_level_page", typeof(int));
            dtSchema.Columns.Add("notify_email_operator");
            dtSchema.Columns.Add("notify_netsend_operator");
            dtSchema.Columns.Add("notify_page_operator");
            dtSchema.Columns.Add("delete_level", typeof(int));
            dtSchema.Columns.Add("date_created", typeof(DateTime));
            dtSchema.Columns.Add("date_modified", typeof(DateTime));
            dtSchema.Columns.Add("version_number", typeof(int));
            dtSchema.Columns.Add("has_schedule", typeof(bool));
            dtSchema.Columns.Add("has_server", typeof(bool));
            dtSchema.Columns.Add("has_step", typeof(bool));
            dtSchema.Columns.Add("DDLHash", typeof(byte[]));
            dtSchema.Columns.Add("DDL", typeof(byte[]));
            return dtSchema;
        }

        private static DataTable JobStepTableSchema()
        {
            DataTable jobStepDT = new("JobSteps");
            jobStepDT.Columns.Add("job_id", typeof(Guid));
            jobStepDT.Columns.Add("step_id", typeof(int));
            jobStepDT.Columns.Add("step_name");
            jobStepDT.Columns.Add("subsystem");
            jobStepDT.Columns.Add("command");
            jobStepDT.Columns.Add("cmdexec_success_code", typeof(int));
            jobStepDT.Columns.Add("on_success_action", typeof(short));
            jobStepDT.Columns.Add("on_success_step_id", typeof(int));
            jobStepDT.Columns.Add("on_fail_action", typeof(short));
            jobStepDT.Columns.Add("on_fail_step_id", typeof(int));
            jobStepDT.Columns.Add("database_name");
            jobStepDT.Columns.Add("database_user_name");
            jobStepDT.Columns.Add("retry_attempts", typeof(int));
            jobStepDT.Columns.Add("retry_interval", typeof(int));
            jobStepDT.Columns.Add("output_file_name");
            jobStepDT.Columns.Add("proxy_name");
            return jobStepDT;
        }

        public void CollectJobs(ref DataSet ds, bool scriptJobs, bool isRDS)
        {
            DataTable jobsDT;
            using (var op = Operation.At(Serilog.Events.LogEventLevel.Debug).Begin("Run Jobs query on instance {instance}", SourceConnection.ConnectionForPrint))
            {
                jobsDT = GetJobsDT();
                op.Complete();
            }

            DataTable jobStepsDT;
            using (var op = Operation.At(Serilog.Events.LogEventLevel.Debug)
                       .Begin("Run JobsSteps query instance {instance}", SourceConnection.ConnectionForPrint))
            {
                jobStepsDT = isRDS ? GetJobStepsDTForRDS() : GetJobStepsDT();
                op.Complete();
            }
            ds.Tables.Add(jobStepsDT);

            if (scriptJobs)
            {
                using (var op = Operation.At(Serilog.Events.LogEventLevel.Debug).Begin("Script Jobs from instance {instance}", SourceConnection.ConnectionForPrint))
                {
                    ScriptJobs(ref jobsDT);
                    op.Complete();
                }
            }
            ds.Tables.Add(jobsDT);
        }

        private void ScriptJobs(ref DataTable jobsDT)
        {
            List<Exception> errors = new();
            using (var cn = new SqlConnection(ConnectionString))
            {
                var instance = new Microsoft.SqlServer.Management.Smo.Server(new Microsoft.SqlServer.Management.Common.ServerConnection(cn));
                int totalJobs = jobsDT.Rows.Count;
                int cnt = 0;
                foreach (DataRow row in jobsDT.Rows)
                {
                    cnt++;
                    string sDDL;
                    string jobName = (string)row["name"];
                    using (var op = Operation.At(Serilog.Events.LogEventLevel.Debug).Begin("Script Job {number}/{totaljobs} ({pct}) {job} from {instance}", cnt, totalJobs, (cnt * 1.0 / totalJobs).ToString("P1"), jobName, SourceConnection.ConnectionForPrint))
                    {
                        try
                        {
                            var job = instance.JobServer.Jobs[jobName];
                            sDDL = SchemaSnapshotDB.StringCollectionToString(job.Script(ScriptingOptions));
                        }
                        catch (Exception ex)
                        {
                            string message = String.Format("Error scripting agent job `{0}` on {1}", jobName, instance.Name);
                            sDDL = "/*\n Error scripting job: \n" + ex.Message + "\n*/";
                            errors.Add(new Exception(message, ex));
                            Log.Error(ex, message);
                        }
                        var bDDL = Zip(sDDL);
                        row["DDL"] = bDDL;
                        row["DDLHash"] = ComputeHash(bDDL);
                        op.Complete();
                    }
                }
            }
        }

        private DataTable GetJobsDT()
        {
            using (var cn = new SqlConnection(ConnectionString))
            using (var da = new SqlDataAdapter(SqlStrings.Jobs, cn))
            {
                cn.Open();
                da.SelectCommand.CommandTimeout = CollectionType.Jobs.GetCommandTimeout();
                var jobDT = JobDataTableSchema();
                da.Fill(jobDT);
                return jobDT;
            }
        }

        private DataTable GetJobStepsDT()
        {
            using (var cn = new SqlConnection(ConnectionString))
            using (var da = new SqlDataAdapter(SqlStrings.JobSteps, cn))
            {
                cn.Open();
                da.SelectCommand.CommandTimeout = CollectionType.Jobs.GetCommandTimeout();
                var jobStepDT = JobStepTableSchema();
                da.Fill(jobStepDT);
                return jobStepDT;
            }
        }

        public DataTable GetJobStepsDTForRDS()
        {
            var jobStepDT = JobStepTableSchema();
            using var cn = new SqlConnection(ConnectionString);
            var instance = new Server(new Microsoft.SqlServer.Management.Common.ServerConnection(cn));
            
            foreach (Job job in instance.JobServer.Jobs)
            {
                foreach (JobStep step in job.JobSteps)
                {
                    var stepR = jobStepDT.NewRow();
                    stepR["job_id"] = job.JobID;
                    stepR["step_name"] = step.Name;
                    stepR["database_name"] = step.DatabaseName;
                    stepR["step_id"] = step.ID;
                    stepR["subsystem"] = step.SubSystem;
                    stepR["command"] = step.Command;
                    stepR["cmdexec_success_code"] = step.CommandExecutionSuccessCode;
                    stepR["on_success_action"] = step.OnSuccessAction;
                    stepR["on_success_step_id"] = step.OnSuccessStep;
                    stepR["on_fail_action"] = step.OnFailAction;
                    stepR["on_fail_step_id"] = step.OnFailStep;
                    stepR["database_user_name"] = step.DatabaseUserName;
                    stepR["retry_attempts"] = step.RetryAttempts;
                    stepR["retry_interval"] = step.RetryInterval;
                    stepR["output_file_name"] = step.OutputFileName;
                    stepR["proxy_name"] = step.ProxyName;

                    jobStepDT.Rows.Add(stepR);
                }
            }

            return jobStepDT;
        }
    }
}