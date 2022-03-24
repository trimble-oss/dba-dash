CREATE PROC dbo.DatabaseID_Get(
	@InstanceGroupName SYSNAME=NULL,
	@InstanceID INT=NULL,
	@DBName SYSNAME=NULL
)
AS
SELECT	D.DatabaseID,
		D.InstanceID 
FROM dbo.Instances I
JOIN dbo.Databases D ON D.InstanceID = I.InstanceID
WHERE (I.InstanceID = @InstanceID OR I.InstanceGroupName = @InstanceGroupName)
AND D.name = @DBName
AND D.IsActive=1
AND I.IsActive=1