CREATE PROC dbo.InstanceUptimeThresholds_Upd(
	@InstanceID INT,
	@WarningThreshold INT=NULL,
	@CriticalThreshold INT=NULL,
	@Inherit BIT=0
)
AS
SET XACT_ABORT ON
IF @Inherit=1 AND @InstanceID=-1
BEGIN
	RAISERROR('Root level can''t inherit',11,1)
	RETURN
END
BEGIN TRAN

DELETE dbo.InstanceUptimeThresholds
WHERE InstanceID = @InstanceID

IF @Inherit=0
BEGIN
	INSERT INTO dbo.InstanceUptimeThresholds
	(
		InstanceID,
		WarningThreshold,
		CriticalThreshold
	)
	VALUES
	(   @InstanceID,
		@WarningThreshold,
		@CriticalThreshold
		)

END
COMMIT
GO