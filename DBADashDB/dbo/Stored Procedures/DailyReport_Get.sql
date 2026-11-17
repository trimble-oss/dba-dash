CREATE PROC dbo.DailyReport_Get(
	@DailyReportID INT=NULL
)
AS
SET NOCOUNT ON

SELECT	R.DailyReportID,
		R.ReportName,
		R.IsActive,
		CAST(R.ScheduleTime AS TIME(0)) AS ScheduleTime,
		R.TimeZone,
		R.DaysOfWeek,
		R.IntervalMinutes,
		CAST(R.WindowEndTime AS TIME(0)) AS WindowEndTime,
		R.ReportType,
		R.ApplyToTagID,
		R.ApplyToTagIDs,
		CASE	WHEN R.ApplyToTagID = -1 THEN '{All}'
				WHEN R.ApplyToTagID = -2 THEN ISNULL(TS.TagText,'{All}')
				ELSE ISNULL(T.TagName + ':' + T.TagValue,'{Deleted tag}')
		END AS ScopeDescription,
		R.IncludePerformance,
		R.IncludeWeekOverWeek,
		R.ChangeThresholdPct,
		R.Settings,
		R.LastRunUTC,
		R.LastRunError,
		R.LastSentUTC,
		R.LastSendAttemptUTC,
		R.LastSendResult,
		R.CreatedDate,
		R.EmailServerID,
		ES.EmailServerName,
		R.Recipients
FROM dbo.DailyReport R
LEFT JOIN dbo.Tags T ON T.TagID = R.ApplyToTagID
LEFT JOIN Alert.EmailServer ES ON ES.EmailServerID = R.EmailServerID
OUTER APPLY Alert.TagScope_Describe(R.ApplyToTagIDs) TS
WHERE @DailyReportID IS NULL OR R.DailyReportID = @DailyReportID
ORDER BY R.ReportName
