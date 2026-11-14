CREATE PROC dbo.DailyReport_RunResult_Upd(
	@DailyReportID INT,
	@ErrorMessage NVARCHAR(MAX)=NULL,
	@Retry BIT=0 /* 1 when nothing was sent (the build failed, or every channel failed), so dbo.DailyReport_Claim tries again later the same day */
)
AS
SET NOCOUNT ON
UPDATE dbo.DailyReport
SET LastRunError = @ErrorMessage,
	RunningSinceUTC = NULL, /* The run has finished */
	RetryDue = CASE WHEN @ErrorMessage IS NOT NULL AND @Retry = 1 THEN 1 ELSE 0 END
WHERE DailyReportID = @DailyReportID
