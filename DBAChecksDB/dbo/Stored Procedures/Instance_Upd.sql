CREATE PROC [dbo].[Instance_Upd](
	@ConnectionID SYSNAME,
	@Instance SYSNAME,
	@SnapshotDate DATETIME2(2),
	@AgentHostName NVARCHAR(16),
	@AgentVersion VARCHAR(30)=NULL,
	@InstanceID INT OUT
)
AS
SELECT @InstanceID = InstanceID
FROM dbo.Instances 
WHERE ConnectionID = @ConnectionID

DECLARE @Ref VARCHAR(30)='Instance'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN
	IF @InstanceID IS NULL
	BEGIN
		BEGIN TRAN
		INSERT INTO dbo.Instances(Instance,ConnectionID,IsActive,AgentHostName,AgentVersion)
		VALUES(@Instance,@ConnectionID,CAST(1 as BIT),@AgentHostName,@AgentVersion)
		SELECT @InstanceID = SCOPE_IDENTITY();

		EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										 @Reference = @Ref,
										 @SnapshotDate = @SnapshotDate
		COMMIT

	END
	ELSE
	BEGIN
		UPDATE dbo.Instances 
		SET Instance = @Instance,
			AgentHostName=@AgentHostName,
			AgentVersion=@AgentVersion
		WHERE InstanceID = @InstanceID

		EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										 @Reference = @Ref,
										 @SnapshotDate = @SnapshotDate
	END
	IF NOT EXISTS(SELECT * FROM dbo.Tags WHERE TagID=-1)
	BEGIN
		SET IDENTITY_INSERT dbo.Tags ON
		INSERT INTO dbo.Tags(TagID,Tag)
		VALUES(-1,'{All Instances}')
		SET IDENTITY_INSERT dbo.Tags OFF
	END
	IF NOT EXISTS(SELECT 1 FROM dbo.InstanceTag WHERE InstanceID = @InstanceID AND TagID = -1)
	BEGIN
		INSERT INTO dbo.InstanceTag(InstanceID,TagID)
		VALUES(@InstanceID,-1)
	END
END