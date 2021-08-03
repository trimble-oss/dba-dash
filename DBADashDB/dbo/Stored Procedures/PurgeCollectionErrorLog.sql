CREATE PROC dbo.PurgeCollectionErrorLog(
	@BatchSize INT=1000
)
AS

DECLARE @RetentionDays INT 

SELECT @RetentionDays = ISNULL((SELECT RetentionDays
								FROM dbo.DataRetention
								WHERE TableName = 'CollectionErrorLog'
								),14)
WHILE (1=1)
BEGIN
	DELETE TOP(@BatchSize)
	FROM dbo.CollectionErrorLog
	WHERE ErrorDate < DATEADD(d,-@RetentionDays,GETUTCDATE())
		IF @@ROWCOUNT=0
			BREAK
END
