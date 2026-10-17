CREATE VIEW dbo.CollectionDatesStatus
AS
SELECT CD.InstanceID,
	  CD.Reference,
	  CASE WHEN SI.IsEnabled = 0 THEN 8 /* Schedule disabled */
			WHEN Paused.CollectionsPausedUntil > GETUTCDATE() THEN 3 /* Collections paused - not overdue while paused */
			WHEN T.Disabled = 1 THEN 3 /* Threshold monitoring explicitly disabled */
			-- Status is measured against the last time the collection *ran* (LastRun.LastRunDate), not the
			-- last time data changed.  For most collections these are identical, but change-detection
			-- collections (e.g. Jobs) only write data when something changed and otherwise emit a heartbeat -
			-- basing status on SnapshotDate alone would wrongly report them as overdue between changes.
			-- Measured from the end of the latest pause if that's later, so a collection isn't overdue the moment a pause ends.
			WHEN DATEDIFF(mi,DueFrom.DueFromDate,GETUTCDATE())  > COALESCE(T.CriticalThreshold,Auto.CriticalThreshold) THEN  1
			WHEN DATEDIFF(mi,DueFrom.DueFromDate,GETUTCDATE()) > COALESCE(T.WarningThreshold,Auto.WarningThreshold) THEN 2
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
	-- Last time the collection ran (heartbeat or data write, whichever is newer).  Drives Status above and
	-- is surfaced separately so the GUI can distinguish "checked recently" from "data last changed".
	LastRun.LastRunDate AS HeartbeatDate,
	DATEDIFF(mi,LastRun.LastRunDate,GETUTCDATE()) AS HeartbeatAge,
	HDH.HumanDuration AS HumanHeartbeatAge,
	CASE WHEN T.InstanceID > 0 THEN 'Instance' WHEN T.InstanceID = -1 THEN 'Root' ELSE 'Default' END AS ConfiguredLevel,
	CASE WHEN ISNULL(T.Disabled,0) = 0 AND T.WarningThreshold IS NULL AND T.CriticalThreshold IS NULL AND Auto.WarningThreshold IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsScheduleThreshold,
	-- SnapshotAge counted from the end of the latest pause if that's later - time paused isn't time overdue.  Used for
	-- alerting, so collections don't alert the moment a pause ends, before they've had a chance to run.
	DATEDIFF(mi,CASE WHEN Paused.CollectionsPausedUntil > CD.SnapshotDate THEN Paused.CollectionsPausedUntil ELSE CD.SnapshotDate END,GETUTCDATE()) AS SnapshotAgeExcludingPause
FROM dbo.CollectionDates CD
-- Fall back to SnapshotDate for rows written by a service version that predates heartbeats (HeartbeatDate
-- NULL).  Heartbeats never lag SnapshotDate, but guard with a CASE anyway so status can't regress.
CROSS APPLY(SELECT CASE WHEN CD.HeartbeatDate > CD.SnapshotDate THEN CD.HeartbeatDate ELSE CD.SnapshotDate END AS LastRunDate) LastRun
OUTER APPLY(SELECT I.CollectionsPausedUntil FROM dbo.Instances I WHERE I.InstanceID = CD.InstanceID) Paused
CROSS APPLY(SELECT CASE WHEN Paused.CollectionsPausedUntil > LastRun.LastRunDate THEN Paused.CollectionsPausedUntil ELSE LastRun.LastRunDate END AS DueFromDate) DueFrom
CROSS APPLY dbo.SecondsToHumanDuration(DATEDIFF(s,CD.SnapshotDate,GETUTCDATE())) HD
CROSS APPLY dbo.SecondsToHumanDuration(DATEDIFF(s,LastRun.LastRunDate,GETUTCDATE())) HDH
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
