CREATE PROC dbo.BlockingSnapshots_Get(
	@InstanceID INT,
	@FromDate DATETIME2(3),
	@ToDate DATETIME2(3),
	@Top INT=500,
	@DatabaseID INT=NULL
)
AS
-- DatabaseID filter isn't required for AzureDB
IF EXISTS(
	SELECT 1
	FROM dbo.InstanceInfo
	WHERE InstanceID=@InstanceID 
	AND EditionID=1674378470
)
BEGIN
	SET @DatabaseID=NULL
END

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
' + CASE WHEN @DatabaseID IS NULL THEN '' 
	ELSE 'AND EXISTS(SELECT 1 
					FROM dbo.BlockingSnapshot BSS
					JOIN dbo.Databases D ON D.database_id = BSS.database_id
					WHERE BSS.BlockingSnapshotID = SS.BlockingSnapshotID
					AND D.DatabaseID = @DatabaseID
					UNION All
					SELECT 1
					FROM dbo.RunningQueries Q
					JOIN dbo.Databases D ON Q.InstanceID = D.InstanceID AND D.database_id = Q.database_id
					WHERE Q.InstanceID = @InstanceID
					AND Q.SnapshotDateUTC = SS.SnapshotDateUTC
					AND D.DatabaseID = @DatabaseID
					AND Q.blocking_session_id <> 0
					)'	END + '
ORDER BY SS.BlockedWaitTime DESC'

EXEC sp_executesql @SQL,N'@InstanceID INT,@DatabaseID INT,@FromDate DATETIME,@ToDate DATETIME,@Top INT',@InstanceID,@DatabaseID,@FromDate,@ToDate,@Top