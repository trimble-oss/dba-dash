CREATE PROC Drives_Get(@InstanceID INT)
AS
SELECT DriveID,Name 
FROM dbo.Drives
WHERE InstanceID=@InstanceID
AND IsActive=1
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[Drives_Get] TO [Reports]
    AS [dbo];

