CREATE PROC DriveLetters_Get(@InstanceID INT)
AS
SELECT Name,Label
FROM Drives
WHERE InstanceID = @InstanceID
AND IsActive = 1;