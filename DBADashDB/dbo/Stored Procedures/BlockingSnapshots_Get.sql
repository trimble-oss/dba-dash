CREATE PROC dbo.BlockingSnapshots_Get(
	@InstanceID INT,
	@FromDate DATETIME2(3), /* UTC */
	@ToDate DATETIME2(3), /* UTC */
	@Top INT=500,
	@DatabaseID INT=NULL,
	@UTCOffset INT=0, /* Used for Hours filter */
	@DaysOfWeek IDs READONLY, /* e.g. 1=Monday. exclude weekends:  1,2,3,4,5.  Filter applied in local timezone (@UTCOffset) */
	@Hours IDs READONLY/* e.g. 9 to 5 :  9,10,11,12,13,14,15,16. Filter applied in local timezone (@UTCOffset)*/
)
AS
SET DATEFIRST 1 /* Start week on Monday */
SET NOCOUNT ON
-- DatabaseID filter isn't required for AzureDB
IF EXISTS(
	SELECT 1
	FROM dbo.InstanceInfo
	WHERE InstanceID=@InstanceID 
	AND EngineEdition=5
)
BEGIN
	SET @DatabaseID=NULL
END

/* Generate CSV list from list of integer values (safe from SQL injection compared to passing in a CSV string) */
DECLARE @DaysOfWeekCsv NVARCHAR(MAX)
SELECT @DaysOfWeekCsv =  STUFF((SELECT ',' + CAST(ID AS VARCHAR)
FROM @DaysOfWeek
FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,1,'')

/* Generate CSV list from list of integer values (safe from SQL injection compared to passing in a CSV string) */
DECLARE @HoursCsv NVARCHAR(MAX)
SELECT @HoursCsv =  STUFF((SELECT ',' + CAST(ID AS VARCHAR)
FROM @Hours
FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,1,'')

DECLARE @SQL NVARCHAR(MAX) 
SET @SQL = N'
SELECT TOP(@Top) SS.BlockingSnapshotID,
				SS.SnapshotDateUTC,
				SS.BlockedSessionCount,
				SS.BlockedWaitTime 
FROM dbo.BlockingSnapshotSummary SS
WHERE SS.InstanceID = @InstanceID
AND SS.SnapshotDateUTC>=@FromDate
AND SS.SnapshotDateUTC<@ToDate
' + CASE WHEN @DaysOfWeekCsv IS NULL THEN N'' ELSE 'AND DATEPART(dw,DATEADD(mi, @UTCOffset, SS.SnapshotDateUTC)) IN (' + @DaysOfWeekCsv + ')' END + '
' + CASE WHEN @HoursCsv IS NULL THEN N'' ELSE 'AND DATEPART(hh,DATEADD(mi, @UTCOffset, SS.SnapshotDateUTC)) IN(' + @HoursCsv + ')' END + '
' + CASE WHEN @DatabaseID IS NULL THEN '' 
	ELSE 'AND EXISTS(SELECT 1
					FROM dbo.RunningQueries Q
					JOIN dbo.Databases D ON Q.InstanceID = D.InstanceID AND D.database_id = Q.database_id
					WHERE Q.InstanceID = @InstanceID
					AND Q.SnapshotDateUTC = SS.SnapshotDateUTC
					AND D.DatabaseID = @DatabaseID
					AND Q.blocking_session_id <> 0
					)'	END + '
ORDER BY SS.BlockedWaitTime DESC'

EXEC sp_executesql @SQL,
					N'@InstanceID INT,
					@DatabaseID INT,
					@FromDate DATETIME2,
					@ToDate DATETIME2,
					@Top INT,
					@UTCOffset INT',
					@InstanceID,
					@DatabaseID,
					@FromDate,
					@ToDate,
					@Top,
					@UTCOffset