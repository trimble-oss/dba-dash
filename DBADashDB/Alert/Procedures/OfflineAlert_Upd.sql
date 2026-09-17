CREATE PROC Alert.OfflineAlert_Upd
AS
SET NOCOUNT ON
DECLARE @Type VARCHAR(50)='OFFLINE';

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
	InstanceID INT NOT NULL,
	AlertKey NVARCHAR(256) COLLATE DATABASE_DEFAULT NOT NULL,
	Priority INT NOT NULL,
	EvaluationPeriodMins INT NOT NULL,
	RuleID INT NOT NULL,
	GroupID INT NOT NULL DEFAULT(0),
	PRIMARY KEY(InstanceID,GroupID)
);

/* Get the Offline rules that apply to each instance, ensuring we have a single rule per instance and group. */
WITH DeDupe AS (
	SELECT I.InstanceID,
			R.AlertKey,
			R.Priority,
			R.RuleID,
			ISNULL(R.EvaluationPeriodMins,0) AS EvaluationPeriodMins,
			R.GroupID,
			ROW_NUMBER() OVER(PARTITION BY I.InstanceID,R.GroupID ORDER BY R.Priority, R.RuleID) rnum
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
	RuleID,
	GroupID
)
SELECT	InstanceID,
		AlertKey,
		Priority,
		EvaluationPeriodMins,
		RuleID,
		GroupID
FROM DeDupe
WHERE rnum=1

INSERT INTO @AlertDetails
(
	InstanceID,
	Priority,
	AlertKey,
	Message,
	RuleID,
	GroupID
)
SELECT	OI.InstanceID,
		I.Priority,
		I.AlertKey,
		IIF(I.EvaluationPeriodMins>0,CONCAT('Instance has been offline for more than ',I.EvaluationPeriodMins,'mins'),'Instance is offline'),
		I.RuleID,
		I.GroupID
FROM dbo.OfflineInstances OI
JOIN #Instances I ON OI.InstanceID = I.InstanceID
WHERE OI.IsCurrent=1
/* Optionally wait for the instance to be offline for a period of time before alerting to avoid alerts for short blips */
AND OI.FirstFail <= DATEADD(mi,-I.EvaluationPeriodMins,SYSUTCDATETIME())

EXEC Alert.ActiveAlerts_Upd @AlertDetails=@AlertDetails,@AlertType=@Type