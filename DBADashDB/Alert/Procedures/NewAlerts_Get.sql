CREATE PROC Alert.NewAlerts_Get(
	@FromDate DATETIME2
)
AS
/* 
	Get new alerts for desktop notifications in the GUI 
*/
SELECT	I.InstanceDisplayName,
		AA.TriggerDate,
		AA.AlertType,
		AA.Priority,
		AA.LastMessage,
		CASE WHEN AA.Escalated > ND.NotifyDate THEN AA.Escalated ELSE ND.NotifyDate END AS AlertDate
FROM Alert.ActiveAlerts AA 
JOIN dbo.Instances I ON AA.InstanceID = I.InstanceID
CROSS APPLY Alert.AlertDelayStatus_Get(AA.IsResolved,ISNULL(AA.ActiveSince,AA.TriggerDate),AA.ResolvingSince,AA.RuleID) DS
/*
	With a notify delay, the alert is new when its episode is first due for notification - once the issue has persisted for the delay,
	or when it came back if the delay passed while it was resolving.  Not each time it comes back.
*/
OUTER APPLY(SELECT CASE WHEN DS.NotifyAfterMins = 0 THEN AA.TriggerDate
					WHEN AA.NotifyReadyDate >= ISNULL(AA.ActiveSince,AA.TriggerDate) THEN AA.NotifyReadyDate
					END AS NotifyDate
			) ND
WHERE (ND.NotifyDate > @FromDate
		OR AA.Escalated > @FromDate
		)
AND DS.IsHeld = 0 /* Not pending or resolving */
AND ND.NotifyDate IS NOT NULL /* Due for notification - e.g. not escalated while pending, then past its delay before the next alert run marked it due */
AND AA.IsBlackout=0
AND AA.IsResolved=0
AND AA.IsAcknowledged=0