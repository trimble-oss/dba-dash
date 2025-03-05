CREATE PROC dbo.OfflineInstanceTimeline_Get(
	@FromDate DATETIME2,
	@ToDate DATETIME2,
	@InstanceIDs dbo.IDs READONLY
)
AS
SELECT OI.InstanceID,
		I.InstanceDisplayName,
		OI.FirstFail,
		OI.LastFail,
		OI.ClosedDate,
		CONCAT('',EMF.Message,', ' + EML.Message) AS Message
FROM dbo.OfflineInstances OI 
JOIN dbo.Instances I ON OI.InstanceID = I.InstanceID
LEFT JOIN dbo.ErrorMessage EMF ON EMF.ErrorMessageID = OI.FirstMessageID
LEFT JOIN dbo.ErrorMessage EML ON EML.ErrorMessageID = OI.LastMessageID AND OI.FirstMessageID <> OI.LastMessageID
WHERE OI.FirstFail < @ToDate
AND OI.LastFail >= @FromDate
AND EXISTS(
			SELECT 1 
			FROM @InstanceIDs T 
			WHERE T.ID=I.InstanceID
			)