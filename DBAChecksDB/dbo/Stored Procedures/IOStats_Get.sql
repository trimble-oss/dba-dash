CREATE PROC [dbo].[IOStats_Get](
	@InstanceID INT,
	@FromDate DATETIME2(2)=NULL, 
	@ToDate DATETIME2(2)=NULL,
	@DatabaseID INT=NULL,
	@Drive CHAR(1)=NULL,
	@DateGroupingMin INT=NULL,
	@FileGroup SYSNAME=NULL
)
AS
IF @FromDate IS NULL
	SET @FromDate = DATEADD(mi,-60,GETUTCDATE())
IF @ToDate IS NULL
	SET @ToDate = GETUTCDATE()
DECLARE @SQL NVARCHAR(MAX)
DECLARE @DateGroupingSQL NVARCHAR(MAX)

SELECT @DatabaseID=ISNULL(@DatabaseID,-1),@Drive=ISNULL(@Drive,'*')

IF EXISTS(SELECT 1 
		FROM dbo.Databases d 
		JOIN dbo.Instances I ON d.InstanceID = I.InstanceID 
		WHERE I.EditionID=1674378470 --azure
		AND d.DatabaseID = @DatabaseID
		)
BEGIN
	SET @DatabaseID=-1
END
IF @DatabaseID=-1 AND @FileGroup IS NOT NULL
BEGIN
	RAISERROR('Database must be specifed when filtering on filegroup',11,1)
END
IF @FileGroup IS NOT NULL AND @Drive <>'*'
BEGIN
	RAISERROR('Can''t filter on drive when filtering on filegroup',11,1)
END

SELECT @DateGroupingSQL= CASE WHEN @DateGroupingMin IS NULL OR @DateGroupingMin =0 THEN 'IOS.SnapshotDate'
			ELSE 'DG.DateGroup' END

DECLARE @TableName SYSNAME
SELECT @TableName = CASE WHEN @FileGroup IS NULL THEN 'dbo.DBIOStats' ELSE 'dbo.FGIOStats' END + CASE WHEN @DateGroupingMin>=60 THEN '_60MIN' ELSE '' END

SET @SQL = N'
SELECT	' + @DateGroupingSQL + ' as SnapshotDate,
			SUM(IOS.num_of_reads+IOS.num_of_writes)/(SUM(IOS.sample_ms_diff)/1000.0) AS IOPs,
			SUM(IOS.num_of_reads)/(SUM(IOS.sample_ms_diff)/1000.0) AS ReadIOPs,
			SUM(IOS.num_of_writes)/(SUM(IOS.sample_ms_diff)/1000.0) AS WriteIOPs,
			SUM(IOS.num_of_bytes_read+IOS.num_of_bytes_written)/POWER(1024.0,2)/(SUM(IOS.sample_ms_diff)/1000.0) MBsec,
			SUM(IOS.num_of_bytes_read)/POWER(1024.0,2)/(SUM(IOS.sample_ms_diff)/1000.0) ReadMBsec,
			SUM(IOS.num_of_bytes_written)/POWER(1024.0,2)/(SUM(IOS.sample_ms_diff)/1000.0) WriteMBsec,
			ISNULL(SUM(IOS.io_stall_read_ms)/(NULLIF(SUM(IOS.num_of_reads),0)*1.0),0) AS ReadLatency,
			ISNULL(SUM(IOS.io_stall_write_ms)/(NULLIF(SUM(IOS.num_of_writes),0)*1.0),0) AS WriteLatency,
			ISNULL(SUM(IOS.io_stall_read_ms+IOS.io_stall_write_ms)/(NULLIF(SUM(IOS.num_of_writes+IOS.num_of_reads),0)*1.0),0) AS Latency,
			MAX(MaxIOPs) AS MaxIOPs,
			MAX(MaxReadIOPs)  AS MaxReadIOPs,
			MAX(MaxWriteIOPs)  AS MaxWriteIOPs,
			MAX(MaxMBsec) MaxMBsec,
			MAX(MaxReadMBsec) MaxReadMBsec,
			MAX(MaxWriteMBsec) MaxWriteMBsec,
			MAX(MaxReadLatency) AS MaxReadLatency,
			MAX(MaxWriteLatency) AS MaxWriteLatency,
			MAX(MaxLatency) AS MaxLatency
	FROM ' + @TableName + ' AS IOS
	' + CASE WHEN @DateGroupingMin IS NULL OR @DateGroupingMin =0 THEN '' ELSE 'CROSS APPLY dbo.DateGroupingMins(IOS.SnapshotDate,@DateGroupingMin) DG' END + '
	WHERE IOS.InstanceID = @InstanceID
	AND IOS.DatabaseID = @DatabaseID
	AND IOS.SnapshotDate >= @FromDate
	AND IOS.SnapshotDate < @ToDate
	' + CASE WHEN @FileGroup IS NULL THEN 'AND IOS.Drive = @Drive
	AND IOS.FileID = -1'
	ELSE 'AND IOS.filegroup_name = @FileGroup' END + '
	GROUP BY ' + @DateGroupingSQL  + '
ORDER BY SnapshotDate'

PRINT @SQL
EXEC sp_executesql @SQL,N'@InstanceID INT,@FromDate DATETIME2(2),@ToDate DATETIME2(2),@DatabaseID INT,@Drive CHAR(3),@DateGroupingMin INT,@FileGroup SYSNAME',@InstanceID,@FromDate,@ToDate,@DatabaseID,@Drive,@DateGroupingMin,@FileGroup