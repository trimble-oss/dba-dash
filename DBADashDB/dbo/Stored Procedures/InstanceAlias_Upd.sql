CREATE PROCEDURE dbo.InstanceAlias_Upd(
	@InstanceID INT,
	@Alias NVARCHAR(128),
	@InstanceDisplayName NVARCHAR(128) OUT
)
AS
UPDATE dbo.Instances 
SET Alias = NULLIF(NULLIF(@Alias,''),ConnectionID)
WHERE InstanceID = @InstanceID

SELECT @InstanceDisplayName = InstanceDisplayName
FROM dbo.Instances
WHERE InstanceID = @InstanceID