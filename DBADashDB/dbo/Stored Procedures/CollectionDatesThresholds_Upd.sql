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
-- Validate/normalize @WarningThreshold etc before touching the table at all - if this throws, the
-- existing row (if any) must still be intact rather than having already been deleted below.
IF @Inherit = 0
BEGIN
	IF @Disabled = 1
	BEGIN
		-- Disabled always means every threshold/multiplier/buffer column is NULL, regardless of whatever
		-- else was passed in - normalize here so the row can never disagree with its own Disabled flag.
		SET @WarningThreshold = NULL;
		SET @CriticalThreshold = NULL;
		SET @WarningMultiplier = NULL;
		SET @CriticalMultiplier = NULL;
		SET @WarningBufferMinutes = NULL;
		SET @CriticalBufferMinutes = NULL;
	END
	ELSE IF @WarningThreshold IS NOT NULL OR @CriticalThreshold IS NOT NULL
	BEGIN
		-- Explicit: both thresholds are required together, and the multiplier/buffer columns (which only
		-- apply to Schedule Based) are cleared so a stale value can't linger from a previous save.
		IF @WarningThreshold IS NULL OR @CriticalThreshold IS NULL
		BEGIN;
			THROW 51000, 'Explicit thresholds require both @WarningThreshold and @CriticalThreshold to be set', 1;
		END
		SET @WarningMultiplier = NULL;
		SET @CriticalMultiplier = NULL;
		SET @WarningBufferMinutes = NULL;
		SET @CriticalBufferMinutes = NULL;
	END
	ELSE
	BEGIN
		-- Schedule Based: the caller must supply all four - this proc deliberately doesn't fall back to
		-- its own copy of the app-shipped defaults (those already live in DBADashGUI.CollectionDates.
		-- CollectionDatesThresholds' Default* constants and dbo.CollectionDatesStatus's Auto CROSS APPLY;
		-- a third copy here would be one more place for them to drift out of sync).
		IF @WarningMultiplier IS NULL OR @CriticalMultiplier IS NULL OR @WarningBufferMinutes IS NULL OR @CriticalBufferMinutes IS NULL
		BEGIN;
			THROW 51001, 'Schedule Based thresholds require @WarningMultiplier, @CriticalMultiplier, @WarningBufferMinutes and @CriticalBufferMinutes to all be set', 1;
		END
	END;
END

-- @Inherit=1 means "no override at this level, defer to the next one down" (instance -> root ->
-- built-in system default) - just remove the row rather than storing a marker. The Inherited/
-- ConfiguredLevel logic in CollectionDatesThresholds_Get.sql and dbo.CollectionDatesStatus already
-- derives that state correctly from the row's absence.
DELETE dbo.CollectionDatesThresholds
WHERE Reference = @Reference
AND InstanceID = @InstanceID;

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
		);
END
