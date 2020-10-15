CREATE PROC DatabaseID_Get(
	@Instance SYSNAME=NULL,
	@InstanceID INT=NULL,
	@DBName SYSNAME=NULL
)
AS
IF @InstanceID IS NULL
BEGIN
	SELECT @InstanceID = InstanceID 
	FROM dbo.Instances 
	WHERE Instance = @Instance
END
SELECT D.DatabaseID,D.InstanceID 
FROM dbo.Instances I
JOIN dbo.Databases D ON D.InstanceID = I.InstanceID
WHERE I.InstanceID = @InstanceID 
AND D.name = @DBName
AND D.IsActive=1