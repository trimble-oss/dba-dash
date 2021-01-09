CREATE PROC [dbo].[TraceFlags_Upd](@InstanceID INT,@SnapshotDate DATETIME2(2),@TraceFlags dbo.TraceFlags READONLY)
AS
SET XACT_ABORT ON
DECLARE @Ref VARCHAR(30)='TraceFlags'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN
	BEGIN TRAN
	DELETE tf 
	OUTPUT Deleted.InstanceID,DELETED.TraceFlag,DELETED.ValidFrom,@SnapshotDate INTO dbo.TraceFlagHistory(InstanceID,TraceFlag,ValidFrom,ValidTo)
	FROM dbo.TraceFlags tf
	WHERE tf.InstanceID=@InstanceID
	AND NOT EXISTS(SELECT 1 FROM @TraceFlags t WHERE t.TraceFlag = tf.TraceFlag AND t.Global=1)

	DECLARE @ValidFrom DATETIME2(2)
	IF EXISTS(SELECT * FROM dbo.CollectionDates
				WHERE InstanceID=@InstanceID
				AND Reference=@Ref)
	BEGIN
		SET @ValidFrom=@SnapshotDate
	END
	ELSE
	BEGIN
		SET @ValidFrom='19000101'
	END
	   
	INSERT INTO dbo.TraceFlags
	(
		InstanceID,
		TraceFlag,
		ValidFrom
	)
	SELECT @InstanceID,
		TraceFlag,
		@ValidFrom
	FROM @TraceFlags t
	WHERE Global=1
	AND NOT EXISTS(
				SELECT 1
				FROM dbo.TraceFlags tf
				WHERE tf.InstanceID= @InstanceID
				AND tf.TraceFlag = t.TraceFlag)

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
	                             @Reference = @Ref,
	                             @SnapshotDate = @SnapshotDate
	COMMIT
END