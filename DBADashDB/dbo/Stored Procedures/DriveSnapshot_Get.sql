CREATE PROC dbo.DriveSnapshot_Get(
	@DriveID INT,
	@FromDate DATETIME2(2),
	@ToDate DATETIME2(2),
	@DateGroupingMins INT=1440
)
AS
SELECT	DG.DateGroup AS SnapshotDate,
		MAX(DSS.Capacity)/POWER(1024.0,3) AS SizeGB,
		MAX(DSS.UsedSpace)/POWER(1024.0,3) AS UsedGB,
		COUNT(*) AS Samples
FROM dbo.DriveSnapshot DSS
CROSS APPLY dbo.DateGroupingMins(DSS.SnapshotDate,@DateGroupingMins) DG
WHERE DSS.DriveID=@DriveID
AND DSS.SnapshotDate>= @FromDate
AND DSS.SnapshotDate<@ToDate
GROUP BY DG.DateGroup
ORDER BY SnapshotDate