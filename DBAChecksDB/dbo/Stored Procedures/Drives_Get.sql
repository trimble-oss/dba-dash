CREATE PROC [dbo].[Drives_Get](@InstanceID INT)
AS
SELECT DriveID,Name,InstanceID,Label,TotalGB,FreeGB,DriveCheckType,Status,D.DriveWarningThreshold,D.DriveCriticalThreshold,D.IsInheritedThreshold
FROM dbo.DriveStatus D
WHERE InstanceID = @InstanceID
ORDER BY Status DESC, PctFreeSpace DESC
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[Drives_Get] TO [Reports]
    AS [dbo];

