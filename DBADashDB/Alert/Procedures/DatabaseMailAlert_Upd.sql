CREATE PROC Alert.DatabaseMailAlert_Upd
AS
/*
	Get instances that fail the DatabaseMail alert rule & update the active alerts.
	Triggers when the collected Database Mail status is not 'STARTED' and doesn't match one of the excluded status values.

	The status is collected via msdb.dbo.sysmail_help_status_sp and stored in dbo.Instances.DBMailStatus.
	Values are 'STARTED', 'STOPPED' or '<ErrorNumber>|<ErrorMessage>' when the status can't be read (e.g.
	'15281|...' when Database Mail XPs is disabled or '229|...' when EXECUTE permission is denied).  These error
	statuses can be excluded via the ExcludedStatuses list on the rule (which supports LIKE syntax).
*/
SET NOCOUNT ON
DECLARE @Type VARCHAR(50)='DatabaseMail';

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

CREATE TABLE #Instances(
	InstanceID INT NOT NULL,
	AlertKey NVARCHAR(256) COLLATE DATABASE_DEFAULT NOT NULL,
	Priority TINYINT NOT NULL,
	RuleID INT NOT NULL,
	ExcludedStatuses NVARCHAR(MAX) COLLATE DATABASE_DEFAULT NULL,
	GroupID INT NOT NULL DEFAULT(0),
	PRIMARY KEY(InstanceID,GroupID)
);

/* Get the rules that apply to each instance, ensuring we have a single rule per instance and group. */
WITH DeDupe AS (
	SELECT I.InstanceID,
			R.AlertKey,
			R.Priority,
			R.RuleID,
			CASE
				WHEN JSON_VALUE(R.Details,'$.ExcludedStatuses[0]') IS NULL THEN '["15281|%","229|%"]'
				ELSE JSON_QUERY(R.Details,'$.ExcludedStatuses')
			END AS ExcludedStatuses,
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
	RuleID,
	ExcludedStatuses,
	GroupID
)
SELECT	InstanceID,
		AlertKey,
		Priority,
		RuleID,
		ExcludedStatuses,
		GroupID
FROM DeDupe
WHERE rnum=1

DECLARE @AlertDetails Alert.AlertDetails;

INSERT INTO @AlertDetails
(
	InstanceID,
	Priority,
	AlertKey,
	Message,
	RuleID,
	GroupID
)
SELECT	AI.InstanceID,
		AI.Priority,
		AI.AlertKey,
		CONCAT('Database Mail status: ',I.DBMailStatus),
		AI.RuleID,
		AI.GroupID
FROM #Instances AI
JOIN dbo.Instances I ON AI.InstanceID = I.InstanceID
WHERE I.DBMailStatus <> 'STARTED'
AND I.DBMailStatus IS NOT NULL
AND I.DBMailStatus <> ''
AND NOT EXISTS(
	SELECT 1
	FROM OPENJSON(AI.ExcludedStatuses)
	/* Trim the pattern so exclusions still match if the user leaves leading/trailing whitespace when editing the JSON */
	WHERE I.DBMailStatus LIKE LTRIM(RTRIM(value))
)

EXEC Alert.ActiveAlerts_Upd @AlertDetails=@AlertDetails,@AlertType=@Type
