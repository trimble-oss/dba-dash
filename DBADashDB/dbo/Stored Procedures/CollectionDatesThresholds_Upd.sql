CREATE PROC CollectionDatesThresholds_Upd(
	@InstanceID INT,
	@Reference VARCHAR(30),
	@WarningThreshold INT,
	@CriticalThreshold INT,
	@Inherit BIT=0,
	@WarningMultiplier DECIMAL(9,2)=NULL,
	@CriticalMultiplier DECIMAL(9,2)=NULL,
	@WarningBufferMinutes DECIMAL(9,2)=NULL,
	@CriticalBufferMinutes DECIMAL(9,2)=NULL,
	@Disabled BIT=0
)
AS
-- @Inherit=1 means "no override at this level, defer to the next one down" (instance -> root ->
-- built-in system default) - just remove the row rather than storing a marker. The Inherited/
-- ConfiguredLevel logic in CollectionDatesThresholds_Get.sql and dbo.CollectionDatesStatus already
-- derives that state correctly from the row's absence.
DELETE dbo.CollectionDatesThresholds
WHERE Reference = @Reference
AND InstanceID = @InstanceID

IF @Inherit = 0
BEGIN
	INSERT INTO dbo.CollectionDatesThresholds
	(
		InstanceID,
		Reference,
		WarningThreshold,
		CriticalThreshold,
		WarningMultiplier,
		CriticalMultiplier,
		WarningBufferMinutes,
		CriticalBufferMinutes,
		Disabled
	)
	VALUES
	(   @InstanceID,
		@Reference,
		@WarningThreshold,
		@CriticalThreshold,
		@WarningMultiplier,
		@CriticalMultiplier,
		@WarningBufferMinutes,
		@CriticalBufferMinutes,
		@Disabled
		)
END
