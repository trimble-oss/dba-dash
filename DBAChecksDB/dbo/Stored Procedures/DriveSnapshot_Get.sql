CREATE PROC DriveSnapshot_Get(@DriveID INT,@FromDate DATETIME2(2),@ToDate DATETIME2(2))
AS
SELECT CAST(DSS.SnapshotDate AS DATE) SnapshotDate,MAX(DSS.Capacity)/POWER(1024.0,3) SizeGB,MAX(DSS.UsedSpace)/POWER(1024.0,3) AS UsedGB
FROM dbo.DriveSnapshot DSS
WHERE DSS.DriveID=@DriveID
AND DSS.SnapshotDate>= @FromDate
AND DSS.SnapshotDate<@ToDate
GROUP BY CAST(DSS.SnapshotDate AS DATE)
ORDER BY SnapshotDate