CREATE VIEW dbo.CollectionDatesStatus
AS
SELECT CD.InstanceID,
	  CD.Reference, 
	  CASE WHEN DATEDIFF(mi,CD.SnapshotDate,GETUTCDATE())  > T.CriticalThreshold THEN  1 
			WHEN DATEDIFF(mi,CD.SnapshotDate,GETUTCDATE()) > T.WarningThreshold THEN 2
			WHEN T.WarningThreshold IS NULL AND T.CriticalThreshold IS NULL THEN 3
			ELSE 4 END AS Status,
	T.WarningThreshold,
	T.CriticalThreshold,
	DATEDIFF(mi,CD.SnapshotDate,GETUTCDATE()) AS SnapshotAge,
	HD.HumanDuration AS HumanSnapshotAge,
	CD.SnapshotDate,
	CASE WHEN T.InstanceID >0 THEN 'Instance' ELSE 'Root' END AS ConfiguredLevel
FROM dbo.CollectionDates CD 
CROSS APPLY dbo.SecondsToHumanDuration(DATEDIFF(s,CD.SnapshotDate,GETUTCDATE())) HD
OUTER APPLY(SELECT TOP(1) CDT.WarningThreshold,
				CDT.CriticalThreshold,
				CDT.InstanceID
		FROM dbo.CollectionDatesThresholds CDT 
		WHERE (CD.InstanceID = CDT.InstanceID OR CDT.InstanceID=-1)
		AND CDT.Reference = CD.Reference
		ORDER BY InstanceID DESC) T