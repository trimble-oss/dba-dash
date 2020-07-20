CREATE PROC LastGoodCheckDBThresholds_Upd(
	@InstanceID INT,
	@DatabaseID INT,
	@WarningThreshold INT=NULL,
	@CriticalThreshold INT=NULL,
	@Inherit BIT=0
)
AS
SET XACT_ABORT ON

BEGIN TRAN
DELETE dbo.LastGoodCheckDBThresholds
WHERE InstanceID = @InstanceID
AND DatabaseID = @DatabaseID

IF @Inherit=0
BEGIN
	INSERT INTO dbo.LastGoodCheckDBThresholds
	(
	    InstanceID,
	    DatabaseID,
	    WarningThresholdHrs,
	    CriticalThresholdHrs
	)
	VALUES
	(   @InstanceID,
	    @DatabaseID,
	    @WarningThreshold,
	    @CriticalThreshold
	    )

END

COMMIT