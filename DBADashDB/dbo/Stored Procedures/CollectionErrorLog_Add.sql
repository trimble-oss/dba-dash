CREATE PROC [dbo].[CollectionErrorLog_Add](
	@Errors dbo.CollectionError READONLY,
	@InstanceID INT,
	@ErrorDate DATETIME2(2) 
)
AS
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionErrorLog
			WHERE InstanceID=@InstanceID 
			AND ErrorDate = @ErrorDate
			)
BEGIN
INSERT INTO dbo.CollectionErrorLog
(
    ErrorDate,
    InstanceID,
    ErrorSource,
    ErrorMessage,
	ErrorContext
)
SELECT @ErrorDate,@InstanceID, ErrorSource,ErrorMessage ,ErrorContext
FROM @Errors
END