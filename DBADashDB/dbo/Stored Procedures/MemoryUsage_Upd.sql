CREATE PROC dbo.MemoryUsage_Upd(
	@MemoryUsage dbo.MemoryUsage READONLY,
	@InstanceID INT,
	@SnapshotDate DATETIME2(2)
)
AS 
SET XACT_ABORT ON
DECLARE @Ref VARCHAR(30)='MemoryUsage'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN
	IF EXISTS(SELECT type 
			  FROM @MemoryUsage T
			  EXCEPT
			  SELECT MemoryClerkType 
			  FROM dbo.MemoryClerkType
			  )
	BEGIN
		INSERT INTO dbo.MemoryClerkType(MemoryClerkType)
		SELECT type 
		FROM @MemoryUsage T
		EXCEPT
		SELECT MemoryClerkType 
		FROM dbo.MemoryClerkType
	END

	BEGIN TRAN
	INSERT INTO dbo.MemoryUsage(
		   InstanceID,
		   SnapshotDate,
		   MemoryClerkTypeID,
		   pages_kb,
		   virtual_memory_reserved_kb,
		   virtual_memory_committed_kb,
		   awe_allocated_kb,
		   shared_memory_reserved_kb,
		   shared_memory_committed_kb
	)
	SELECT @InstanceID,
		   @SnapshotDate,
		   MCT.MemoryClerkTypeID,
		   MU.pages_kb,
		   MU.virtual_memory_reserved_kb,
		   MU.virtual_memory_committed_kb,
		   MU.awe_allocated_kb,
		   MU.shared_memory_reserved_kb,
		   MU.shared_memory_committed_kb 
	FROM @MemoryUsage MU
	JOIN dbo.MemoryClerkType MCT ON MU.type = MCT.MemoryClerkType

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
	                             @Reference = @Ref,
	                             @SnapshotDate = @SnapshotDate

	COMMIT
END