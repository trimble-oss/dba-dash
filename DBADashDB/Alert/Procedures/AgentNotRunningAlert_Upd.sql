CREATE PROC Alert.AgentNotRunningAlert_Upd
AS
DECLARE @Type VARCHAR(50)= 'AgentNotRunning'
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
PRINT CONCAT('Processing alerts of type ',@Type);

DECLARE @AlertDetails Alert.AlertDetails;

INSERT INTO @AlertDetails(
	InstanceID,
	Priority,
	Message,
	AlertKey,
	RuleID,
	GroupID
)
SELECT I.InstanceID,
		R.Priority,
		'SQL Server agent is not running',
		R.AlertKey,
		R.RuleID,
		R.GroupID
FROM Alert.Rules R
CROSS APPLY Alert.ApplicableInstances_Get(R.ApplyToTagID,R.ApplyToInstanceID,R.AlertKey,R.ApplyToHidden) AI
JOIN dbo.Instances I ON AI.InstanceID = I.InstanceID
WHERE R.Type = @Type
AND R.IsActive=1
AND I.IsAgentRunning=0
AND I.EngineEdition<>4 /* Exclude Express */

EXEC Alert.ActiveAlerts_Upd @AlertDetails=@AlertDetails,@AlertType=@Type,@ResolveAlertsOfType=1;
