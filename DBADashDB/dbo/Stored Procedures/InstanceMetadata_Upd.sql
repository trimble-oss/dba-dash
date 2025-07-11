CREATE PROC dbo.InstanceMetadata_Upd(
	@InstanceID INT,
	@Provider VARCHAR(50),
	@Metadata NVARCHAR(MAX),
	@SnapshotDate DATETIME2 = NULL
)
AS
SET NOCOUNT ON;
DECLARE @Ref VARCHAR(30)='InstanceMetadata'
DECLARE @OldMetadata NVARCHAR(MAX);
DECLARE @LastSnapshotDate DATETIME2;

SELECT @LastSnapshotDate = SnapshotDate
FROM dbo.CollectionDates 
WHERE InstanceID = @InstanceID 
AND Reference=@Ref

IF @LastSnapshotDate >= @SnapshotDate
BEGIN
	RETURN
END

SELECT @OldMetadata = Metadata
FROM dbo.InstanceMetadata
WHERE InstanceID = @InstanceID

IF @OldMetadata IS NULL
BEGIN
	INSERT INTO dbo.InstanceMetadata (InstanceID, Provider, Metadata,SnapshotDate,PreviousVersionLastSnapshotDate)
	VALUES (@InstanceID, @Provider, @Metadata, @SnapshotDate,@LastSnapshotDate);
END

IF @OldMetadata <> @Metadata
BEGIN
	UPDATE dbo.InstanceMetadata
	SET Provider = @Provider,
		Metadata = @Metadata,
		SnapshotDate = @SnapshotDate,
		PreviousVersionLastSnapshotDate = @LastSnapshotDate
	WHERE InstanceID = @InstanceID;
END

EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										@Reference = @Ref,
										@SnapshotDate = @SnapshotDate
