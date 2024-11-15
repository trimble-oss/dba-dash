CREATE PROC Alert.WaitAlert_Upd 
AS
/* 
	Get instances that fail the Wait alert rule & update the active alerts
*/
DECLARE @Type VARCHAR(50)='Wait';

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

CREATE TABLE #EffectiveThresholds (
	InstanceID INT NOT NULL,
	RuleID INT NOT NULL,
	EvaluationPeriodMins TINYINT NOT NULL,
	Priority TINYINT NOT NULL,
	WaitType NVARCHAR(60) COLLATE DATABASE_DEFAULT NOT NULL,
	TotalWaitMsPerSec DECIMAL(18,2) NULL,
	TotalWaitMsPerSecPerCore DECIMAL(18,2) NULL,
	AvgWaitTimeMsPerSec DECIMAL(18,2) NULL,
	scheduler_count INT NOT NULL
)
/* Get rules and the instances they apply to */
INSERT INTO #EffectiveThresholds(
	InstanceID,
	RuleID,
	EvaluationPeriodMins,
	Priority,
	WaitType,
	TotalWaitMsPerSec,
	TotalWaitMsPerSecPerCore,
	AvgWaitTimeMsPerSec,
	scheduler_count
)

SELECT 	I.InstanceID,
		R.RuleID,
		ISNULL(R.EvaluationPeriodMins,5),
		R.Priority,	
		TRY_CAST(JSON_VALUE(R.Details,'$.WaitType') AS NVARCHAR(60)) AS WaitType,
		CASE WHEN TRY_CAST(JSON_VALUE(R.Details,'$.IsPerCore') AS BIT)=1 THEN NULL ELSE R.Threshold END,
		CASE WHEN TRY_CAST(JSON_VALUE(R.Details,'$.IsPerCore') AS BIT)=1 THEN R.Threshold ELSE NULL END,
		TRY_CAST(JSON_VALUE(R.Details,'$.AvgWaitTimeMsPerSec') AS DECIMAL(28,9)),
		I.scheduler_count
FROM Alert.Rules R 
CROSS APPLY Alert.ApplicableInstances_Get(R.ApplyToTagID,R.ApplyToInstanceID,R.AlertKey) I
WHERE R.Type = @Type
AND R.IsActive=1

DECLARE @MaxEvaluationPeriodMins INT
SELECT @MaxEvaluationPeriodMins = MAX(EvaluationPeriodMins) 
FROM #EffectiveThresholds

CREATE TABLE #WaitAlert(
	InstanceID INT NOT NULL PRIMARY KEY,
	Priority TINYINT NOT NULL,
	Message NVARCHAR(MAX) NOT NULL
);

WITH SS AS (
	SELECT	T.InstanceID,
			T.EvaluationPeriodMins,
			NULLIF(DATEDIFF(ms,FSS.SnapshotDate,LSS.SnapshotDate)/1000.0,0) AS DurationSeconds
	FROM (SELECT DISTINCT InstanceID,EvaluationPeriodMins FROM #EffectiveThresholds) AS T
	/* Get date of last snapshot for the instance for accurate calculation of ms/sec */
	CROSS APPLY(SELECT TOP(1) SnapshotDate 
			FROM dbo.Waits W WITH(FORCESEEK)
			WHERE W.InstanceID = T.InstanceID
			AND W.SnapshotDate >= CAST(DATEADD(mi,-@MaxEvaluationPeriodMins,SYSUTCDATETIME()) AS DATETIME2(2))
			AND W.SnapshotDate <= CAST(SYSUTCDATETIME() AS DATETIME2(2))
			ORDER BY SnapshotDate DESC) LSS
	/* Get date of first snapshot for the instance within the evaluation period for accurate calculation of ms/sec */
	CROSS APPLY(SELECT TOP(1) SnapshotDate 
			FROM dbo.Waits W WITH(FORCESEEK)
			WHERE W.InstanceID = T.InstanceID
			AND W.SnapshotDate >= CAST(DATEADD(mi,-@MaxEvaluationPeriodMins,SYSUTCDATETIME()) AS DATETIME2(2))
			AND W.SnapshotDate >= DATEADD(mi,-T.EvaluationPeriodMins,SYSUTCDATETIME())
			AND W.SnapshotDate <= CAST(SYSUTCDATETIME() AS DATETIME2(2))
			ORDER BY SnapshotDate ASC) FSS
)
SELECT T.RuleID,
		T.InstanceID,
		T.WaitType,
		T.Priority,
		AGG.TotalWaitMsPerSec,
		AGG.TotalWaitMsPerSecPerCore,
		AGG.AvgWaitTimeMs
INTO #ExceededThreshold
FROM #EffectiveThresholds T 
INNER JOIN SS ON T.InstanceID = SS.InstanceID AND T.EvaluationPeriodMins = SS.EvaluationPeriodMins
CROSS APPLY(
		SELECT 	SUM(W.wait_time_ms) / SS.DurationSeconds AS TotalWaitMsPerSec,
				SUM(W.wait_time_ms) / SS.DurationSeconds / T.scheduler_count  AS TotalWaitMsPerSecPerCore,
				SUM(W.wait_time_ms) / NULLIF(SUM(W.waiting_tasks_count),0) AS AvgWaitTimeMs 
		FROM dbo.Waits W WITH(FORCESEEK)
		JOIN dbo.WaitType WT ON W.WaitTypeID = WT.WaitTypeID
		WHERE W.InstanceID = T.InstanceID
		AND W.SnapshotDate >= CAST(DATEADD(mi,-@MaxEvaluationPeriodMins,SYSUTCDATETIME()) AS DATETIME2(2))
		AND W.SnapshotDate >= DATEADD(mi,-T.EvaluationPeriodMins,SYSUTCDATETIME())
		AND W.SnapshotDate <= CAST(SYSUTCDATETIME() AS DATETIME2(2))
		AND WT.WaitType LIKE T.WaitType COLLATE DATABASE_DEFAULT
		) AGG
WHERE (AGG.TotalWaitMsPerSec >=T.TotalWaitMsPerSec OR T.TotalWaitMsPerSec IS NULL)
AND (AGG.TotalWaitMsPerSecPerCore >=T.TotalWaitMsPerSecPerCore OR T.TotalWaitMsPerSecPerCore IS NULL)
AND (AGG.AvgWaitTimeMs >= T.AvgWaitTimeMsPerSec  OR T.AvgWaitTimeMsPerSec IS NULL)

DECLARE @AlertDetails Alert.AlertDetails;

INSERT INTO @AlertDetails(
	InstanceID,
	Priority,
	Message,
	AlertKey,
	RuleID
)
SELECT InstanceID,
		Priority, 
		CONCAT('Wait Type ', WaitType,' exceeded the specified threshold.  Total Wait :',
				FORMAT(TotalWaitMsPerSec,'N1'),'ms/sec',' / ',FORMAT(TotalWaitMsPerSecPerCore,'N1'),
				' ms/sec/core. Avg Wait: ', FORMAT(AvgWaitTimeMs,'N1'),'ms'),
	'Wait - ' + WaitType AS AlertKey,
	RuleID
FROM #ExceededThreshold

EXEC Alert.ActiveAlerts_Upd @AlertDetails=@AlertDetails,@AlertType=@Type
