CREATE PROC Alert.AgentJobAlert_Upd
AS
DECLARE @Type VARCHAR(50)= 'AgentJob'
DECLARE @AlertKeyPrefix NVARCHAR(128) = 'Job:'
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

CREATE TABLE #RecentJobs(
	InstanceID INT NOT NULL,
	job_id UNIQUEIDENTIFIER NOT NULL,
	LastFailed DATETIME2 NULL,
	LastSucceeded DATETIME2 NULL,
	name NVARCHAR(128) NULL,
	category NVARCHAR(128) NULL,
	PRIMARY KEY(InstanceID,job_id)
)
/* Get a list of recently failed jobs */
INSERT INTO #RecentJobs(
	InstanceID,
	job_id,
	LastFailed,
	LastSucceeded,
	name,
	category
)
SELECT InstanceID,
	J.job_id,
	J.LastFailed,
	J.LastSucceeded,
	J.name,
	J.category
FROM dbo.Jobs J
WHERE J.LastFailed>=DATEADD(mi,-60,SYSUTCDATETIME())
AND J.IsActive=1
AND J.enabled=1

CREATE TABLE #AgentJobApplicable(
	InstanceID INT NOT NULL,
	Priority TINYINT NOT NULL,
	Category NVARCHAR(128) COLLATE DATABASE_DEFAULT NULL,
	JobName NVARCHAR(128) COLLATE DATABASE_DEFAULT NULL,
	RuleID INT NOT NULL
)
/* Get a list of rules and instances they apply to */
INSERT INTO #AgentJobApplicable(
	InstanceID,
	Priority,
	Category,
	JobName,
	RuleID
)
SELECT I.InstanceID,
		R.Priority,
		NULLIF(JSON_VALUE(R.Details,'$.Category'),'') AS Category,
		NULLIF(JSON_VALUE(R.Details,'$.JobName'),'') AS JobName,
		R.RuleID
FROM Alert.Rules R
CROSS APPLY Alert.ApplicableInstances_Get(R.ApplyToTagID,R.ApplyToInstanceID,R.AlertKey,R.ApplyToHidden) I
WHERE R.Type = @Type
AND R.IsActive=1

DECLARE @AlertDetails Alert.AlertDetails

INSERT INTO @AlertDetails(
	InstanceID,
	Priority,
	Message,
	AlertKey,
	RuleID
)
SELECT J.InstanceID,
		AJA.Priority, 
		J.name + ' job failed',
		CONCAT(@AlertKeyPrefix, J.name) AS AlertKey,
		AJA.RuleID
FROM dbo.Jobs J
JOIN #AgentJobApplicable AJA ON J.InstanceID = AJA.InstanceID 
		AND (J.category LIKE AJA.Category OR AJA.Category IS NULL)
		AND(J.name LIKE AJA.JobName OR AJA.JobName IS NULL)
LEFT JOIN Alert.AgentJobSnapshot SS ON SS.InstanceID = J.InstanceID AND SS.job_id = J.job_id /* previous snapshot of recently failed jobs */
WHERE (J.LastFailed > SS.LastFailed OR SS.LastFailed IS NULL) /* Job has failed since the last time this proc was run */
AND J.LastFailed>=DATEADD(mi,-60,SYSUTCDATETIME()) /* Job has failed within the last hour */
AND (J.LastSucceeded< J.LastFailed OR J.LastSucceeded IS NULL) /* Last execution status is failed */

EXEC Alert.ActiveAlerts_Upd @AlertDetails=@AlertDetails,@AlertType=@Type,@ResolveAlertsOfType=0

/* Resolve any existing AgentJob alerts if the job has succeeded since it last failed */
UPDATE AA
		SET ResolvedDate = SYSUTCDATETIME(),
		IsResolved = 1,
		LastMessage = 'Issue resolved',
		UpdateCount +=1,
		ResolvedCount +=1,
		UpdatedDate = SYSUTCDATETIME()
FROM Alert.ActiveAlerts AA
WHERE AA.AlertType = @Type
AND EXISTS(SELECT 1 
			FROM dbo.Jobs J 
			WHERE J.InstanceID = AA.InstanceID 
			AND J.name = STUFF(AA.AlertKey,1,LEN(@AlertKeyPrefix),'') 
			AND (J.LastSucceeded>LastFailed OR J.enabled=0)
			)
AND AA.IsResolved=0

/* Update the snapshot or recently failed jobs */
TRUNCATE TABLE Alert.AgentJobSnapshot
INSERT INTO Alert.AgentJobSnapshot(
	InstanceID,
	job_id,
	LastFailed,
	LastSucceeded
)
SELECT InstanceID,
	job_id,
	LastFailed,
	LastSucceeded
FROM #RecentJobs

