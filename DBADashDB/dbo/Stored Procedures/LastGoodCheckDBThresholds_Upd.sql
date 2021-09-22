CREATE PROC dbo.LastGoodCheckDBThresholds_Upd(
	@InstanceID INT,
	@DatabaseID INT,
	@WarningThreshold INT=NULL,
	@CriticalThreshold INT=NULL,
	@Inherit BIT=0,
	@MinimumAge INT=NULL,
	@ExcludedDatabases NVARCHAR(MAX)=NULL
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
	    CriticalThresholdHrs,
		MinimumAge,
		ExcludedDatabases
	)
	VALUES
	(   @InstanceID,
	    @DatabaseID,
	    @WarningThreshold,
	    @CriticalThreshold,
		@MinimumAge,
		@ExcludedDatabases
	    )

END

COMMIT