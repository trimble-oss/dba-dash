CREATE PROC dbo.DataRetention_Upd(@TableName SYSNAME,@RetentionDays INT)
AS
UPDATE dbo.DataRetention
SET RetentionDays = @RetentionDays
WHERE TableName =@TableName
IF @@ROWCOUNT=0
BEGIN
	IF EXISTS(SELECT * FROM dbo.PartitionFunctionName(@TableName))
	BEGIN
		INSERT INTO dbo.DataRetention
		(
			TableName,
			RetentionDays
		)
		VALUES
		(@TableName,@RetentionDays)
	END
	ELSE
	BEGIN
		RAISERROR('Invalid table',11,1)
	END
END