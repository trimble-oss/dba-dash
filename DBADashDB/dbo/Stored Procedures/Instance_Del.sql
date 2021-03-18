CREATE PROC dbo.Instance_Del(
	@InstanceID INT,
	@IsActive BIT=0
)
AS
UPDATE dbo.Instances
SET IsActive=@IsActive
WHERE InstanceID = @InstanceID