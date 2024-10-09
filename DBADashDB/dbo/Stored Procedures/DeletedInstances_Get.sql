CREATE PROC dbo.DeletedInstances_Get
AS
DECLARE @HardDeleteThresholdDays INT

SELECT @HardDeleteThresholdDays = TRY_CAST(SettingValue AS INT)
FROM dbo.Settings
WHERE SettingName = 'HardDeleteThresholdDays'

SELECT	I.InstanceID,
		I.ConnectionID,
		I.InstanceDisplayName,
		LastCollection.SnapshotDate,
		HD.HumanDuration AS LastCollection,
		DATEDIFF(mi,LastCollection.SnapshotDate,GETUTCDATE()) AS LastCollectionMins,
		DATEADD(d,@HardDeleteThresholdDays,LastCollection.SnapshotDate) AS ScheduledDeletion,
		DATEDIFF(d,GETUTCDATE(),DATEADD(d,@HardDeleteThresholdDays,LastCollection.SnapshotDate)) AS ScheduledDeletionDays,
		@HardDeleteThresholdDays AS HardDeleteThresholdDays
FROM dbo.Instances I
OUTER APPLY(
			SELECT TOP(1) SnapshotDate 
			FROM dbo.CollectionDates CD 
			WHERE CD.InstanceID = I.InstanceID 
			ORDER BY SnapshotDate DESC
			) LastCollection
CROSS APPLY dbo.SecondsToHumanDuration(DATEDIFF(s,LastCollection.SnapshotDate,GETUTCDATE())) HD
WHERE IsActive=0
