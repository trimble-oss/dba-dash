/*
	Alerts for elastic jobs that failed - the counterpart of Alert.AgentJobAlert_Upd for the jobs of an elastic job agent.  The alert is
	raised against the agent's job database (an Azure DB instance).

	An alert is raised when a job's last execution failed (failed, timed out or was canceled) within the last hour, and it hasn't been
	alerted on since.  The message lists the databases the failed execution failed against, where the executions against each target
	share the job execution's ID.  It's resolved when the job succeeds, or is disabled or deleted.
	Rule details: JobName and ExcludeJobName - newline separated lists of names, with LIKE patterns.
*/
CREATE PROC Alert.ElasticJobAlert_Upd
AS
DECLARE @Type VARCHAR(50)= 'ElasticJob'
DECLARE @AlertKeyPrefix NVARCHAR(256) = 'ElasticJob:'
/* Check if we have any rules to process */
IF NOT EXISTS(
	SELECT 1
	FROM Alert.Rules
	WHERE Type = @Type
)
BEGIN
	PRINT CONCAT('No rules of type ',@Type,' to process')
	RETURN;
END
PRINT CONCAT('Processing alerts of type ',@Type)
EXEC Alert.ApplicableInstances_Ensure;

CREATE TABLE #RecentJobs(
	InstanceID INT NOT NULL,
	job_id UNIQUEIDENTIFIER NOT NULL,
	LastFailed DATETIME2 NULL,
	LastSucceeded DATETIME2 NULL,
	PRIMARY KEY(InstanceID,job_id)
)
/* Jobs that failed recently */
INSERT INTO #RecentJobs(
	InstanceID,
	job_id,
	LastFailed,
	LastSucceeded
)
SELECT	J.InstanceID,
		J.job_id,
		J.LastFailed,
		J.LastSucceeded
FROM dbo.ElasticJobs J
WHERE J.LastFailed>=DATEADD(mi,-60,SYSUTCDATETIME())
AND J.IsActive=1
AND J.enabled=1

CREATE TABLE #ElasticJobApplicable(
	InstanceID INT NOT NULL,
	Priority TINYINT NOT NULL,
	JobName NVARCHAR(MAX) COLLATE DATABASE_DEFAULT NULL,
	ExcludeJobName NVARCHAR(MAX) COLLATE DATABASE_DEFAULT NULL,
	RuleID INT NOT NULL,
	GroupID INT NOT NULL DEFAULT(0)
)
/* Rules and the instances they apply to */
INSERT INTO #ElasticJobApplicable(
	InstanceID,
	Priority,
	JobName,
	ExcludeJobName,
	RuleID,
	GroupID
)
SELECT	I.InstanceID,
		R.Priority,
		/* Normalize line breaks to CHAR(10) so we can split on that later. This allows users to enter values with line breaks from the UI */
		NULLIF(LTRIM(REPLACE(REPLACE(D.JobName,CHAR(13)+CHAR(10),CHAR(10)),CHAR(13),CHAR(10))),'') AS JobName,
		NULLIF(REPLACE(REPLACE(D.ExcludeJobName,CHAR(13)+CHAR(10),CHAR(10)),CHAR(13),CHAR(10)),'') AS ExcludeJobName,
		R.RuleID,
		R.GroupID
FROM Alert.Rules R
OUTER APPLY OPENJSON(CASE WHEN ISJSON(R.Details)=1 THEN R.Details ELSE N'{}' END)
WITH (
	JobName NVARCHAR(MAX) '$.JobName',
	ExcludeJobName NVARCHAR(MAX) '$.ExcludeJobName'
) D
CROSS APPLY Alert.ApplicableInstances_Get(R.RuleID,R.ApplyToTagID,R.ApplyToInstanceID,R.AlertKey,R.ApplyToHidden) I
WHERE R.Type = @Type
AND R.IsActive=1

DECLARE @AlertDetails Alert.AlertDetails

INSERT INTO @AlertDetails(
	InstanceID,
	Priority,
	Message,
	AlertKey,
	RuleID,
	GroupID
)
SELECT	J.InstanceID,
		EJA.Priority,
		CONCAT(J.job_name, N' elastic job failed', N' against ' + NULLIF(T.Targets,N'')),
		CONCAT(@AlertKeyPrefix, J.job_name) AS AlertKey,
		EJA.RuleID,
		EJA.GroupID
FROM dbo.ElasticJobs J
JOIN #ElasticJobApplicable EJA ON J.InstanceID = EJA.InstanceID
LEFT JOIN Alert.ElasticJobSnapshot EJS ON EJS.InstanceID = J.InstanceID AND EJS.job_id = J.job_id /* previous snapshot of recently failed jobs */
/* The last failed execution of the job - the one LastFailed is from.  LastFailed is when it completed, so the last to complete rather than to be created,
	and however long before that it was created. */
