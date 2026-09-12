CREATE PROC dbo.PurgeQueryText(
	@BatchSize INT=1000
)
AS
CREATE TABLE #oldHandles(
	ID INT IDENTITY(1,1) PRIMARY KEY,
	sql_handle VARBINARY(64)
)
DECLARE @RetentionDays INT
SELECT @RetentionDays = RetentionDays 
FROM dbo.DataRetention
WHERE TableName = 'RunningQueries'

INSERT INTO #oldHandles
SELECT sql_handle 
FROM dbo.QueryText QT
WHERE SnapshotDate< DATEADD(d,-@RetentionDays,GETUTCDATE()) 
AND NOT EXISTS(SELECT 1 
				FROM dbo.RunningQueries Q 
				WHERE QT.sql_handle = Q.sql_handle
				)
AND NOT EXISTS(SELECT 1
				FROM dbo.RunningQueriesCursors RQC
				WHERE QT.sql_handle = RQC.sql_handle
				)
/*	The query stats collection shares this table, and its statements outlive the snapshots that reference
	them - a statement keeps its row so that it keeps its identity and its history if it runs again.  Text
	deleted while a statement still points at it would leave that statement unreadable in the report
	with nothing to re-collect it from.  dbo.PurgeQueryStatements runs first and removes the statements
	that have aged out, which is what releases their text to be purged here. */
AND NOT EXISTS(SELECT 1
				FROM dbo.QueryStatements QS
				WHERE QT.sql_handle = QS.sql_handle
				)

DECLARE @From INT =1
DECLARE @To INT

WHILE 1=1
BEGIN
	SET @To = @From+ @BatchSize
	DELETE QT 
	FROM dbo.QueryText QT
	WHERE  EXISTS(SELECT 1 
				FROM #oldHandles H
				WHERE QT.sql_handle = H.sql_handle
				AND ID >= @From
				AND ID < @To
				)
	IF @@ROWCOUNT=0
		BREAK
	SET @From+=@BatchSize
END
