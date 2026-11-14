CREATE PROC dbo.DailyReport_Del(
	@DailyReportID INT
)
AS
SET NOCOUNT ON
SET XACT_ABORT ON

BEGIN TRAN
DELETE dbo.DailyReportIssueState
WHERE DailyReportID = @DailyReportID

DELETE dbo.DailyReportChannel
WHERE DailyReportID = @DailyReportID

DELETE dbo.DailyReport
WHERE DailyReportID = @DailyReportID
COMMIT
