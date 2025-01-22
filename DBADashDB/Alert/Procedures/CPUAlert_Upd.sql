CREATE PROC Alert.CPUAlert_Upd
AS
/* 
	Get instances that fail the CPU alert rule & update the active alerts
*/
DECLARE @Type VARCHAR(50)='CPU';

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

CREATE TABLE #EffectiveThresholds(
	RuleID INT NOT NULL,
	AlertKey NVARCHAR(128) COLLATE DATABASE_DEFAULT NOT NULL,	
	InstanceID INT NOT NULL,
	Priority TINYINT NOT NULL,
	EvaluationPeriodMins TINYINT NOT NULL,
	Threshold TINYINT NOT NULL,
)
CREATE TABLE #CPUAlert(
	InstanceID INT NOT NULL PRIMARY KEY,
	Priority TINYINT NOT NULL,
	Message NVARCHAR(MAX) COLLATE DATABASE_DEFAULT NOT NULL
);

/* Get thresholds that apply to each instance. If multiple thresholds apply with the same evaluation period & priority, take the lowest threshold*/
WITH T AS (
	SELECT 	R.RuleID,
		R.AlertKey,
		I.InstanceID,
		R.Priority,
		R.EvaluationPeriodMins,
		TRY_CAST(R.Threshold AS TINYINT) AS Threshold,
		ROW_NUMBER() OVER(PARTITION BY I.InstanceID,R.EvaluationPeriodMins,R.Priority ORDER BY R.Threshold,R.RuleID) rnum
	FROM Alert.Rules R
	CROSS APPLY Alert.ApplicableInstances_Get(R.ApplyToTagID,R.ApplyToInstanceID,R.AlertKey,R.ApplyToHidden) I
	WHERE R.Type = @Type
	AND R.Threshold<=100
	AND R.IsActive=1
)
INSERT INTO #EffectiveThresholds(
	RuleID,
	AlertKey,
    InstanceID,
    Priority,
    EvaluationPeriodMins,
    Threshold
)
SELECT T.RuleID,
	T.AlertKey,
    T.InstanceID,
    T.Priority,
    ISNULL(T.EvaluationPeriodMins,5),
    T.Threshold 
FROM T 
WHERE rnum=1;

DECLARE @AlertDetails Alert.AlertDetails;

WITH T AS (
	SELECT T.RuleID,
			T.InstanceID,
			T.Priority,
			T.EvaluationPeriodMins,
			T.AlertKey,
			agg.AvgCPU,
			ROW_NUMBER() OVER(PARTITION BY T.InstanceID ORDER BY T.Priority,T.RuleID) rnum
	FROM #EffectiveThresholds T
	CROSS APPLY(SELECT AVG(TotalCPU) AvgCPU
				FROM dbo.CPU 
				WHERE T.InstanceID = CPU.InstanceID
				AND CPU.EventTime >=CAST(DATEADD(mi,-EvaluationPeriodMins,SYSUTCDATETIME()) AS DATETIME2(3))
				AND CPU.EventTime >=CAST(DATEADD(mi,-60,SYSUTCDATETIME()) AS DATETIME2(3)) /* Hard cap at 60min evaluation period for performance */	
				AND CPU.EventTime <= CAST(SYSUTCDATETIME() AS DATETIME2(3)) 
				) agg
	WHERE agg.AvgCPU >T.Threshold
)
/* Get instances exceeding CPU alert threshold, de-duplicated by instance */
INSERT INTO @AlertDetails(
		InstanceID,
		Priority,
		Message,
		AlertKey,
		RuleID
)
SELECT	T.InstanceID,
		T.Priority,
		CONCAT(I.ConnectionID,' exceeded CPU threshold, averaging ',T.AvgCPU,'% over ',T.EvaluationPeriodMins,'mins') AS Message,
		T.AlertKey,
		T.RuleID
FROM T 
JOIN dbo.Instances I ON T.InstanceID = I.InstanceID
WHERE T.rnum=1

EXEC Alert.ActiveAlerts_Upd @AlertDetails=@AlertDetails,@AlertType=@Type