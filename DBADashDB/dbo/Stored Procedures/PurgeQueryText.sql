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
