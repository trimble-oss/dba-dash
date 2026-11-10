/*
	How healthy a collection is on an instance - shared by the summary (dbo.Summary_Upd) and the Collection Health report
	(dbo.CollectionHealth_Get) so the two always agree.  Status as the other DBA Dash statuses: 1 critical, 2 warning, 3 N/A,
	4 OK.

	Two things count, and the worse wins:
	- Failures in a row now (dbo.CollectionRunStatus).  A run of failures counts once it is long enough in runs AND has gone
	  on long enough in time, from the last successful run to the latest failure.  The time keeps a collection that runs every
	  minute from showing on a short blip, while one that runs daily shows on its first failure.  A collection that has never
	  succeeded has failed for as long as we know, so only the runs count.
	- The failure rate over a period (the caller's counts - the summary's window, or the report's range), once there are
	  enough runs for it to mean something.  Partly failed runs count towards a warning.  The rate is only critical while the
	  latest run failed: a collection that was failing and has been fixed is a warning at most.

	Thresholds come from dbo.CollectionHealthThresholds, resolved a column at a time from the most specific row: this
	instance and collection, this instance and every collection, this collection on every instance, everything.  Then the
	built-in defaults (dbo.CollectionHealthThresholdDefaults).

	@Reference is the collection - a step (Text, Plans...) uses the collection's thresholds.
	@FailedCount is failed and import failed runs.  @KnownCount is runs with a status.
*/
CREATE FUNCTION dbo.CollectionHealthStatus(
	@InstanceID INT,
	@Reference VARCHAR(100),
	@ConsecutiveFailures INT,
	@LastStatus TINYINT,
	@LastRunDate DATETIME2(2),
	@LastSuccessDate DATETIME2(2),
	@KnownCount INT,
	@FailedCount INT,
	@PartialCount INT
)
RETURNS TABLE
AS
RETURN
SELECT	Calc.Status,
		CASE	WHEN Calc.Status = 3 AND T.Disabled = 1 THEN 'Disabled'
				WHEN Calc.Status = 3 THEN NULL
				WHEN Calc.Status IN(1,2) AND (Calc.StreakStatus = Calc.Status OR Calc.RateStatus IS NULL) THEN Streak.Reason
				WHEN Calc.Status IN(1,2) THEN Rate.Reason
				END AS StatusReason,
		Fail.FailingMinutes,
		Rate.FailedPct,
		Rate.ProblemPct,
		Lvl.ThresholdLevel,
		T.WarningConsecutiveFailures,
		T.WarningFailingMinutes,
		T.CriticalConsecutiveFailures,
		T.CriticalFailingMinutes,
		T.WarningFailedPct,
		T.CriticalFailedPct,
		T.MinRunsForFailedPct,
		T.Disabled
FROM dbo.CollectionHealthThresholdDefaults() D
LEFT JOIN dbo.CollectionHealthThresholds T1 ON T1.InstanceID = @InstanceID AND T1.Reference = @Reference
LEFT JOIN dbo.CollectionHealthThresholds T2 ON T2.InstanceID = @InstanceID AND T2.Reference = ''
LEFT JOIN dbo.CollectionHealthThresholds T3 ON T3.InstanceID = -1 AND T3.Reference = @Reference
LEFT JOIN dbo.CollectionHealthThresholds T4 ON T4.InstanceID = -1 AND T4.Reference = ''
CROSS APPLY(SELECT	COALESCE(T1.WarningConsecutiveFailures,T2.WarningConsecutiveFailures,T3.WarningConsecutiveFailures,T4.WarningConsecutiveFailures,D.WarningConsecutiveFailures) AS WarningConsecutiveFailures,
					COALESCE(T1.WarningFailingMinutes,T2.WarningFailingMinutes,T3.WarningFailingMinutes,T4.WarningFailingMinutes,D.WarningFailingMinutes) AS WarningFailingMinutes,
					COALESCE(T1.CriticalConsecutiveFailures,T2.CriticalConsecutiveFailures,T3.CriticalConsecutiveFailures,T4.CriticalConsecutiveFailures,D.CriticalConsecutiveFailures) AS CriticalConsecutiveFailures,
					COALESCE(T1.CriticalFailingMinutes,T2.CriticalFailingMinutes,T3.CriticalFailingMinutes,T4.CriticalFailingMinutes,D.CriticalFailingMinutes) AS CriticalFailingMinutes,
					COALESCE(T1.WarningFailedPct,T2.WarningFailedPct,T3.WarningFailedPct,T4.WarningFailedPct,D.WarningFailedPct) AS WarningFailedPct,
					COALESCE(T1.CriticalFailedPct,T2.CriticalFailedPct,T3.CriticalFailedPct,T4.CriticalFailedPct,D.CriticalFailedPct) AS CriticalFailedPct,
					COALESCE(T1.MinRunsForFailedPct,T2.MinRunsForFailedPct,T3.MinRunsForFailedPct,T4.MinRunsForFailedPct,D.MinRunsForFailedPct) AS MinRunsForFailedPct,
					COALESCE(T1.Disabled,T2.Disabled,T3.Disabled,T4.Disabled,D.Disabled) AS Disabled
			) T
