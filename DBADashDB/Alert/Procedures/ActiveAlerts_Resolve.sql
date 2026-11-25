CREATE PROC Alert.ActiveAlerts_Resolve(
	@AlertIDs dbo.BigIDs READONLY,
	@AlertType VARCHAR(50),
	@UnclearedAreActive BIT = 0
)
AS
/*
	Resolves alerts of @AlertType whose issue has cleared, applying the rule's resolve delay (ResolveAfterMins, defaulting to NotifyAfterMins).
	@AlertIDs is every alert of the type whose issue has cleared on this run.

	The first time an alert's issue is seen to have cleared, ResolvingSince records it.  The alert is resolved once the issue has stayed
	clear for the delay.  If the issue comes back first, Alert.ActiveAlerts_Upd clears ResolvingSince when the alert is reported again
	and it carries on as the same episode: ActiveSince isn't reset, so an issue that keeps clearing briefly still passes the notify delay
	in Alert.Notifications_Get.
	A resolving alert that's in neither list - e.g. its custom SQL proc failed - keeps waiting.  It isn't treated as active again, unless
	@UnclearedAreActive=1: the caller checked every alert of the type, so one that hasn't cleared still has the issue - e.g. a failing job
	that isn't reported again because the failure isn't new.
	UpdatedDate is refreshed each time an alert is confirmed clear, so Alert.Alerts_Upd doesn't close it as stale during a long resolve delay.

	Also records when each alert of the type is first due for notification in its episode (NotifyReadyDate) - for desktop notifications.
	So it's called after the alerts are updated: by Alert.ActiveAlerts_Upd and by the alert procs that work out for themselves when their
	alerts are resolved.
*/
SET NOCOUNT ON
DECLARE @Now DATETIME2 = SYSUTCDATETIME();

UPDATE AA
	SET ResolvingSince = ISNULL(AA.ResolvingSince,@Now),
	ResolvedDate = CASE WHEN RD.IsResolved=1 THEN @Now ELSE AA.ResolvedDate END,
	IsResolved = RD.IsResolved,
	LastMessage = CASE WHEN RD.IsResolved=1 THEN 'Issue resolved' ELSE AA.LastMessage END,
	UpdateCount += RD.IsResolved,
	ResolvedCount += RD.IsResolved,
	UpdatedDate = @Now
FROM Alert.ActiveAlerts AA
LEFT JOIN Alert.Rules R ON AA.RuleID = R.RuleID
CROSS APPLY(SELECT COALESCE(R.ResolveAfterMins,R.NotifyAfterMins,0) AS ResolveAfterMins) D
CROSS APPLY(SELECT CASE WHEN D.ResolveAfterMins = 0
						OR AA.ResolvingSince <= DATEADD(mi,-D.ResolveAfterMins,@Now)
					THEN 1 ELSE 0 END AS IsResolved
			) RD
WHERE AA.AlertType = @AlertType
AND AA.IsResolved = 0
AND EXISTS(SELECT 1
			FROM @AlertIDs T
			WHERE T.ID = AA.AlertID
			)

IF @UnclearedAreActive = 1
BEGIN
	/* Resolving, but the issue is back - the same episode carries on */
	UPDATE AA
		SET ResolvingSince = NULL,
		UpdatedDate = @Now,
		UpdateCount += 1
	FROM Alert.ActiveAlerts AA
	WHERE AA.AlertType = @AlertType
	AND AA.IsResolved = 0
	AND AA.IsBlackout = 0
	AND AA.ResolvingSince IS NOT NULL
	AND NOT EXISTS(SELECT 1
				FROM @AlertIDs T
				WHERE T.ID = AA.AlertID
				)
END

/* First due for notification in this episode */
UPDATE AA
	SET NotifyReadyDate = @Now
FROM Alert.ActiveAlerts AA
CROSS APPLY Alert.AlertDelayStatus_Get(AA.IsResolved,ISNULL(AA.ActiveSince,AA.TriggerDate),AA.ResolvingSince,AA.RuleID) DS
WHERE AA.AlertType = @AlertType
AND AA.IsResolved = 0
AND AA.IsBlackout = 0
AND DS.IsHeld = 0
AND (AA.NotifyReadyDate IS NULL OR AA.NotifyReadyDate < ISNULL(AA.ActiveSince,AA.TriggerDate))
