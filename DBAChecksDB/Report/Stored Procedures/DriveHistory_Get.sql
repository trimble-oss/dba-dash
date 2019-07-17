CREATE PROC Report.DriveHistory_Get(@InstanceID INT,@DriveIDs VARCHAR(MAX)=NULL,@IsActive BIT=1,@HistoryDays INT=365)
AS
DECLARE @Drives TABLE(
	DriveID INT PRIMARY KEY
)
IF @DriveIDs IS NULL
BEGIN
	INSERT INTO @Drives
	(
	    DriveID
	)
	SELECT DriveID 
	FROM dbo.Drives
	WHERE (IsActive=@IsActive OR @IsActive IS NULL)
	AND InstanceID=@InstanceID
END 
ELSE 
BEGIN
	INSERT INTO @Drives
	(
		DriveID
	)
	SELECT Item
	FROM dbo.SplitStrings(@DriveIDs,',')
END;
WITH T AS (
	SELECT D.DriveID,D.Name,D.Label, DSS.SnapshotDate,SUM(DSS.Capacity)/POWER(1024.0,3) TotalGB,SUM(DSS.FreeSpace)/POWER(1024.0,3) FreeGB,
		SUM(DSS.Capacity-DSS.FreeSpace)/POWER(1024.0,3) UsedGB,
		SUM(DSS.FreeSpace)/SUM(DSS.Capacity*1.0) AS PctFree,ROW_NUMBER() OVER(PARTITION BY D.DriveID,CAST(DSS.SnapshotDate AS DATE) ORDER BY DSS.SnapshotDate) rnum
	FROM dbo.DriveSnapshot DSS
	JOIN dbo.Drives D ON D.DriveID = DSS.DriveID
	WHERE D.InstanceID = @InstanceID
	AND EXISTS(SELECT 1 FROM @Drives t WHERE t.DriveID = D.DriveID)
	AND (D.IsActive=@IsActive OR @IsActive IS NULL)
	AND DSS.SnapshotDate >= DATEADD(d,-@HistoryDays,GETUTCDATE())
	GROUP BY DSS.SnapshotDate,D.DriveID,D.Name,D.Label
)
SELECT T.DriveID,
       T.Name,
       T.Label,
       T.SnapshotDate,
       T.TotalGB,
       T.FreeGB,
       T.UsedGB,
       T.PctFree
FROM T 
WHERE (rnum=1 OR @HistoryDays<=60)
ORDER BY T.SnapshotDate