CROSS APPLY(SELECT CASE WHEN T1.InstanceID IS NOT NULL THEN 'Instance'
						WHEN T2.InstanceID IS NOT NULL THEN 'Instance (all collections)'
						WHEN T3.InstanceID IS NOT NULL THEN 'Collection'
						WHEN T4.InstanceID IS NOT NULL THEN 'All'
						ELSE 'Default' END AS ThresholdLevel
			) Lvl
/* NULL when it has never succeeded - failing for as long as we know */
CROSS APPLY(SELECT CASE WHEN ISNULL(@ConsecutiveFailures,0) = 0 THEN 0
						WHEN @LastSuccessDate IS NULL THEN NULL
						ELSE DATEDIFF(mi,@LastSuccessDate,@LastRunDate) END AS FailingMinutes
			) Fail
CROSS APPLY(SELECT	CASE WHEN @ConsecutiveFailures >= T.CriticalConsecutiveFailures AND (Fail.FailingMinutes IS NULL OR Fail.FailingMinutes >= T.CriticalFailingMinutes) THEN 1
						 WHEN @ConsecutiveFailures >= T.WarningConsecutiveFailures AND (Fail.FailingMinutes IS NULL OR Fail.FailingMinutes >= T.WarningFailingMinutes) THEN 2
						 END AS Status,
					CONCAT(CASE WHEN @ConsecutiveFailures = 1 THEN 'Failed once' ELSE CONCAT('Failed ',@ConsecutiveFailures,' times in a row') END,
							CASE	WHEN Fail.FailingMinutes IS NULL THEN ' and has never succeeded'
									WHEN Fail.FailingMinutes < 120 THEN CONCAT(', ',Fail.FailingMinutes,' min since it last succeeded')
									WHEN Fail.FailingMinutes < 2880 THEN CONCAT(', ',Fail.FailingMinutes / 60,' hours since it last succeeded')
									ELSE CONCAT(', ',Fail.FailingMinutes / 1440,' days since it last succeeded') END
							) AS Reason
			) Streak
CROSS APPLY(SELECT	CAST(100.0 * @FailedCount / NULLIF(@KnownCount,0) AS DECIMAL(5,2)) AS FailedPct,
					CAST(100.0 * (@FailedCount + ISNULL(@PartialCount,0)) / NULLIF(@KnownCount,0) AS DECIMAL(5,2)) AS ProblemPct
			) P
CROSS APPLY(SELECT	P.FailedPct,
					P.ProblemPct,
					/* Only with a failure to count - so a 0% threshold means any failure, and the summary (which only reads the
					   rate for collections that failed or partly failed) agrees with the report */
					CASE WHEN @KnownCount < T.MinRunsForFailedPct THEN NULL
						 WHEN @FailedCount > 0 AND P.FailedPct >= T.CriticalFailedPct AND @LastStatus IN(5,6) THEN 1
						 WHEN @FailedCount + ISNULL(@PartialCount,0) > 0 AND P.ProblemPct >= T.WarningFailedPct THEN 2
						 END AS Status,
					CONCAT(CAST(P.FailedPct AS DECIMAL(5,1)),'% of ',@KnownCount,' runs failed',
							CASE WHEN @PartialCount > 0 THEN CONCAT(', ',CAST(P.ProblemPct - P.FailedPct AS DECIMAL(5,1)),'% partly') ELSE '' END
							) AS Reason
			) Rate
CROSS APPLY(SELECT	Streak.Status AS StreakStatus,
					Rate.Status AS RateStatus,
					CASE	WHEN T.Disabled = 1 THEN 3
							WHEN @LastStatus IS NULL AND ISNULL(@KnownCount,0) = 0 THEN 3 /* Only runs from an older service version, with no status */
							WHEN Streak.Status = 1 OR Rate.Status = 1 THEN 1
							WHEN Streak.Status = 2 OR Rate.Status = 2 THEN 2
							ELSE 4 END AS Status
			) Calc
