CREATE PROC dbo.CollectionHealthThresholds_Upd(
	@InstanceID INT, /* -1 for every instance */
	@Reference VARCHAR(100), /* '' for every collection */
	@WarningConsecutiveFailures INT = NULL, /* NULL to inherit each value from the level above */
	@WarningFailingMinutes INT = NULL,
	@CriticalConsecutiveFailures INT = NULL,
	@CriticalFailingMinutes INT = NULL,
	@WarningFailedPct DECIMAL(5,2) = NULL,
	@CriticalFailedPct DECIMAL(5,2) = NULL,
	@MinRunsForFailedPct INT = NULL,
	@Disabled BIT = NULL
)
AS
/*
	Sets the collection health thresholds at one level - see dbo.CollectionHealthThresholds.  Everything NULL removes the row,
	so the level inherits it all.
*/
SET NOCOUNT ON
SET XACT_ABORT ON

IF @InstanceID IS NULL OR @Reference IS NULL
BEGIN;
	THROW 51000, '@InstanceID and @Reference are required.  Use -1 for every instance and '''' for every collection.', 1;
END

BEGIN TRAN

DELETE dbo.CollectionHealthThresholds
WHERE InstanceID = @InstanceID
AND Reference = @Reference

IF COALESCE(@WarningConsecutiveFailures,@WarningFailingMinutes,@CriticalConsecutiveFailures,@CriticalFailingMinutes,@MinRunsForFailedPct) IS NOT NULL
	OR COALESCE(@WarningFailedPct,@CriticalFailedPct) IS NOT NULL
	OR @Disabled IS NOT NULL
BEGIN
	INSERT INTO dbo.CollectionHealthThresholds(
		InstanceID,
		Reference,
		WarningConsecutiveFailures,
		WarningFailingMinutes,
		CriticalConsecutiveFailures,
		CriticalFailingMinutes,
		WarningFailedPct,
		CriticalFailedPct,
		MinRunsForFailedPct,
		Disabled
	)
	VALUES(
		@InstanceID,
		@Reference,
		@WarningConsecutiveFailures,
		@WarningFailingMinutes,
		@CriticalConsecutiveFailures,
		@CriticalFailingMinutes,
		@WarningFailedPct,
		@CriticalFailedPct,
		@MinRunsForFailedPct,
		@Disabled
	)
END

COMMIT
