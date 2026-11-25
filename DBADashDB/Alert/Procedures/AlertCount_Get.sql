CREATE PROC Alert.AlertCount_Get
AS
/*
	Return summary info for active alerts for bell icon in GUI.
	Pending and resolving alerts are held back by their rule's delays, so they don't need attention yet.
*/
SELECT	COUNT(*) Total,
		ISNULL(SUM(CASE WHEN AA.IsBlackout=0 AND AA.IsAcknowledged=0 AND AA.IsResolved=0 AND DS.IsHeld=0 THEN 1 ELSE 0 END),0) AS ForAttention,
		ISNULL(MIN(CASE WHEN AA.IsBlackout=0 AND AA.IsAcknowledged=0 AND AA.IsResolved=0 AND DS.IsHeld=0 THEN AA.Priority ELSE NULL END),CAST(255 AS TINYINT)) AS Priority
FROM Alert.ActiveAlerts AA
CROSS APPLY Alert.AlertDelayStatus_Get(AA.IsResolved,ISNULL(AA.ActiveSince,AA.TriggerDate),AA.ResolvingSince,AA.RuleID) DS