CREATE PROC Alert.ActiveAlerts_Upd(
	@AlertDetails AlertDetails READONLY,
	@AlertType VARCHAR(50),
	@ResolveAlertsOfType BIT=1
)
AS
/* 
	Updates the list of active alerts for the alert type, inserting new alerts, updating existing ones and resolving alerts that are no longer present in alert details.

	A rule's resolve delay keeps the alert open while the issue has cleared for less than that time - see Alert.ActiveAlerts_Resolve.
	If the issue comes back in the meantime it's the same episode: ActiveSince isn't reset.
	@ResolveAlertsOfType=0 is for alert procs that work out for themselves when their alerts are resolved.  They call Alert.ActiveAlerts_Resolve.
*/
DECLARE @AD AlertDetails;
DECLARE @Now DATETIME2 = SYSUTCDATETIME();
/* 
	Ensure AlertKey is unique for each instance and group.  
	Take the highest priority rule.
*/
WITH DeDupe AS (
	SELECT InstanceID,
		Priority,
		AlertKey,
		Message, 
		RuleID,
		GroupID,
		ROW_NUMBER() OVER(PARTITION BY InstanceID,AlertKey,GroupID ORDER BY Priority,RuleID) AS rnum 
	FROM @AlertDetails
)
INSERT INTO @AD(
		InstanceID,
		Priority,
		AlertKey,
		Message,  
		RuleID,
		GroupID
		)
SELECT InstanceID,
		Priority,
		AlertKey,
		Message,  
		RuleID,
		GroupID
FROM DeDupe
WHERE rnum=1

IF @ResolveAlertsOfType=1
BEGIN
	/* Existing alerts that are no longer active */
	DECLARE @Cleared dbo.BigIDs
	INSERT INTO @Cleared(ID)
	SELECT AA.AlertID
	FROM Alert.ActiveAlerts AA
	WHERE AA.AlertType = @AlertType
	AND NOT EXISTS(SELECT 1 
				FROM @AD AD
				WHERE AD.InstanceID = AA.InstanceID
				AND AD.AlertKey = AA.AlertKey
				AND AD.GroupID = AA.GroupID
				)
	AND AA.IsResolved=0
	AND AA.IsBlackout=0

	EXEC Alert.ActiveAlerts_Resolve @AlertIDs=@Cleared, @AlertType=@AlertType
END

/* 
	Update active alerts with current date and message.
	A resolved alert that's triggered again starts a new episode.  An alert that was waiting out its resolve delay carries on with the same one.
*/
UPDATE AA
	SET UpdatedDate = SYSUTCDATETIME(),
	LastMessage = A.Message,
	ActiveSince = CASE WHEN AA.IsResolved=1 THEN @Now ELSE ISNULL(AA.ActiveSince,AA.TriggerDate) END,
	ResumedDate = CASE WHEN AA.IsResolved=0 AND AA.ResolvingSince IS NOT NULL THEN @Now ELSE AA.ResumedDate END,
	ResolvingSince = NULL,
	IsResolved = 0,
	UpdateCount +=1,
	Escalated = CASE WHEN A.Priority < AA.Priority THEN SYSUTCDATETIME() ELSE AA.Escalated END,
	DeEscalated = CASE WHEN A.Priority > AA.Priority THEN SYSUTCDATETIME() ELSE AA.DeEscalated END,
	Priority = A.Priority,
	RuleID = A.RuleID,
	GroupID = A.GroupID
FROM Alert.ActiveAlerts AA
JOIN @AD A ON AA.InstanceID = A.InstanceID AND AA.AlertKey = A.AlertKey AND AA.GroupID = A.GroupID
WHERE AA.AlertType = @AlertType
AND AA.IsBlackout=0

/* Add new alerts */
INSERT INTO Alert.ActiveAlerts(InstanceID,Priority,AlertType,AlertKey,FirstMessage,LastMessage,RuleID,GroupID,TriggerDate,ActiveSince)
SELECT AD.InstanceID,AD.Priority,@AlertType,AD.AlertKey,AD.Message,AD.Message,AD.RuleID,AD.GroupID,@Now,@Now
FROM @AD AD
OUTER APPLY Alert.IsBlackoutPeriod(AD.InstanceID,AD.AlertKey) BP
WHERE NOT EXISTS(SELECT 1 
			FROM Alert.ActiveAlerts AA
			WHERE AD.InstanceID = AA.InstanceID
			AND AD.AlertKey = AA.AlertKey
			AND AD.GroupID = AA.GroupID
			)
AND BP.IsBlackout=0