CREATE PROC dbo.DailyReport_SendResult_Upd(
	@DailyReportID INT,
	@Sent BIT, /* Reached at least one channel */
	@Result NVARCHAR(MAX) /* One line per channel: the mail server's reply, or why it wasn't sent */
)
AS
/*
	Records a send of a daily report - scheduled or Send now - so the GUI can show when it last went out and what each channel did with it.
	Separate from LastRunUTC, which is the scheduled run's claim: a Send now mustn't stop today's scheduled report.
*/
SET NOCOUNT ON
UPDATE dbo.DailyReport
SET LastSendAttemptUTC = SYSUTCDATETIME(),
	LastSentUTC = CASE WHEN @Sent = 1 THEN SYSUTCDATETIME() ELSE LastSentUTC END,
	LastSendResult = @Result
WHERE DailyReportID = @DailyReportID
