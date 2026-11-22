using Microsoft.Data.SqlClient;
using Serilog;
using SerilogTimings;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.Messaging
{
    public class AgentJobExecutionMessage : MessageBase, IConnectionMessage
    {
        public string ConnectionID { get; set; }

        public Guid JobId { get; set; }

        public JobActions JobAction { get; set; } = JobActions.StartJob;

        public enum JobActions
        {
            StartJob,
            StopJob,
            StatusOnly
        }

        public string StepName { get; set; }

        private async Task<DataTable> GetHelpJob(string connectionString, CancellationToken cancellationToken)
        {
            await using var cn = new SqlConnection(connectionString);
            await cn.OpenAsync(cancellationToken);
            await using var cmd = new SqlCommand("dbo.sp_help_job", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@job_id", JobId);
            cmd.Parameters.AddWithValue("job_aspect", "JOB");
            await using var rdr = await cmd.ExecuteReaderAsync(cancellationToken);
            var dt = new DataTable("JOB");
            dt.Load(rdr);
            return dt;
        }

        public override async Task<DataSet> Process(CollectionConfig cfg, Guid handle, CancellationToken cancellationToken)
        {
            if (IsExpired)
            {
                throw new Exception("Message expired");
            }
            if (string.IsNullOrEmpty(cfg.AllowedJobs))
            {
                throw new Exception("Agent job execution is disabled.  Use the service config tool to enable.");
            }

            using var op = Operation.Begin(
                "Execute Job {Job} on {ConnectionID} triggered from message {Handle}",
                JobId,
                ConnectionID,
                handle);
            try
            {
                var src = await cfg.GetSourceConnectionAsync(ConnectionID);
                var builder = new SqlConnectionStringBuilder(src.SourceConnection.ConnectionString)
                {
                    InitialCatalog = "msdb"
                };
                var ds = new DataSet();
                var dtJob = await GetHelpJob(builder.ConnectionString, cancellationToken);
                ThrowIfJobExecutionIsNotAllowed(cfg, dtJob);
                ds.Tables.Add(dtJob);

                if (JobAction is JobActions.StartJob or JobActions.StopJob)
                {
                    var dtMessages = await RunJob(builder.ConnectionString, cancellationToken);
                    ds.Tables.Add(dtMessages);
                }
                op.Complete();
                return ds;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Plan collection request from {Handle} failed", handle);
                throw;
            }
        }

        private static void ThrowIfJobExecutionIsNotAllowed(CollectionConfig cfg, DataTable dtJob)
        {
            var jobInfo = new HelpJobInfo(dtJob);
            AllowedJobsFilter.ThrowIfNotAllowed(cfg.AllowedJobs,
                new[] { jobInfo.Category, jobInfo.Name, jobInfo.JobId.ToString() },
                $"{jobInfo.Name} with category {jobInfo.Category} and id {jobInfo.JobId}");
        }

        private async Task<DataTable> RunJob(string connectionString, CancellationToken cancellationToken)
        {
            await using var cn = new SqlConnection(connectionString);
            await cn.OpenAsync(cancellationToken);
            var proc = JobAction == JobActions.StartJob ? "dbo.sp_start_job" : "dbo.sp_stop_job";
            await using var cmd =
                new SqlCommand(proc, cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@job_id", JobId);
            if (JobAction == JobActions.StartJob && !string.IsNullOrEmpty(StepName))
            {
                cmd.Parameters.AddWithValue("@step_name", StepName);
            }
            var sb = new StringBuilder();
            cn.InfoMessage += (sender, args) =>
            {
                sb.AppendLine(args.Message);
            };
            var dt = new DataTable("RunJobMessages");

            dt.Columns.Add("Message");
            await cmd.ExecuteNonQueryAsync(cancellationToken);
            dt.Rows.Add(sb.ToString().Trim());
            return dt;
        }
    }
}