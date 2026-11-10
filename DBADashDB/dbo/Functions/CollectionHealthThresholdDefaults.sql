/*
	The built-in thresholds for collection health - used where nothing is configured in dbo.CollectionHealthThresholds.

	A run of failures counts once it is both long enough in runs and has gone on long enough in time - so a collection that
	runs every minute has to fail for a while before it shows, while one that runs daily shows on its first failure.  The time
	is from the last successful run to the latest failure.
		Warning:	1 failure, failing for 10 minutes
		Critical:	3 failures in a row, failing for 30 minutes
	The failure rate counts once there are enough runs for it to mean something:
		Warning:	10% failed or partly failed
		Critical:	50% failed, and the latest run failed - a collection that has recovered is a warning at most
		Over at least 5 runs
*/
CREATE FUNCTION dbo.CollectionHealthThresholdDefaults()
RETURNS TABLE
AS
RETURN
SELECT	1 AS WarningConsecutiveFailures,
		10 AS WarningFailingMinutes,
		3 AS CriticalConsecutiveFailures,
		30 AS CriticalFailingMinutes,
		CAST(10 AS DECIMAL(5,2)) AS WarningFailedPct,
		CAST(50 AS DECIMAL(5,2)) AS CriticalFailedPct,
		5 AS MinRunsForFailedPct,
		CAST(0 AS BIT) AS Disabled
