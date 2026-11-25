CREATE PROC Alert.ActiveAlerts_Resolve(
	@AlertIDs dbo.BigIDs READONLY,
	@AlertType VARCHAR(50)
)
AS
/*
	Resolves alerts of @AlertType whose issue has cleared, applying the rule's resolve delay (ResolveAfterMins, defaulting to NotifyAfterMins).
	@AlertIDs is every alert of the type whose issue has cleared on this run.

	The first time an alert's issue is seen to have cleared, ResolvingSince records it.  The alert is resolved once the issue has stayed
	clear for the delay.  If the issue comes back first, Alert.ActiveAlerts_Upd clears ResolvingSince when the alert is reported again
	and it carries on as the same episode: ActiveSince isn't reset, so an issue that keeps clearing briefly still passes the notify delay
	in Alert.Notifications_Get.
	A resolving alert that's in neither list - e.g. its custom SQL proc failed - keeps waiting.  It isn't treated as active again.

	Called by Alert.ActiveAlerts_Upd and by the alert procs that work out for themselves when their alerts are resolved.
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
	UpdatedDate = CASE WHEN RD.IsResolved=1 THEN @Now ELSE AA.UpdatedDate END
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
AND (RD.IsResolved = 1 OR AA.ResolvingSince IS NULL)
