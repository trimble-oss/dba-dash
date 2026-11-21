/*
	The definition of an elastic job: its steps (current version), and the members of the target groups they run against.
*/
CREATE PROC dbo.ElasticJobSteps_Get(
	@InstanceID INT,
	@JobID UNIQUEIDENTIFIER
)
AS
SET NOCOUNT ON

SELECT	S.InstanceID,
		S.job_id,
		J.job_name,
		S.step_id,
		S.step_name,
		S.target_group_name,
		S.command,
		S.command_type,
		S.credential_name,
		S.retry_attempts,
		S.initial_retry_interval_seconds,
		S.maximum_retry_interval_seconds,
		S.retry_interval_backoff_multiplier,
		S.step_timeout_seconds,
		S.max_parallelism,
		S.output_type,
		S.output_server_name,
		S.output_database_name,
		S.output_schema_name,
		S.output_table_name,
		S.job_version
FROM dbo.ElasticJobSteps S
JOIN dbo.ElasticJobs J ON J.InstanceID = S.InstanceID AND J.job_id = S.job_id
WHERE S.InstanceID = @InstanceID
AND S.job_id = @JobID
ORDER BY S.step_id

SELECT	M.target_group_name,
		M.membership_type,
		M.target_type,
		M.server_name,
		M.database_name,
		M.elastic_pool_name,
		M.shard_map_name,
		M.resource_group_name,
		M.refresh_credential_name
FROM dbo.ElasticJobTargetGroupMembers M
WHERE M.InstanceID = @InstanceID
AND EXISTS(	SELECT 1
			FROM dbo.ElasticJobSteps S
			WHERE S.InstanceID = M.InstanceID
			AND S.job_id = @JobID
			AND S.target_group_id = M.target_group_id
			)
ORDER BY M.target_group_name, M.membership_type, M.server_name, M.elastic_pool_name, M.shard_map_name, M.database_name
GO
GRANT EXECUTE
	ON OBJECT::[dbo].[ElasticJobSteps_Get] TO [Reports]
	AS [dbo];
