CREATE PROC dbo.DailyReport_Alerts_Get(
	@InstanceIDs VARCHAR(MAX)
)
AS
/*
	Open alerts (Alert.ActiveAlerts not yet resolved) for the instances in a daily report, from Critical to Low priority - informational
	ones are left out.  An alert has a row per notification channel group, so it's returned once per instance and AlertKey: the row that
	most needs attention - not acknowledged or in a blackout if any is, then the highest priority.  IsAcknowledged and IsBlackout let the
	report count the alerts it doesn't list.
*/
SET NOCOUNT ON

;WITH Scope AS (
	SELECT TRY_CAST(value AS INT) AS InstanceID
	FROM STRING_SPLIT(@InstanceIDs,',')
),
Ranked AS (
	SELECT AA.InstanceID,
		AA.AlertType,
		AA.AlertKey,
		AA.Priority,
		AA.LastMessage,
		AA.IsAcknowledged,
		AA.IsBlackout,
		MIN(AA.TriggerDate) OVER(PARTITION BY AA.InstanceID, AA.AlertKey) AS TriggerDate,
		ROW_NUMBER() OVER(PARTITION BY AA.InstanceID, AA.AlertKey
						ORDER BY CASE WHEN AA.IsAcknowledged = 1 OR AA.IsBlackout = 1 THEN 1 ELSE 0 END, AA.Priority, AA.UpdatedDate DESC) AS rnum
	FROM Alert.ActiveAlerts AA
	JOIN Scope S ON S.InstanceID = AA.InstanceID
	WHERE AA.IsResolved = 0
	AND AA.Priority <= 30
)
SELECT I.InstanceDisplayName AS Instance,
	R.AlertType,
	R.AlertKey,
	R.Priority,
	LEFT(R.LastMessage, 500) AS Message,
	R.TriggerDate,
	R.IsAcknowledged,
	R.IsBlackout
FROM Ranked R
JOIN dbo.Instances I ON I.InstanceID = R.InstanceID
WHERE R.rnum = 1
ORDER BY R.Priority, R.TriggerDate