OUTER APPLY(SELECT TOP(1) E.job_execution_id, E.create_time
			FROM dbo.ElasticJobExecutions E
			CROSS APPLY dbo.ElasticJobOutcome(E.lifecycle,E.is_active) EO
			WHERE E.InstanceID = J.InstanceID
			AND E.job_id = J.job_id
			AND E.step_id = 0
			AND E.is_active = 0
			AND EO.IsFailed = 1
			AND E.create_time <= J.LastFailed
			AND ISNULL(E.end_time,E.create_time) <= J.LastFailed
			ORDER BY ISNULL(E.end_time,E.create_time) DESC) LastExec
/* The databases it failed against - the first 5 by name, then a count of the rest */
OUTER APPLY(SELECT STUFF((	SELECT N', ' + FT.Target + CASE WHEN FT.rnum = 5 AND FT.TargetCount > 5 THEN CONCAT(N' and ', FT.TargetCount - 5, N' others') ELSE N'' END
							FROM (	SELECT	COALESCE(F.target_database_name,F.target_elastic_pool_name,F.target_server_name) AS Target,
											ROW_NUMBER() OVER(ORDER BY COALESCE(F.target_database_name,F.target_elastic_pool_name,F.target_server_name)) AS rnum,
											COUNT(*) OVER() AS TargetCount
									FROM dbo.ElasticJobExecutions F
									CROSS APPLY dbo.ElasticJobOutcome(F.lifecycle,F.is_active) O
									WHERE F.InstanceID = J.InstanceID
									AND F.job_id = J.job_id
									AND F.step_id > 0
									AND F.job_execution_id = LastExec.job_execution_id
									AND F.create_time >= LastExec.create_time
									AND O.IsFailed = 1
									GROUP BY COALESCE(F.target_database_name,F.target_elastic_pool_name,F.target_server_name)
								) FT
							WHERE FT.rnum <= 5
							ORDER BY FT.rnum
							FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,2,N'') AS Targets
			) T
WHERE (J.LastFailed > EJS.LastFailed OR EJS.LastFailed IS NULL) /* Job has failed since the last time this proc was run */
AND J.LastFailed>=DATEADD(mi,-60,SYSUTCDATETIME()) /* Job has failed within the last hour */
AND (J.LastSucceeded< J.LastFailed OR J.LastSucceeded IS NULL) /* Last execution status is failed */
AND J.IsActive=1
AND J.enabled=1
/* Job name matches one of the supplied values or no values supplied. LIKE or = match */
AND	(
	EJA.JobName IS NULL
	OR EXISTS(	SELECT 1
				FROM STRING_SPLIT(EJA.JobName,CHAR(10)) SS
				OUTER APPLY (SELECT LTRIM(RTRIM(SS.value)) AS value) V
				WHERE V.value <> ''
				AND (
					J.job_name LIKE V.value
					OR J.job_name = V.value
					)
			)
	)
/* Job name does not match any of the supplied exclude values. LIKE or = match */
AND NOT EXISTS(	SELECT 1
				FROM STRING_SPLIT(EJA.ExcludeJobName,CHAR(10)) SS
				OUTER APPLY (SELECT LTRIM(RTRIM(SS.value)) AS value) V
				WHERE V.value <> ''
				AND (
					J.job_name LIKE V.value
					OR J.job_name = V.value
					)
				)

EXEC Alert.ActiveAlerts_Upd @AlertDetails=@AlertDetails,@AlertType=@Type,@ResolveAlertsOfType=0

/*	Resolve alerts unless the job is still failing: an enabled job of that name whose last execution failed.  Succeeded since, disabled,
	deleted or renamed resolves it.  Only the current job of that name counts - a job deleted and created again with the same name (a
	common way to redeploy elastic jobs) leaves the deleted one behind, inactive, and that mustn't resolve the new one's alert.
	Resolved once that has been the case for the rule's resolve delay. */
DECLARE @Cleared dbo.BigIDs
INSERT INTO @Cleared(ID)
SELECT AA.AlertID
FROM Alert.ActiveAlerts AA
WHERE AA.AlertType = @Type
AND AA.IsResolved=0
AND NOT EXISTS(	SELECT 1
				FROM dbo.ElasticJobs J
				WHERE J.InstanceID = AA.InstanceID
				AND J.job_name = STUFF(AA.AlertKey,1,LEN(@AlertKeyPrefix),'')
				AND J.IsActive = 1
				AND J.enabled = 1
				AND J.LastFailed IS NOT NULL
				AND (J.LastSucceeded < J.LastFailed OR J.LastSucceeded IS NULL)
				)

/* Every alert of the type was checked - one that hasn't cleared still has a failing job, even if the failure isn't new */
EXEC Alert.ActiveAlerts_Resolve @AlertIDs=@Cleared, @AlertType=@Type, @UnclearedAreActive=1

/* Update the snapshot of recently failed jobs */
TRUNCATE TABLE Alert.ElasticJobSnapshot
INSERT INTO Alert.ElasticJobSnapshot(
	InstanceID,
	job_id,
	LastFailed,
	LastSucceeded
)
SELECT	InstanceID,
		job_id,
		LastFailed,
		LastSucceeded
FROM #RecentJobs
