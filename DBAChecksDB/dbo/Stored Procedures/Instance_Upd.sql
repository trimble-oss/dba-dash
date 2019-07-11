CREATE PROC [dbo].[Instance_Upd](@ConnectionID SYSNAME,@Instance SYSNAME,@SnapshotDateUTC DATETIME,@InstanceID INT OUT)
AS
SELECT @InstanceID = InstanceID
FROM dbo.Instances 
WHERE ConnectionID = @ConnectionID

IF @InstanceID IS NULL
BEGIN
	BEGIN TRAN
	INSERT INTO dbo.Instances(Instance,ConnectionID,IsActive)
	VALUES(@Instance,@ConnectionID,CAST(1 as BIT))
	SELECT @InstanceID = SCOPE_IDENTITY();
	INSERT INTO dbo.SnapshotDates(
	    InstanceID,
		InstanceDate
	)
	VALUES
	(@InstanceID, @SnapshotDateUTC
	    )
	COMMIT

END
ELSE
BEGIN
	UPDATE dbo.Instances 
	SET Instance = @Instance,
	SnapshotDate=@SnapshotDateUTC
	WHERE InstanceID = @InstanceID

	UPDATE dbo.SnapshotDates 
	SET	InstanceDate=@SnapshotDateUTC
	WHERE InstanceID = @InstanceID
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
	INSERT INTO dbo.IntanceTag(InstanceID,TagID)
	VALUES(@InstanceID,-1)
END