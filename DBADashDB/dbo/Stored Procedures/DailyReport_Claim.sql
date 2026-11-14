CREATE PROC dbo.DailyReport_Claim(
	@MaxRetries TINYINT=2,
	@RetryDelayMins INT=15,
	@StaleRunMins INT=60
)
AS
/*
	Called by the service on a short interval.  Returns the reports that are due and marks them as run, so each scheduled time is sent
	once even if more than one service instance is running.

	A report's scheduled times are in its time zone, on the days in DaysOfWeek (a bit per day, Monday 1 ... Sunday 64):
	  - IntervalMinutes NULL: once a day at ScheduleTime.
	  - Otherwise every IntervalMinutes from ScheduleTime, up to WindowEndTime (NULL: the end of the day).
	The current slot is the latest scheduled time that has passed today.  A report is due when it hasn't been claimed since that slot
	started.  So a slot missed while the service was down is sent once when it restarts - the latest one only, not one per missed slot -
	and after the last slot of the day the report waits for the next day it's scheduled on.

	A report created after a slot's time waits for the next slot rather than going out the moment it's saved - Send now in the GUI is for that.

	A report isn't claimed while its last run is still in progress (RunningSinceUTC), so with more than one service a slow run of a
	repeating report doesn't overlap the next.  A run that never records its result stops blocking after @StaleRunMins.  If it also
	sent nothing (no LastSentUTC since it was claimed), it counts as a retry of the same slot, so a crash mid-run doesn't lose the day's report.
	Send now takes the same lock (dbo.DailyReport_RunLock_Upd) so a scheduled run and a manual one never overlap.

	A run that sent nothing (dbo.DailyReport_RunResult_Upd @Retry=1 - e.g. the SMTP server was down) is tried again @RetryDelayMins later,
	up to @MaxRetries times within the same slot.  A run that reached some channels isn't retried, so those recipients don't get it twice.
*/
SET NOCOUNT ON
SET XACT_ABORT ON

DECLARE @NowUTC DATETIME2(3) = SYSUTCDATETIME()
DECLARE @Midnight TIME(0) = '00:00'
CREATE TABLE #Due(DailyReportID INT NOT NULL PRIMARY KEY, IsRetry BIT NOT NULL)

BEGIN TRAN
INSERT INTO #Due(DailyReportID, IsRetry)
SELECT R.DailyReportID, D.IsRetry
FROM dbo.DailyReport R WITH (UPDLOCK, ROWLOCK, READPAST)
CROSS APPLY (SELECT CAST(CAST(@NowUTC AS DATETIMEOFFSET(0)) AT TIME ZONE R.TimeZone AS DATETIME2(3)) AS LocalNow,
					CAST(CAST(R.CreatedDate AS DATETIMEOFFSET(0)) AT TIME ZONE R.TimeZone AS DATETIME2(3)) AS LocalCreated,
					CAST(CAST(R.LastRunUTC AS DATETIMEOFFSET(0)) AT TIME ZONE R.TimeZone AS DATETIME2(3)) AS LocalLastRun) L
/* Minutes after midnight: now, the first scheduled time and the last one the window allows */
CROSS APPLY (SELECT DATEDIFF(mi, @Midnight, CAST(L.LocalNow AS TIME(0))) AS NowMin,
					DATEDIFF(mi, @Midnight, R.ScheduleTime) AS StartMin,
					CASE WHEN R.IntervalMinutes IS NULL THEN 0
						 ELSE (CASE WHEN R.WindowEndTime IS NULL THEN 1439 ELSE DATEDIFF(mi, @Midnight, R.WindowEndTime) END - DATEDIFF(mi, @Midnight, R.ScheduleTime)) / R.IntervalMinutes
					END AS LastSlot,
					/* Monday is bit 1: 1900-01-01 was a Monday */
					POWER(2, DATEDIFF(dd, 0, CAST(L.LocalNow AS DATE)) % 7) AS DayBit) M
/* The latest scheduled time that has passed today - NULL before the first one */
CROSS APPLY (SELECT CASE WHEN M.NowMin >= M.StartMin
						 THEN DATEADD(mi, M.StartMin + ISNULL(R.IntervalMinutes,0) *
								CASE WHEN R.IntervalMinutes IS NULL THEN 0
									 WHEN (M.NowMin - M.StartMin) / R.IntervalMinutes < M.LastSlot THEN (M.NowMin - M.StartMin) / R.IntervalMinutes
									 ELSE M.LastSlot END,
								CAST(CAST(L.LocalNow AS DATE) AS DATETIME2(3)))
					END AS SlotStart) S
CROSS APPLY (SELECT CASE WHEN S.SlotStart IS NULL OR R.DaysOfWeek & M.DayBit = 0 THEN NULL
						WHEN (L.LocalLastRun IS NULL OR L.LocalLastRun < S.SlotStart) AND L.LocalCreated < S.SlotStart
						THEN 0
						WHEN R.RetryDue = 1
						AND R.RetryAttempts < @MaxRetries
						AND L.LocalLastRun >= S.SlotStart
						AND R.LastRunUTC <= DATEADD(mi,-@RetryDelayMins,@NowUTC)
						THEN 1
						/* The claim for this slot expired without a result (the service stopped mid-run) and nothing was sent since: run it again.
						   Only a scheduled claim (RunningSinceUTC = LastRunUTC) - a Send now holds the same lock, but the slot was never its to send */
						WHEN R.RunningSinceUTC < DATEADD(mi,-@StaleRunMins,@NowUTC)
						AND R.RunningSinceUTC = R.LastRunUTC
						AND R.RetryAttempts < @MaxRetries
						AND L.LocalLastRun >= S.SlotStart
						AND (R.LastSentUTC IS NULL OR R.LastSentUTC < R.RunningSinceUTC)
						THEN 1
					END AS IsRetry) D
WHERE R.IsActive = 1
AND D.IsRetry IS NOT NULL
/* Not while a run is in progress - unless it started over @StaleRunMins ago and so died without recording its result */
AND (R.RunningSinceUTC IS NULL OR R.RunningSinceUTC < DATEADD(mi,-@StaleRunMins,@NowUTC))

UPDATE R
SET LastRunUTC = @NowUTC,
	RunningSinceUTC = @NowUTC,
	LastRunError = NULL,
	RetryDue = 0,
	RetryAttempts = CASE WHEN D.IsRetry = 1 THEN R.RetryAttempts + 1 ELSE 0 END
FROM dbo.DailyReport R
JOIN #Due D ON D.DailyReportID = R.DailyReportID
COMMIT

SELECT DailyReportID
FROM #Due
