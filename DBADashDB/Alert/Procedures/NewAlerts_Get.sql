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
		CASE WHEN AA.Escalated > AA.TriggerDate THEN AA.Escalated ELSE AA.TriggerDate END AS AlertDate
FROM Alert.ActiveAlerts AA 
JOIN dbo.Instances I ON AA.InstanceID = I.InstanceID
WHERE (AA.TriggerDate > @FromDate 
		OR AA.Escalated > @FromDate 
		)
AND AA.IsBlackout=0
AND AA.IsResolved=0
AND AA.IsAcknowledged=0