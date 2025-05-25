using Microsoft.Data.SqlClient;
using Org.BouncyCastle.Crypto.Engines;
using Serilog;
using SerilogTimings;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.Messaging
{
    public class AgentJobExecutionMessage : MessageBase
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
            var rdr = await cmd.ExecuteReaderAsync(cancellationToken);
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
                "Execute Job {job} on {ConnectionID} triggered from message {handle}",
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
                Log.Error(ex, "Plan collection request from {handle} failed", handle);
                throw;
            }
        }

        private static void ThrowIfJobExecutionIsNotAllowed(CollectionConfig cfg, DataTable dtJob)
        {
            // Quick check for "allow all" case
            if ((cfg.AllowedJobs is "*" or "%"))
            {
                return;
            }

            var jobInfo = new HelpJobInfo(dtJob);
            var jobValues = new[] { jobInfo.Category, jobInfo.Name, jobInfo.JobId.ToString() };
            // Check denied first
            if (IsValueDenied(jobValues, cfg.AllowedJobs))
            {
                throw new Exception($"Job execution is denied for {jobInfo.Name} with category {jobInfo.Category} and id {jobInfo.JobId}");
            }

            // Check allowed
            if (IsValueAllowed(jobValues, cfg.AllowedJobs))
            {
                return;
            }

            throw new Exception($"Job execution is not allowed for {jobInfo.Name} with category {jobInfo.Category} and id {jobInfo.JobId}");
        }

        private static bool IsValueDenied(string[] jobValues, string configValue)
        {
            if (string.IsNullOrEmpty(configValue)) return false;

            return configValue.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Where(s => s.StartsWith('-'))
                .Select(s => s[1..])
                .Any(pattern => jobValues.Any(value => IsWildcardMatch(value, pattern)));
        }

        private static bool IsValueAllowed(string[] jobValues, string configValue)
        {
            if (string.IsNullOrEmpty(configValue)) return false;

            return configValue.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Where(s => !s.StartsWith('-'))
                .Any(pattern => jobValues.Any(value => IsWildcardMatch(value, pattern)));
        }

        private static bool IsWildcardMatch(string value, string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return string.IsNullOrEmpty(value);

            if (pattern is "*" or "%")
                return true;

            // For exact matches (no wildcards), use simple comparison
            if (!pattern.Contains('*') && !pattern.Contains('%'))
                return string.Equals(value, pattern, StringComparison.OrdinalIgnoreCase);

            // Use regex for wildcard patterns
            var regexPattern = Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\%", ".*");

            return Regex.IsMatch(value, $"^{regexPattern}$", RegexOptions.IgnoreCase);
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