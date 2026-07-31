CREATE PROC Alert.CustomSqlAlertError_Log(
	@ErrorMessage NVARCHAR(MAX),
	@ThrottleMins INT = 60
)
AS
/*
	Logs an error from custom SQL alert processing (Alert.CustomSqlAlert_Upd) to dbo.CollectionErrorLog
	so it surfaces in the DBA Dash error log alongside collection errors.

	ErrorSource='Alert', ErrorContext='CustomSqlAlert', InstanceID is NULL (proc-level, not instance specific).

	Throttled: an identical message is not re-logged within @ThrottleMins so a persistently failing proc
	doesn't flood the error log every processing cycle.
*/
SET NOCOUNT ON

IF @ErrorMessage IS NULL
	RETURN;

IF NOT EXISTS(
	SELECT 1
	FROM dbo.CollectionErrorLog
	WHERE ErrorSource = 'Alert'
	AND ErrorContext = 'CustomSqlAlert'
	AND ErrorDate > DATEADD(mi, -@ThrottleMins, SYSUTCDATETIME())
	AND ErrorMessage = @ErrorMessage
)
BEGIN
	INSERT INTO dbo.CollectionErrorLog(ErrorDate, InstanceID, ErrorSource, ErrorMessage, ErrorContext)
	VALUES(SYSUTCDATETIME(), NULL, 'Alert', @ErrorMessage, 'CustomSqlAlert');
END
