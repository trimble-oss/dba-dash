CREATE PROC Alert.FailedLoginsAlert_Upd
AS
/*
	Get instances that fail the FailedLogins alert rule & update the active alerts.
	Triggers when the number of failed logins over the evaluation period meets or exceeds the threshold.
*/
DECLARE @Type VARCHAR(50)='FailedLogins';

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
	AlertKey NVARCHAR(256) COLLATE DATABASE_DEFAULT NOT NULL,
	InstanceID INT NOT NULL,
	Priority TINYINT NOT NULL,
	EvaluationPeriodMins INT NOT NULL,
	Threshold BIGINT NOT NULL,
	GroupID INT NOT NULL DEFAULT(0)
);

/* Get thresholds that apply to each instance. If multiple thresholds apply with the same evaluation period & priority, take the lowest threshold*/
WITH T AS (
	SELECT 	R.RuleID,
		R.AlertKey,
		I.InstanceID,
		R.Priority,
		R.EvaluationPeriodMins,
		TRY_CAST(R.Threshold AS BIGINT) AS Threshold,
		R.GroupID,
		ROW_NUMBER() OVER(PARTITION BY I.InstanceID,R.GroupID,R.EvaluationPeriodMins,R.Priority ORDER BY R.Threshold,R.RuleID) rnum
	FROM Alert.Rules R
	CROSS APPLY Alert.ApplicableInstances_Get(R.ApplyToTagID,R.ApplyToInstanceID,R.AlertKey,R.ApplyToHidden) I
	WHERE R.Type = @Type
	AND R.Threshold>=1
	AND R.IsActive=1
)
INSERT INTO #EffectiveThresholds(
	RuleID,
	AlertKey,
	InstanceID,
	Priority,
	EvaluationPeriodMins,
	Threshold,
	GroupID
)
SELECT T.RuleID,
	T.AlertKey,
	T.InstanceID,
	T.Priority,
	ISNULL(T.EvaluationPeriodMins,60),
	T.Threshold,
	T.GroupID
FROM T
WHERE rnum=1;

DECLARE @AlertDetails Alert.AlertDetails;

WITH T AS (
	SELECT T.RuleID,
			T.InstanceID,
			T.Priority,
			T.EvaluationPeriodMins,
			T.AlertKey,
			T.GroupID,
			T.Threshold,
			agg.FailedLoginCount,
			ROW_NUMBER() OVER(PARTITION BY T.InstanceID,T.GroupID ORDER BY T.Priority,T.RuleID) rnum
	FROM #EffectiveThresholds T
	CROSS APPLY(SELECT COUNT_BIG(*) AS FailedLoginCount
				FROM dbo.FailedLogins FL
				WHERE FL.InstanceID = T.InstanceID
				AND FL.LogDate >= CAST(DATEADD(mi,-T.EvaluationPeriodMins,SYSUTCDATETIME()) AS DATETIME2(3))
				AND FL.LogDate <= CAST(SYSUTCDATETIME() AS DATETIME2(3))
				) agg
	WHERE agg.FailedLoginCount >= T.Threshold
)
/* Get instances reaching the failed logins threshold, de-duplicated by instance */
INSERT INTO @AlertDetails(
		InstanceID,
		Priority,
		Message,
		AlertKey,
		RuleID,
		GroupID
)
SELECT	T.InstanceID,
		T.Priority,
		CONCAT(I.ConnectionID,' had ',T.FailedLoginCount,' failed logins over ',T.EvaluationPeriodMins,'mins (threshold: ',T.Threshold,')') AS Message,
		T.AlertKey,
		T.RuleID,
		T.GroupID
FROM T
JOIN dbo.Instances I ON T.InstanceID = I.InstanceID
WHERE T.rnum=1

EXEC Alert.ActiveAlerts_Upd @AlertDetails=@AlertDetails,@AlertType=@Type
