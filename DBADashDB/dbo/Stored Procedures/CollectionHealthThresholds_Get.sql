CREATE PROC dbo.CollectionHealthThresholds_Get(
	@InstanceID INT, /* -1 for every instance */
	@Reference VARCHAR(100) /* '' for every collection */
)
AS
/*
	The collection health thresholds configured at one level (dbo.CollectionHealthThresholds), and what that level inherits
	from the levels above it - shown in the dialog for the values left to inherit.  NULL configured means inherited.

	Levels, most specific first: this instance and collection, this instance and every collection, this collection on every
	instance, everything, the built-in defaults.  Keep in step with dbo.CollectionHealthStatus.
*/
SET NOCOUNT ON

DECLARE @Level TINYINT = CASE	WHEN @InstanceID <> -1 AND @Reference <> '' THEN 1
								WHEN @InstanceID <> -1 THEN 2
								WHEN @Reference <> '' THEN 3
								ELSE 4 END

SELECT	@InstanceID AS InstanceID,
		@Reference AS Reference,
		C.WarningConsecutiveFailures,
		C.WarningFailingMinutes,
		C.CriticalConsecutiveFailures,
		C.CriticalFailingMinutes,
		C.WarningFailedPct,
		C.CriticalFailedPct,
		C.MinRunsForFailedPct,
		C.Disabled,
		COALESCE(CASE WHEN @Level = 1 THEN T2.WarningConsecutiveFailures END,CASE WHEN @Level = 1 THEN T3.WarningConsecutiveFailures END,CASE WHEN @Level < 4 THEN T4.WarningConsecutiveFailures END,D.WarningConsecutiveFailures) AS InheritedWarningConsecutiveFailures,
		COALESCE(CASE WHEN @Level = 1 THEN T2.WarningFailingMinutes END,CASE WHEN @Level = 1 THEN T3.WarningFailingMinutes END,CASE WHEN @Level < 4 THEN T4.WarningFailingMinutes END,D.WarningFailingMinutes) AS InheritedWarningFailingMinutes,
		COALESCE(CASE WHEN @Level = 1 THEN T2.CriticalConsecutiveFailures END,CASE WHEN @Level = 1 THEN T3.CriticalConsecutiveFailures END,CASE WHEN @Level < 4 THEN T4.CriticalConsecutiveFailures END,D.CriticalConsecutiveFailures) AS InheritedCriticalConsecutiveFailures,
		COALESCE(CASE WHEN @Level = 1 THEN T2.CriticalFailingMinutes END,CASE WHEN @Level = 1 THEN T3.CriticalFailingMinutes END,CASE WHEN @Level < 4 THEN T4.CriticalFailingMinutes END,D.CriticalFailingMinutes) AS InheritedCriticalFailingMinutes,
		COALESCE(CASE WHEN @Level = 1 THEN T2.WarningFailedPct END,CASE WHEN @Level = 1 THEN T3.WarningFailedPct END,CASE WHEN @Level < 4 THEN T4.WarningFailedPct END,D.WarningFailedPct) AS InheritedWarningFailedPct,
		COALESCE(CASE WHEN @Level = 1 THEN T2.CriticalFailedPct END,CASE WHEN @Level = 1 THEN T3.CriticalFailedPct END,CASE WHEN @Level < 4 THEN T4.CriticalFailedPct END,D.CriticalFailedPct) AS InheritedCriticalFailedPct,
		COALESCE(CASE WHEN @Level = 1 THEN T2.MinRunsForFailedPct END,CASE WHEN @Level = 1 THEN T3.MinRunsForFailedPct END,CASE WHEN @Level < 4 THEN T4.MinRunsForFailedPct END,D.MinRunsForFailedPct) AS InheritedMinRunsForFailedPct,
		COALESCE(CASE WHEN @Level = 1 THEN T2.Disabled END,CASE WHEN @Level = 1 THEN T3.Disabled END,CASE WHEN @Level < 4 THEN T4.Disabled END,D.Disabled) AS InheritedDisabled
FROM dbo.CollectionHealthThresholdDefaults() D
LEFT JOIN dbo.CollectionHealthThresholds C ON C.InstanceID = @InstanceID AND C.Reference = @Reference
LEFT JOIN dbo.CollectionHealthThresholds T2 ON T2.InstanceID = @InstanceID AND T2.Reference = ''
LEFT JOIN dbo.CollectionHealthThresholds T3 ON T3.InstanceID = -1 AND T3.Reference = @Reference
LEFT JOIN dbo.CollectionHealthThresholds T4 ON T4.InstanceID = -1 AND T4.Reference = ''
