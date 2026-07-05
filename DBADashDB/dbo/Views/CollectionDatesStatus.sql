CREATE VIEW dbo.CollectionDatesStatus
AS
SELECT CD.InstanceID,
	  CD.Reference,
	  CASE WHEN SI.IsEnabled = 0 THEN 8 /* Schedule disabled */
			WHEN T.Disabled = 1 THEN 3 /* Threshold monitoring explicitly disabled */
			WHEN DATEDIFF(mi,CD.SnapshotDate,GETUTCDATE())  > COALESCE(T.CriticalThreshold,Auto.CriticalThreshold) THEN  1
			WHEN DATEDIFF(mi,CD.SnapshotDate,GETUTCDATE()) > COALESCE(T.WarningThreshold,Auto.WarningThreshold) THEN 2
			-- Warning (not NA) only when the *instance* has never reported any ScheduleInfo at all (old
			-- service version, or not restarted since upgrading) - once it has, a reference that still has
			-- no SI row (e.g. a custom collection, which ScheduleInfo doesn't cover) correctly falls to NA
			-- below instead of being stuck on Warning forever.
			WHEN T.WarningThreshold IS NULL AND T.CriticalThreshold IS NULL AND Auto.WarningThreshold IS NULL AND NOT EXISTS(SELECT 1 FROM dbo.ScheduleInfo SI2 WHERE SI2.InstanceID = CD.InstanceID) THEN 2
			WHEN T.WarningThreshold IS NULL AND T.CriticalThreshold IS NULL AND Auto.WarningThreshold IS NULL THEN 3
			ELSE 4 END AS Status,
	CASE WHEN T.Disabled = 1 THEN NULL ELSE COALESCE(T.WarningThreshold,Auto.WarningThreshold) END AS WarningThreshold,
	CASE WHEN T.Disabled = 1 THEN NULL ELSE COALESCE(T.CriticalThreshold,Auto.CriticalThreshold) END AS CriticalThreshold,
	DATEDIFF(mi,CD.SnapshotDate,GETUTCDATE()) AS SnapshotAge,
	HD.HumanDuration AS HumanSnapshotAge,
	CD.SnapshotDate,
	CASE WHEN T.InstanceID > 0 THEN 'Instance' WHEN T.InstanceID = -1 THEN 'Root' ELSE 'Default' END AS ConfiguredLevel,
	CASE WHEN ISNULL(T.Disabled,0) = 0 AND T.WarningThreshold IS NULL AND T.CriticalThreshold IS NULL AND Auto.WarningThreshold IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsScheduleThreshold
FROM dbo.CollectionDates CD
CROSS APPLY dbo.SecondsToHumanDuration(DATEDIFF(s,CD.SnapshotDate,GETUTCDATE())) HD
OUTER APPLY(SELECT TOP(1) CDT.WarningThreshold,
				CDT.CriticalThreshold,
				CDT.WarningMultiplier,
				CDT.CriticalMultiplier,
				CDT.WarningBufferMinutes,
				CDT.CriticalBufferMinutes,
				CDT.Disabled,
				CDT.InstanceID
		FROM dbo.CollectionDatesThresholds CDT
		WHERE (CD.InstanceID = CDT.InstanceID OR CDT.InstanceID=-1)
		AND CDT.Reference = CD.Reference
		ORDER BY InstanceID DESC) T
LEFT JOIN dbo.ScheduleInfo SI ON SI.InstanceID = CD.InstanceID AND SI.Reference = CD.Reference
-- App-shipped defaults (not user-configurable - instance/root level already provides the supported way to
-- override). Keep these in sync with DBADashGUI.CollectionDates.CollectionDatesThresholds' Default* constants.
CROSS APPLY(SELECT
		CAST(CEILING(SI.MaxIntervalMinutes * COALESCE(T.WarningMultiplier,2.0) + COALESCE(T.WarningBufferMinutes,3.0)) AS INT) AS WarningThreshold,
		CAST(CEILING(SI.MaxIntervalMinutes * COALESCE(T.CriticalMultiplier,4.0) + COALESCE(T.CriticalBufferMinutes,6.0)) AS INT) AS CriticalThreshold
	) Auto
