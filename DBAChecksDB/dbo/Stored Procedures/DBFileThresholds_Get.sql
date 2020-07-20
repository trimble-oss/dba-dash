CREATE PROC [dbo].[DBFileThresholds_Get](
	@InstanceID INT,
	@DatabaseID INT,
	@DataSpaceID INT
)
AS
SET NOCOUNT ON
SET XACT_ABORT ON
BEGIN TRAN

SELECT InstanceID,
       DatabaseID,
       data_space_id,
       FreeSpaceWarningThreshold,
       FreeSpaceCriticalThreshold,
       FreeSpaceCheckType 
FROM dbo.DBFileThresholds
WHERE InstanceID=@InstanceID 
AND DatabaseID = @DatabaseID
AND data_space_id=@DataSpaceID