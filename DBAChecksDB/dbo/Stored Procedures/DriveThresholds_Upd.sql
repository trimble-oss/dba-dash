CREATE PROC DriveThresholds_Upd(@InstanceID INT,@DriveID INT,@Warning DECIMAL(9,3),@Critical DECIMAL(9,3),@DriveCheckType CHAR(1))
AS
SET NOCOUNT ON
SET XACT_ABORT ON
IF @DriveCheckType NOT IN('G','%','I','-')
BEGIN 
	RAISERROR('Invalid Drive Check Type',11,1);
	RETURN;
END
BEGIN TRAN

DELETE dbo.DriveThresholds
WHERE InstanceID=@InstanceID 
AND DriveID=@DriveID 
IF (@DriveCheckType IN('G','%','-'))
BEGIN
	INSERT INTO dbo.DriveThresholds
	(
		InstanceID,
		DriveID,
		DriveWarningThreshold,
		DriveCriticalThreshold,
		DriveCheckType
	)
	VALUES
	(   @InstanceID,    
		@DriveID,    
		@Warning, 
		@Critical, 
		@DriveCheckType    
		)
END
COMMIT