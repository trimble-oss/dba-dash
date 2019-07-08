CREATE PROC [dbo].[DriveReport_Get](@InstanceID INT=NULL,@FilterLevel TINYINT=4)
AS
SELECT * 
FROM dbo.DriveStatus
WHERE (InstanceID = @InstanceID OR @InstanceID IS NULL)
AND (Status<=@FilterLevel)
ORDER BY Status,PctFreeSpace
OPTION(RECOMPILE)



