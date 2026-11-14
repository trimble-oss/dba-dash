CREATE PROC dbo.DailyReport_IssueState_Upd(
	@DailyReportID INT,
	@Issues dbo.DailyReportIssues READONLY,
	@RunUTC DATETIME2(3),
	@Update BIT=0, /* 0: compare only (preview, send now, and a scheduled run before it's sent).  1: record this run as the last one */
	/* Keys starting with this aren't removed by an update - the part of the report they come from couldn't be read (e.g. alert|), so they
	   aren't known to have gone.  NULL: every key not in @Issues is removed */
	@KeepKeyPrefix NVARCHAR(20)=NULL
)
AS
/*
	Compares a daily report's issues with the ones its last scheduled run recorded.

	Result sets (always returned, so a compare and an update read the same):
		0  One row: IsFirstRun (nothing recorded yet, so nothing can be called new) and PreviousRunUTC.
		1  The current issues that were already there, with when they were first seen and their status then.  An issue not in this set is new.
		2  The issues the last run recorded that aren't here now - resolved since the last report - with their status then.

	@Update=1 is called once the report has been sent: the current issues become the recorded ones, keeping each ongoing issue's
	first seen time, and the resolved ones are removed.  A new key keeps the FirstSeenUTC it's given (an issue now described
	differently is still as old as it was), otherwise it's first seen now.  Calling it only after a send means a retried run still shows the same new issues.
*/
SET NOCOUNT ON
SET XACT_ABORT ON

DECLARE @PreviousRunUTC DATETIME2(3) = (SELECT MAX(LastSeenUTC) FROM dbo.DailyReportIssueState WHERE DailyReportID = @DailyReportID)
DECLARE @IsFirstRun BIT = CASE WHEN EXISTS(SELECT 1 FROM dbo.DailyReport WHERE DailyReportID = @DailyReportID AND IssueStateInitialized = 1) THEN 0 ELSE 1 END

SELECT @IsFirstRun AS IsFirstRun, @PreviousRunUTC AS PreviousRunUTC

SELECT I.IssueKey, S.FirstSeenUTC, S.Status AS PreviousStatus
FROM @Issues I
JOIN dbo.DailyReportIssueState S ON S.DailyReportID = @DailyReportID AND S.IssueKey = I.IssueKey

SELECT S.IssueKey, S.Description, S.FirstSeenUTC, S.Status
FROM dbo.DailyReportIssueState S
WHERE S.DailyReportID = @DailyReportID
AND NOT EXISTS(SELECT 1 FROM @Issues I WHERE I.IssueKey = S.IssueKey)
ORDER BY S.Description

IF @Update = 1
BEGIN
	BEGIN TRAN

	DELETE S
	FROM dbo.DailyReportIssueState S
	WHERE S.DailyReportID = @DailyReportID
	AND NOT EXISTS(SELECT 1 FROM @Issues I WHERE I.IssueKey = S.IssueKey)
	AND (@KeepKeyPrefix IS NULL OR S.IssueKey NOT LIKE @KeepKeyPrefix + N'%')

	UPDATE S
	SET LastSeenUTC = @RunUTC,
		Description = I.Description,
		Status = I.Status
	FROM dbo.DailyReportIssueState S
	JOIN @Issues I ON I.IssueKey = S.IssueKey
	WHERE S.DailyReportID = @DailyReportID

	INSERT INTO dbo.DailyReportIssueState(DailyReportID, IssueKey, Description, Status, FirstSeenUTC, LastSeenUTC)
	SELECT @DailyReportID, I.IssueKey, I.Description, I.Status, ISNULL(I.FirstSeenUTC, @RunUTC), @RunUTC
	FROM @Issues I
	WHERE NOT EXISTS(SELECT 1 FROM dbo.DailyReportIssueState S WHERE S.DailyReportID = @DailyReportID AND S.IssueKey = I.IssueKey)

	/* A run with no issues leaves no rows, so the report itself records that it has a baseline */
	UPDATE dbo.DailyReport
	SET IssueStateInitialized = 1
	WHERE DailyReportID = @DailyReportID
	AND IssueStateInitialized = 0

	COMMIT
END
