CREATE PROC Alert.AgentJobAlert_Upd
AS
DECLARE @Type VARCHAR(50)= 'AgentJob'
DECLARE @AlertKeyPrefix NVARCHAR(256) = 'Job:'
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
	Category NVARCHAR(MAX) COLLATE DATABASE_DEFAULT NULL,
	JobName NVARCHAR(MAX) COLLATE DATABASE_DEFAULT NULL,
	ExcludeCategory NVARCHAR(MAX) COLLATE DATABASE_DEFAULT NULL,
	ExcludeJobName NVARCHAR(MAX) COLLATE DATABASE_DEFAULT NULL,
	RuleID INT NOT NULL
)
/* Get a list of rules and instances they apply to */
INSERT INTO #AgentJobApplicable(
	InstanceID,
	Priority,
	Category,
	JobName,
	ExcludeCategory,
	ExcludeJobName,
	RuleID
)
SELECT I.InstanceID,
		R.Priority,
		/* Normalize line breaks to CHAR(10) so we can split on that later. This allows users to enter values with line breaks from the UI */
		NULLIF(LTRIM(REPLACE(REPLACE(D.Category,CHAR(13)+CHAR(10),CHAR(10)),CHAR(13),CHAR(10))),'') AS Category,
		NULLIF(LTRIM(REPLACE(REPLACE(D.JobName,CHAR(13)+CHAR(10),CHAR(10)),CHAR(13),CHAR(10))),'') AS JobName,
		NULLIF(REPLACE(REPLACE(D.ExcludeCategory,CHAR(13)+CHAR(10),CHAR(10)),CHAR(13),CHAR(10)),'') AS ExcludeCategory,
		NULLIF(REPLACE(REPLACE(D.ExcludeJobName,CHAR(13)+CHAR(10),CHAR(10)),CHAR(13),CHAR(10)),'') AS ExcludeJobName,
		R.RuleID
FROM Alert.Rules R
OUTER APPLY OPENJSON(CASE WHEN ISJSON(R.Details)=1 THEN R.Details ELSE N'{}' END)
WITH (
	Category NVARCHAR(MAX) '$.Category',
	JobName NVARCHAR(MAX) '$.JobName',
	ExcludeCategory NVARCHAR(MAX) '$.ExcludeCategory',
	ExcludeJobName NVARCHAR(MAX) '$.ExcludeJobName'
) D
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
LEFT JOIN Alert.AgentJobSnapshot AJS ON AJS.InstanceID = J.InstanceID AND AJS.job_id = J.job_id /* previous snapshot of recently failed jobs */
WHERE (J.LastFailed > AJS.LastFailed OR AJS.LastFailed IS NULL) /* Job has failed since the last time this proc was run */
AND J.LastFailed>=DATEADD(mi,-60,SYSUTCDATETIME()) /* Job has failed within the last hour */
AND (J.LastSucceeded< J.LastFailed OR J.LastSucceeded IS NULL) /* Last execution status is failed */
/* Job name matches one of the supplied values or no values supplied */
AND	(
	AJA.JobName IS NULL 
	OR EXISTS(	SELECT 1 
				FROM STRING_SPLIT(AJA.JobName,CHAR(10)) SS 
				OUTER APPLY (SELECT LTRIM(RTRIM(SS.value)) AS value) T
				WHERE T.value <> '' 
				AND (
					J.name LIKE T.value
					OR J.name = T.value
					)					
			)
	)
/* Category matches one of the supplied values or no values supplied. LIKE or = match */
AND (AJA.Category IS NULL
	OR EXISTS(	SELECT 1 
				FROM STRING_SPLIT(AJA.Category,CHAR(10)) SS 
				OUTER APPLY (SELECT LTRIM(RTRIM(SS.value)) AS value) T
				WHERE T.value <> '' 
				AND (
					J.category LIKE T.value
					OR J.category = T.value
					)
				)
	)
/* Job name does not match any of the supplied exclude values. LIKE or = match */
AND NOT EXISTS(	SELECT 1 
				FROM STRING_SPLIT(AJA.ExcludeJobName,CHAR(10)) SS 
				OUTER APPLY (SELECT LTRIM(RTRIM(SS.value)) AS value) T
				WHERE T.value <> '' 
				AND (
					J.name LIKE T.value
					OR J.name = T.value
					)
				)
/* Category does not match any of the supplied exclude values. LIKE or = match */
AND NOT EXISTS(	SELECT 1 
				FROM STRING_SPLIT(AJA.ExcludeCategory,CHAR(10)) SS 
				OUTER APPLY (SELECT LTRIM(RTRIM(SS.value)) AS value) T
				WHERE T.value <> '' 
				AND (
					J.category LIKE T.value
					OR J.category = T.value
					)
				)


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

