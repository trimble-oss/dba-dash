CREATE PROC dbo.DataRetention_Upd(
	@SchemaName SYSNAME='dbo',
	@TableName SYSNAME,
	@RetentionDays INT,
	@Validate BIT=1
)
AS
UPDATE dbo.DataRetention
	SET RetentionDays = @RetentionDays
WHERE TableName =@TableName
AND SchemaName = @SchemaName

IF @@ROWCOUNT=0
BEGIN
	IF @Validate = 0 
		OR EXISTS(
				SELECT 1 
				FROM dbo.PartitionHelper PH
				WHERE PH.TableName = @TableName
				AND PH.SchemaName = @SchemaName
			)
	BEGIN
		INSERT INTO dbo.DataRetention
		(
			SchemaName,
			TableName,
			RetentionDays
		)
		VALUES
		(@SchemaName,@TableName,@RetentionDays)
	END
	ELSE
	BEGIN
		RAISERROR('Invalid table',11,1)
	END
END