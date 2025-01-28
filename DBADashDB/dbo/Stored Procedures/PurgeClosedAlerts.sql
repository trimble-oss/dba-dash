CREATE PROC dbo.PurgeClosedAlerts(
	@BatchSize INT=5000,
	@KeepNotes BIT=1
)
AS

DECLARE @RetentionDays INT 

SELECT @RetentionDays = ISNULL((SELECT RetentionDays
								FROM dbo.DataRetention
								WHERE TableName = 'ClosedAlerts'
								AND SchemaName = 'Alert'
								),14)
WHILE (1=1)
BEGIN
	DELETE TOP(@BatchSize)
	FROM Alert.ClosedAlerts
	WHERE ClosedDate < DATEADD(d,-@RetentionDays,GETUTCDATE())
	AND (Notes IS NULL OR @KeepNotes=0) /* Keep alerts that have notes by default */
	
	IF @@ROWCOUNT=0
			BREAK
END
