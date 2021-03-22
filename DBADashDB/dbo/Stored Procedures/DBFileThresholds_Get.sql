CREATE PROC [dbo].[DBFileThresholds_Get](
	@InstanceID INT,
	@DatabaseID INT,
	@DataSpaceID INT
)
AS
SELECT InstanceID,
       DatabaseID,
       data_space_id,
       FreeSpaceWarningThreshold,
       FreeSpaceCriticalThreshold,
       FreeSpaceCheckType,
	   PctMaxSizeCriticalThreshold,
	   PctMaxSizeWarningThreshold
FROM dbo.DBFileThresholds
WHERE InstanceID=@InstanceID 
AND DatabaseID = @DatabaseID
AND data_space_id=@DataSpaceID