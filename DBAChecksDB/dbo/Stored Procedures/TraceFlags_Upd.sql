CREATE PROC TraceFlags_Upd(@InstanceID INT,@SnapshotDate DATETIME2(2),@TraceFlags dbo.TraceFlags READONLY)
AS
SET XACT_ABORT ON
DECLARE @Ref VARCHAR(30)='TraceFlags'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN
	BEGIN TRAN
	DELETE TraceFlags 
	WHERE InstanceID=@InstanceID
	INSERT INTO dbo.TraceFlags
	(
		InstanceID,
		TraceFlag
	)
	SELECT @InstanceID,
		TraceFlag 
	FROM @TraceFlags
	WHERE Global=1
	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
	                             @Reference = @Ref,
	                             @SnapshotDate = @SnapshotDate
	COMMIT
END