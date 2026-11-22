using Microsoft.Data.SqlClient;
using Serilog;
using SerilogTimings;
using System;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.Messaging
{
    /// <summary>
    /// Starts or stops (cancels) an elastic job, or returns the status of one of its executions - the elastic job counterpart of
    /// <see cref="AgentJobExecutionMessage"/>.  The connection is the job database of an elastic job agent.
    ///
    /// <para>Guarded as SQL Agent jobs are: the service only acts on jobs allowed by <see cref="CollectionConfig.AllowedJobs"/>, matched
    /// against the job's name and ID (elastic jobs have no category), and the user needs the AllowJobExecution role in the repository.
    /// Starting and stopping jobs (jobs.sp_start_job/jobs.sp_stop_job) needs the service login to be a member of jobs_admin in the job
    /// database - collection needs only jobs_reader.</para>
    ///
    /// <para>The job is identified by ID, as it's stored in the repository.  Each batch checks the job still has the name it was allowed
    /// by, and an execution is only stopped if it's an execution of that job, so the check can't be got around by a rename or by passing
    /// the ID of another job's execution.</para>
    /// </summary>
    public class ElasticJobExecutionMessage : MessageBase, IConnectionMessage
    {
        public string ConnectionID { get; set; }

        public Guid JobId { get; set; }

        /// <summary>
        /// The execution to stop or return the status of.  Null to stop every active execution of the job, or for the status of its latest.
        /// Set on the reply to <see cref="JobActions.StartJob"/> - the execution started is in the <see cref="ExecutionsTableName"/> table.
        /// </summary>
        public Guid? JobExecutionId { get; set; }

        public JobActions JobAction { get; set; } = JobActions.StatusOnly;

        public enum JobActions
        {
            StartJob,
            StopJob,
            StatusOnly
        }

        /// <summary>The job: job_id, job_name, enabled</summary>
        public const string JobTableName = "JOB";

        /// <summary>
        /// Rows of jobs.job_executions for the execution: the job's own row (no step) first, then its steps and targets.  At most
        /// <see cref="MaxExecutionRows"/>.  The columns are those of the view, which vary with the version of the agent, with last_message
        /// truncated.  Empty if the job has never run.
        /// </summary>
        public const string ExecutionsTableName = "Executions";

        /// <summary>
        /// Messages from starting or stopping the job (Message), and the execution started - or stopped, if there was one (JobExecutionId, null
        /// otherwise).  The execution is returned here as well as in <see cref="ExecutionsTableName"/>, which won't have it if its row isn't there yet.
        /// </summary>
        public const string MessagesTableName = "RunJobMessages";

        /// <summary>A job that runs against a lot of targets can have a lot of rows - enough for the status, without returning all of them</summary>
        public const int MaxExecutionRows = 1000;

        /// <summary>The job, and whether it still has the name it was allowed by</summary>
        private const string GetJobSql = @"SELECT job_id, job_name, enabled
FROM jobs.jobs
WHERE job_id = @JobId";

        private const string StartJobSql = @"
IF NOT EXISTS(SELECT 1 FROM jobs.jobs WHERE job_id = @JobId AND job_name = @JobName)
BEGIN
	RAISERROR('The job has been renamed or removed.  Refresh and try again.',16,1)
	RETURN
END
EXEC jobs.sp_start_job @job_name = @JobName, @job_execution_id = @JobExecutionId OUTPUT";

        /// <summary>
        /// Stops the execution - or every active execution of the job when there's no @JobExecutionId.  Only executions of the job: the job's
        /// own row (no step), which jobs.sp_stop_job cancels along with its steps and targets.
        /// </summary>
        private const string StopJobSql = @"
IF NOT EXISTS(SELECT 1 FROM jobs.jobs WHERE job_id = @JobId AND job_name = @JobName)
BEGIN
	RAISERROR('The job has been renamed or removed.  Refresh and try again.',16,1)
	RETURN
END
DECLARE @Stop TABLE(job_execution_id UNIQUEIDENTIFIER PRIMARY KEY)
INSERT INTO @Stop(job_execution_id)
SELECT DISTINCT job_execution_id
FROM jobs.job_executions
WHERE job_id = @JobId
AND step_id IS NULL
AND is_active = 1
AND (job_execution_id = @JobExecutionId OR @JobExecutionId IS NULL)

IF NOT EXISTS(SELECT 1 FROM @Stop)
BEGIN
	RAISERROR('The job isn''t running.',16,1)
	RETURN
END

DECLARE @id UNIQUEIDENTIFIER
DECLARE @Stopped INT = 0
DECLARE cStop CURSOR LOCAL FAST_FORWARD FOR SELECT job_execution_id FROM @Stop
OPEN cStop
FETCH NEXT FROM cStop INTO @id
WHILE @@FETCH_STATUS = 0
BEGIN
	EXEC jobs.sp_stop_job @job_execution_id = @id
	SET @Stopped += 1
	FETCH NEXT FROM cStop INTO @id
END
CLOSE cStop
DEALLOCATE cStop

PRINT CONCAT('Cancellation requested for ',@Stopped,' execution(s).')
IF @JobExecutionId IS NULL AND @Stopped = 1
BEGIN
	SELECT @JobExecutionId = job_execution_id FROM @Stop
END";

        /// <summary>
        /// The execution - when there's no @JobExecutionId, the job's latest active execution, or its latest if none is active (an older execution
        /// can still be running after a newer one has finished).  The view's own columns (they vary with the version of the agent),
        /// with last_message truncated as for collection.
        /// </summary>
        private const string GetExecutionsSql = @"
IF @JobExecutionId IS NULL
BEGIN
	SELECT TOP(1) @JobExecutionId = job_execution_id
	FROM jobs.job_executions
	WHERE job_id = @JobId
	AND step_id IS NULL
	ORDER BY is_active DESC, create_time DESC
END

DECLARE @Columns NVARCHAR(MAX)
SELECT @Columns = STUFF((
	SELECT N',' + CASE WHEN name = N'last_message' THEN N'CAST(LEFT(last_message,4000) AS NVARCHAR(4000)) AS last_message' ELSE QUOTENAME(name) END
	FROM sys.columns
	WHERE object_id = OBJECT_ID(N'jobs.job_executions')
	ORDER BY column_id
	FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,1,N'')

DECLARE @SQL NVARCHAR(MAX) = N'SELECT TOP(@MaxRows) ' + @Columns + N'
FROM jobs.job_executions
WHERE job_execution_id = @JobExecutionId
AND job_id = @JobId
ORDER BY CASE WHEN step_id IS NULL THEN 0 ELSE 1 END, step_id, create_time'

EXEC sp_executesql @SQL, N'@JobExecutionId UNIQUEIDENTIFIER,@JobId UNIQUEIDENTIFIER,@MaxRows INT', @JobExecutionId, @JobId, @MaxRows";

        /// <summary>Permission denied - for EXECUTE on jobs.sp_start_job/jobs.sp_stop_job, the login isn't in jobs_admin</summary>
        private const int PermissionDeniedError = 229;

        public override async Task<DataSet> Process(CollectionConfig cfg, Guid handle, CancellationToken cancellationToken)
        {
            ThrowIfExpired();
            if (string.IsNullOrEmpty(cfg.AllowedJobs))
            {
                throw new Exception("Job execution is disabled.  Use the service config tool to enable.");
            }
            if (JobId == Guid.Empty)
            {
                throw new ArgumentException("JobId is required");
            }

            using var op = Operation.Begin(
                "Elastic job {Action} for {Job} on {ConnectionID} triggered from message {MessageId} with handle {Handle}",
                JobAction,
                JobId,
                ConnectionID,
                Id,
                handle);
            try
            {
                var src = await cfg.GetSourceConnectionAsync(ConnectionID);
                await using var cn = new SqlConnection(src.SourceConnection.ConnectionString);
                await cn.OpenAsync(cancellationToken);

                var ds = new DataSet();
                var dtJob = await GetJobAsync(cn, cancellationToken);
                if (dtJob.Rows.Count == 0)
                {
                    throw new Exception($"Elastic job {JobId} wasn't found in the job database");
                }
                var jobName = dtJob.Rows[0].Field<string>("job_name");
                AllowedJobsFilter.ThrowIfNotAllowed(cfg.AllowedJobs, new[] { jobName, JobId.ToString() }, $"{jobName} with id {JobId}");
                ds.Tables.Add(dtJob);

                if (JobAction is JobActions.StartJob or JobActions.StopJob)
                {
                    ds.Tables.Add(await RunAsync(cn, jobName, cancellationToken));
                }
                ds.Tables.Add(await GetExecutionsAsync(cn, cancellationToken));
                op.Complete();
                return ds;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Elastic job {Action} request from {Handle} failed", JobAction, handle);
                throw;
            }
        }

        private async Task<DataTable> GetJobAsync(SqlConnection cn, CancellationToken cancellationToken)
        {
            await using var cmd = new SqlCommand(GetJobSql, cn) { CommandTimeout = Lifetime };
            cmd.Parameters.Add("@JobId", SqlDbType.UniqueIdentifier).Value = JobId;
            await using var rdr = await cmd.ExecuteReaderAsync(cancellationToken);
            var dt = new DataTable(JobTableName);
            dt.Load(rdr);
            return dt;
        }

        /// <summary>Starts or stops the job.  <see cref="JobExecutionId"/> is set to the execution started - or stopped, if there was one.</summary>
        private async Task<DataTable> RunAsync(SqlConnection cn, string jobName, CancellationToken cancellationToken)
        {
            await using var cmd = new SqlCommand(JobAction == JobActions.StartJob ? StartJobSql : StopJobSql, cn) { CommandTimeout = Lifetime };
            cmd.Parameters.Add("@JobId", SqlDbType.UniqueIdentifier).Value = JobId;
            cmd.Parameters.Add("@JobName", SqlDbType.NVarChar, 128).Value = jobName;
            var pExecutionId = cmd.Parameters.Add("@JobExecutionId", SqlDbType.UniqueIdentifier);
            pExecutionId.Direction = ParameterDirection.InputOutput;
            pExecutionId.Value = JobAction == JobActions.StopJob && JobExecutionId.HasValue ? JobExecutionId.Value : DBNull.Value;

            var sb = new StringBuilder();
            void OnInfoMessage(object sender, SqlInfoMessageEventArgs args) => sb.AppendLine(args.Message);
            cn.InfoMessage += OnInfoMessage;
            try
            {
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (SqlException ex) when (ex.Number == PermissionDeniedError && ex.Message.Contains("EXECUTE", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception($"{ex.Message}  The service login needs to be a member of the jobs_admin role in the job database to start or stop elastic jobs.", ex);
            }
            finally
            {
                cn.InfoMessage -= OnInfoMessage;
            }

            if (pExecutionId.Value is Guid id)
            {
                JobExecutionId = id;
            }
            var message = sb.ToString().Trim();
            if (message.Length == 0 && JobAction == JobActions.StartJob)
            {
                message = $"Job '{jobName}' started.  Execution {JobExecutionId}";
            }
            var dt = new DataTable(MessagesTableName);
            dt.Columns.Add("Message");
            dt.Columns.Add("JobExecutionId", typeof(Guid));
            dt.Rows.Add(message, JobExecutionId.HasValue ? JobExecutionId.Value : DBNull.Value);
            return dt;
        }

        private async Task<DataTable> GetExecutionsAsync(SqlConnection cn, CancellationToken cancellationToken)
        {
            await using var cmd = new SqlCommand(GetExecutionsSql, cn) { CommandTimeout = Lifetime };
            cmd.Parameters.Add("@JobId", SqlDbType.UniqueIdentifier).Value = JobId;
            cmd.Parameters.Add("@JobExecutionId", SqlDbType.UniqueIdentifier).Value = JobExecutionId.HasValue ? JobExecutionId.Value : DBNull.Value;
            cmd.Parameters.Add("@MaxRows", SqlDbType.Int).Value = MaxExecutionRows;
            await using var rdr = await cmd.ExecuteReaderAsync(cancellationToken);
            var dt = new DataTable(ExecutionsTableName);
            dt.Load(rdr);
            return dt;
        }
    }
}
