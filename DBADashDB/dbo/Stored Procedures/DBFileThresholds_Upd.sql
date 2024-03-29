﻿CREATE PROC [dbo].[DBFileThresholds_Upd](
	@InstanceID INT,
	@DatabaseID INT,
	@DataSpaceID INT,
	@Warning DECIMAL(9,3),
	@Critical DECIMAL(9,3),
	@CheckType CHAR(1),
	@PctMaxSizeWarningThreshold DECIMAL(9,3),
	@PctMaxSizeCriticalThreshold DECIMAL(9,3),
	@FreeSpaceCheckZeroAutogrowthOnly BIT
)
AS
SET NOCOUNT ON
SET XACT_ABORT ON
IF @CheckType NOT IN('M','%','I','-')
BEGIN 
	RAISERROR('Invalid File Check Type',11,1);
	RETURN;
END
BEGIN TRAN

DELETE dbo.DBFileThresholds
WHERE InstanceID=@InstanceID 
AND DatabaseID = @DatabaseID
AND data_space_id=@DataSpaceID

IF (@CheckType IN('M','%','-'))
BEGIN
	INSERT INTO dbo.DBFileThresholds
	(
		InstanceID,
		DatabaseID,
		data_space_id,
		FreeSpaceWarningThreshold,
		FreeSpaceCriticalThreshold,
		FreeSpaceCheckType,
		PctMaxSizeWarningThreshold,
		PctMaxSizeCriticalThreshold,
		FreeSpaceCheckZeroAutogrowthOnly
	)
	VALUES
	(   @InstanceID,    
		@DatabaseID, 
		@DataSpaceID,
		@Warning, 
		@Critical, 
		@CheckType,
		@PctMaxSizeWarningThreshold,
		@PctMaxSizeCriticalThreshold,
		@FreeSpaceCheckZeroAutogrowthOnly
		)
END
COMMIT;