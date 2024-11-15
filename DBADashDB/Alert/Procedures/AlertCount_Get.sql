CREATE PROC Alert.AlertCount_Get
AS
/*
	Return summary info for active alerts for bell icon in GUI
*/
SELECT	COUNT(*) Total,
		ISNULL(SUM(CASE WHEN IsBlackout=0 AND IsAcknowledged=0 AND IsResolved=0 THEN 1 ELSE 0 END),0) AS ForAttention,
		ISNULL(MIN(CASE WHEN IsBlackout=0 AND IsAcknowledged=0 AND IsResolved=0 THEN Priority ELSE NULL END),CAST(255 AS TINYINT)) AS Priority
FROM Alert.ActiveAlerts