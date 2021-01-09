CREATE PROC [dbo].[BlockingSnapshots_Get](
	@InstanceID INT,
	@FromDate DATETIME2(3),
	@ToDate DATETIME2(3),
	@Top INT=500,
	@DatabaseID INT=NULL
)
AS
DECLARE @SQL NVARCHAR(MAX) 
SET @SQL = N'
SELECT TOP(@Top) BlockingSnapshotID,SnapshotDateUTC,BlockedSessionCount,BlockedWaitTime 
FROM dbo.BlockingSnapshotSummary SS
WHERE InstanceID = @InstanceID
AND SnapshotDateUTC>=@FromDate
AND SnapshotDateUTC<@ToDate
' + CASE WHEN @DatabaseID IS NULL THEN '' ELSE 'AND EXISTS(SELECT 1 
			FROM dbo.BlockingSnapshot BSS
			JOIN dbo.Databases D ON D.database_id = BSS.database_id
			WHERE BSS.BlockingSnapshotID = SS.BlockingSnapshotID
			AND D.DatabaseID = @DatabaseID
			)'	END + '
ORDER BY BlockedWaitTime DESC'

EXEC sp_executesql @SQL,N'@InstanceID INT,@DatabaseID INT,@FromDate DATETIME,@ToDate DATETIME,@Top INT',@InstanceID,@DatabaseID,@FromDate,@ToDate,@Top