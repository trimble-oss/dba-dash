CREATE PROC Alert.RestartAlert_Upd
AS
SET NOCOUNT ON
DECLARE @Type VARCHAR(50)='RESTART';

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

DECLARE @AlertDetails Alert.AlertDetails;

CREATE TABLE #Instances(
	InstanceID INT NOT NULL PRIMARY KEY,
	AlertKey NVARCHAR(128) COLLATE DATABASE_DEFAULT NOT NULL,
	Priority INT NOT NULL,
	EvaluationPeriodMins INT NOT NULL,
	RuleID INT NOT NULL
);

/* Get the Offline rules that apply to each instance, ensuring thewe have a single rule per instance. */
WITH DeDupe AS (
	SELECT I.InstanceID,
			R.AlertKey,
			R.Priority,
			R.RuleID,
			ISNULL(R.EvaluationPeriodMins,10) aS EvaluationPeriodMins,
			ROW_NUMBER() OVER(PARTITION BY I.InstanceID ORDER BY R.Priority, R.RuleID) rnum
	FROM Alert.Rules R
	CROSS APPLY Alert.ApplicableInstances_Get(R.ApplyToTagID,R.ApplyToInstanceID,R.AlertKey,R.ApplyToHidden) I
	WHERE R.Type = @Type
	AND R.IsActive=1
)
INSERT INTO #Instances(
    InstanceID,
	AlertKey,
	Priority,
	EvaluationPeriodMins,
	RuleID
)
SELECT	InstanceID,
		AlertKey,
		Priority,
		EvaluationPeriodMins,
		RuleID 
FROM DeDupe 
WHERE rnum=1

INSERT INTO @AlertDetails
(
    InstanceID,
    Priority,
	AlertKey,
    Message,
	RuleID
)
SELECT	T.InstanceID,
		T.Priority,
		T.AlertKey,
		CONCAT('Instance restarted within the last ', T.EvaluationPeriodMins,'mins'),
		T.RuleID
FROM dbo.Instances I
JOIN #Instances T ON I.InstanceID = T.InstanceID
WHERE I.IsActive=1
AND DATEADD(mi,I.UTCOffset,I.sqlserver_start_time) >= DATEADD(mi,-T.EvaluationPeriodMins,GETUTCDATE())

EXEC Alert.ActiveAlerts_Upd @AlertDetails=@AlertDetails,@AlertType=@Type