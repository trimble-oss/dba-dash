DECLARE @ToDate DATETIME2(2)='20201001 13:00' 
DECLARE @FromDate DATETIME2(2)


DECLARE @InstanceID INT
DECLARE @EditionID INT
DECLARE C1 CURSOR FOR 
	SELECT instanceID,i.EditionID 
	FROM dbo.Instances i

OPEN c1
WHILE 1=1
BEGIN
	FETCH NEXT FROM c1 INTO @InstanceID,@EditionID
	IF @@FETCH_STATUS<>0
		BREAK

	SELECT @FromDate = ISNULL(DATEADD(hh,1,MAX(SnapshotDate)),'19000101')
	FROM dbo.DBIOStats
	WHERE InstanceID = @InstanceID
	AND DatabaseID =-1
	AND Drive='*'
	AND FileID=-1

IF @EditionID=1674378470
BEGIN
	INSERT INTO DBIOStats
	SELECT IOS.InstanceID,		
			ISNULL(x.DatabaseID,-1) AS DatabaseID,
			ISNULL(x.Drive,'*') AS Drive,
			ISNULL(x.FileID,-1) AS FileID,
			IOS.SnapshotDate,
			SUM(IOS.num_of_reads) AS num_of_reads,
			SUM(IOS.num_of_writes) AS num_of_writes,
			SUM(IOS.num_of_bytes_read) num_of_bytes_read,
			SUM(IOS.num_of_bytes_written) num_of_bytes_written,
			SUM(IOS.io_stall_read_ms) io_stall_read_ms,
			SUM(IOS.io_stall_write_ms) io_stall_write_ms,
			MAX(IOS.sample_ms_diff) sample_ms_diff,
			SUM(IOS.size_on_disk_bytes)
	FROM dbo.IOStats IOS 
	JOIN dbo.DBFiles F ON IOS.FileID = F.FileID
	OUTER APPLY(SELECT ISNULL(F.DatabaseID,-999) AS DatabaseID,
						ISNULL(F.FileID,-999) AS FileID,
						'?' AS Drive
						) x
	WHERE IOS.InstanceID = @InstanceID
	AND IOS.SnapshotDate>@FromDate
	AND IOS.SnapshotDate< @ToDate
	GROUP BY GROUPING SETS(
				(IOS.InstanceID),
				(x.DatabaseID,x.FileID,x.Drive)
				)
	,IOS.InstanceID,IOS.SnapshotDate
	OPTION(RECOMPILE)
END
ELSE
BEGIN
	INSERT INTO DBIOStats
	SELECT IOS.InstanceID,		
			ISNULL(x.DatabaseID,-1) AS DatabaseID,
			ISNULL(x.Drive,'*') AS Drive,
			ISNULL(x.FileID,-1) AS FileID,
			IOS.SnapshotDate,
			SUM(IOS.num_of_reads) AS num_of_reads,
			SUM(IOS.num_of_writes) AS num_of_writes,
			SUM(IOS.num_of_bytes_read) num_of_bytes_read,
			SUM(IOS.num_of_bytes_written) num_of_bytes_written,
			SUM(IOS.io_stall_read_ms) io_stall_read_ms,
			SUM(IOS.io_stall_write_ms) io_stall_write_ms,
			MAX(IOS.sample_ms_diff) sample_ms_diff,
			SUM(IOS.size_on_disk_bytes)
	FROM dbo.IOStats IOS 
	LEFT JOIN dbo.DBFiles F ON IOS.FileID = F.FileID
	OUTER APPLY(SELECT ISNULL(F.DatabaseID,-999) AS DatabaseID,
						ISNULL(F.FileID,-999) AS FileID,
						CASE WHEN F.physical_name LIKE '_:\%' THEN LEFT(F.physical_name,1) ELSE '?' END AS Drive
						) x
	WHERE IOS.InstanceID = @InstanceID
	AND IOS.SnapshotDate>@FromDate
	AND IOS.SnapshotDate< @ToDate
	GROUP BY GROUPING SETS(
				(IOS.InstanceID),
				(x.DatabaseID),
				(x.DatabaseID,x.Drive),
				(x.DatabaseID,x.FileID,x.Drive),
				(x.Drive)
				)
	,IOS.InstanceID,IOS.SnapshotDate
	OPTION(RECOMPILE)
END

PRINT @InstanceID

END
CLOSE c1 
DEALLOCATE c1