CREATE PROC dbo.Instance_SetActive(
	@InstanceID INT=NULL,
	@ConnectionID NVARCHAR(128)=NULL,
	@IsActive BIT
)
AS
/*
	Marks an instance deleted (@IsActive=0) or restores it - a soft delete only.  Used when a connection is removed or
	added from the GUI, so it's granted to ManageConnections, whose members might otherwise only have read-only app access.
	Instance_Del isn't granted as it can also hard delete.
*/
SET NOCOUNT ON
IF @InstanceID IS NULL
BEGIN
	SELECT @InstanceID = InstanceID
	FROM dbo.Instances
	WHERE ConnectionID = @ConnectionID
END
IF @InstanceID IS NULL
BEGIN
	RAISERROR('Instance not found',11,1)
	RETURN
END

UPDATE dbo.Instances
SET IsActive = @IsActive
WHERE InstanceID = @InstanceID
