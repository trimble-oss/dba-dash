CREATE PROC dbo.DailyReport_RunLock_Upd(
	@DailyReportID INT,
	@Acquire BIT, /* 1: take the report's run lock.  0: release it */
	@LockUTC DATETIME2(3)=NULL OUTPUT, /* Set when the lock is taken (NULL when it's held by another run); pass it back to release it */
	@StaleRunMins INT=60
)
AS
/*
	The run lock (DailyReport.RunningSinceUTC) for a Send now, so a manual send of a changes report never overlaps a scheduled run of the
	same report: both build from and update the report's issue history, and an overlap could send the same changes twice or record an older
	snapshot over a newer one.  dbo.DailyReport_Claim takes the same lock for a scheduled run and skips a report while it's held, so a
	scheduled run waits for the Send now (it's claimed on the next check) rather than being lost.

	Taking it doesn't change LastRunUTC, so a Send now never stands in for a scheduled send.  A lock older than @StaleRunMins is taken over,
	as dbo.DailyReport_Claim does.  Releasing clears it only if it's still this run's lock.
*/
SET NOCOUNT ON
DECLARE @NowUTC DATETIME2(3) = SYSUTCDATETIME()

IF @Acquire = 1
BEGIN
	UPDATE dbo.DailyReport
	SET RunningSinceUTC = @NowUTC
	WHERE DailyReportID = @DailyReportID
	AND (RunningSinceUTC IS NULL OR RunningSinceUTC < DATEADD(mi,-@StaleRunMins,@NowUTC))

	SET @LockUTC = CASE WHEN @@ROWCOUNT = 1 THEN @NowUTC END
END
ELSE
BEGIN
	UPDATE dbo.DailyReport
	SET RunningSinceUTC = NULL
	WHERE DailyReportID = @DailyReportID
	AND RunningSinceUTC = @LockUTC
END
