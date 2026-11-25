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
	With a notify delay, the alert is new once the issue has persisted for the delay rather than when it was first seen.
	If the delay passed while the alert was resolving, it's new when the issue came back.
*/
OUTER APPLY(SELECT DATEADD(mi,DS.NotifyAfterMins,ISNULL(AA.ActiveSince,AA.TriggerDate)) AS DelayPassed) DP
OUTER APPLY(SELECT CASE WHEN DS.NotifyAfterMins = 0 THEN AA.TriggerDate
					WHEN AA.ResumedDate > DP.DelayPassed THEN AA.ResumedDate
					ELSE DP.DelayPassed END AS NotifyDate
			) ND
WHERE (ND.NotifyDate > @FromDate
		OR AA.Escalated > @FromDate
		)
AND DS.IsHeld = 0 /* Not pending or resolving */
AND AA.IsBlackout=0
AND AA.IsResolved=0
AND AA.IsAcknowledged=0