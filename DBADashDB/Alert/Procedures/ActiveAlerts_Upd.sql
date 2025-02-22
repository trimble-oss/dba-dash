CREATE PROC Alert.ActiveAlerts_Upd(
	@AlertDetails AlertDetails READONLY,
	@AlertType VARCHAR(50),
	@ResolveAlertsOfType BIT=1
)
AS
/* 
	Updates the list of active alerts for the alert type, inserting new alerts, updating existing ones and resolving alerts that are no longer present in alert details.

*/
DECLARE @AD AlertDetails;
/* 
	Ensure AlertKey is unique for each instance.  
	Take the highest priority rule.
*/
WITH DeDupe AS (
	SELECT InstanceID,
		Priority,
		AlertKey,
		Message, 
		RuleID,
		ROW_NUMBER() OVER(PARTITION BY InstanceID,AlertKey ORDER BY Priority,RuleID) AS rnum 
	FROM @AlertDetails
)
INSERT INTO @AD(
		InstanceID,
		Priority,
		AlertKey,
		Message,  
		RuleID
		)
SELECT InstanceID,
		Priority,
		AlertKey,
		Message,  
		RuleID
FROM DeDupe
WHERE rnum=1

IF @ResolveAlertsOfType=1
BEGIN
	/* Resolve existing issues if there is an existing alert that is no longer active */
	UPDATE AA
		SET ResolvedDate = SYSUTCDATETIME(),
		IsResolved = 1,
		LastMessage = 'Issue resolved',
		UpdateCount +=1,
		ResolvedCount +=1,
		UpdatedDate = SYSUTCDATETIME()
	FROM Alert.ActiveAlerts AA
	WHERE AlertType = @AlertType
	AND NOT EXISTS(SELECT 1 
				FROM @AD AD
				WHERE AD.InstanceID = AA.InstanceID
				AND AD.AlertKey = AA.AlertKey
				)
	AND IsResolved=0
	AND IsBlackout=0
END

/* Update active alerts with current date and message */
UPDATE AA
	SET UpdatedDate = SYSUTCDATETIME(),
	LastMessage = A.Message,
	IsResolved = 0,
	UpdateCount +=1,
	Escalated = CASE WHEN A.Priority < AA.Priority THEN SYSUTCDATETIME() ELSE AA.Escalated END,
	DeEscalated = CASE WHEN A.Priority > AA.Priority THEN SYSUTCDATETIME() ELSE AA.DeEscalated END,
	Priority = A.Priority,
	RuleID = A.RuleID
FROM Alert.ActiveAlerts AA
JOIN @AD A ON AA.InstanceID = A.InstanceID AND AA.AlertKey = A.AlertKey
WHERE AA.AlertType = @AlertType
AND AA.IsBlackout=0

/* Add new alerts */
INSERT INTO Alert.ActiveAlerts(InstanceID,Priority,AlertType,AlertKey,FirstMessage,LastMessage,RuleID)
SELECT AD.InstanceID,AD.Priority,@AlertType,AD.AlertKey,AD.Message,AD.Message,AD.RuleID
FROM @AD AD
OUTER APPLY Alert.IsBlackoutPeriod(AD.InstanceID,AD.AlertKey) BP
WHERE NOT EXISTS(SELECT 1 
			FROM Alert.ActiveAlerts AA
			WHERE AD.InstanceID = AA.InstanceID
			AND AD.AlertKey = AA.AlertKey
			)
AND BP.IsBlackout=0