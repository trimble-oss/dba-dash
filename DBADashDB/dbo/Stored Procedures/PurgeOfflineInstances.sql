CREATE PROC dbo.PurgeOfflineInstances(
	@BatchSize INT=5000
)
AS 
DECLARE @RetentionDays INT 

SELECT @RetentionDays = ISNULL((SELECT RetentionDays
								FROM dbo.DataRetention
								WHERE TableName = 'OfflineInstances'
								AND SchemaName = 'dbo'
								),730)
WHILE (1=1)
BEGIN
	DELETE TOP(@BatchSize) OI
	FROM dbo.OfflineInstances OI
	WHERE EXISTS(SELECT 1 
				FROM dbo.Instances I 
				WHERE I.InstanceID=OI.InstanceID 
				AND OI.FirstFail < DATEADD(DAY,-@RetentionDays,GETUTCDATE())
				AND OI.IsCurrent=0
				)
	
	IF @@ROWCOUNT=0
			BREAK
END
