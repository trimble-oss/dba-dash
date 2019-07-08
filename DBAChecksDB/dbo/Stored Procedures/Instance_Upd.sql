CREATE PROC [dbo].[Instance_Upd](@ConnectionID SYSNAME,@Instance SYSNAME,@SnapshotDateUTC DATETIME,@InstanceID INT OUT)
AS
SELECT @InstanceID = InstanceID
FROM dbo.Instances 
WHERE ConnectionID = @ConnectionID

IF @InstanceID IS NULL
BEGIN
	INSERT INTO dbo.Instances(Instance,ConnectionID,IsActive,SnapshotDate)
	VALUES(@Instance,@ConnectionID,CAST(1 as BIT),@SnapshotDateUTC)
	SELECT @InstanceID = SCOPE_IDENTITY();

END
ELSE
BEGIN
	UPDATE dbo.Instances 
	SET Instance = @Instance,
	SnapshotDate=@SnapshotDateUTC
	WHERE InstanceID = @InstanceID
END