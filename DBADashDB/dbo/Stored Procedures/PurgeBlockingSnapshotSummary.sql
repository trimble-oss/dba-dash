CREATE PROC dbo.PurgeBlockingSnapshotSummary(
	@BatchSize INT = 500000
)
AS
DECLARE @MaxDate DATETIME2

SELECT @MaxDate = DATEADD(d,-RetentionDays,GETUTCDATE())
FROM dbo.DataRetention
WHERE TableName = 'RunningQueries'

WHILE 1=1
BEGIN
	DELETE TOP(@BatchSize) 
	FROM dbo.BlockingSnapshotSummary
	WHERE SnapshotDateUTC < @MaxDate

	IF @@ROWCOUNT<@BatchSize
		BREAK
END