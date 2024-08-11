CREATE PROC dbo.MaxCollectionDateByInstance_Get
AS
SELECT	I.ConnectionID,
		MAX(CD.SnapshotDate) AS MaxSnapshotDate
FROM dbo.Instances I 
JOIN dbo.CollectionDates CD ON I.InstanceID = CD.InstanceID
GROUP BY I.ConnectionID