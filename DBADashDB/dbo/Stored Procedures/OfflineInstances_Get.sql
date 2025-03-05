CREATE PROC dbo.OfflineInstances_Get(
	@InstanceIDs dbo.IDs READONLY
)
AS
SELECT	I.InstanceID,
		I.InstanceDisplayName,
		OI.FirstFail,
		OI.LastFail,
		HDLastFail.HumanDuration AS TimeSinceLastFail,
		LC.LastCollection,
		HD.HumanDuration AS Duration,
		HDLastCollection.HumanDuration AS TimeSinceLastCollection,
		OI.FailCount,
		EMF.Message AS FirstMessage,
		EML.Message AS LastMessage
FROM dbo.OfflineInstances OI
JOIN dbo.Instances I ON OI.InstanceID = I.InstanceID
LEFT JOIN dbo.ErrorMessage EMF ON EMF.ErrorMessageID = OI.FirstMessageID
LEFT JOIN dbo.ErrorMessage EML ON EML.ErrorMessageID = OI.LastMessageID
LEFT JOIN (	SELECT InstanceID,
					MAX(SnapshotDate) AS LastCollection
			FROM dbo.CollectionDates
			GROUP BY InstanceID) LC
	ON LC.InstanceID = OI.InstanceID	
OUTER APPLY dbo.SecondsToHumanDuration(DATEDIFF_BIG(s,OI.FirstFail,OI.LastFail)) AS HD
OUTER APPLY dbo.SecondsToHumanDuration(DATEDIFF_BIG(s,LC.LastCollection,GETUTCDATE())) AS HDLastCollection
OUTER APPLY dbo.SecondsToHumanDuration(DATEDIFF_BIG(s,OI.LastFail,GETUTCDATE())) AS HDLastFail
WHERE OI.IsCurrent=1
AND EXISTS(
			SELECT 1 
			FROM @InstanceIDs T 
			WHERE T.ID=I.InstanceID
			)