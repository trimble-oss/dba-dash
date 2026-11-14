CREATE PROC dbo.DailyReport_Upd(
	@DailyReportID INT=NULL OUTPUT, /* NULL to add */
	@ReportName NVARCHAR(100),
	@IsActive BIT,
	@ScheduleTime TIME(0),
	@TimeZone NVARCHAR(64),
	@DaysOfWeek TINYINT=127,
	@IntervalMinutes INT=NULL,
	@WindowEndTime TIME(0)=NULL,
	@ReportType TINYINT=0,
	@ApplyToTagID INT,
	@ApplyToTagIDs VARCHAR(800)=NULL,
	@IncludePerformance BIT,
	@IncludeWeekOverWeek BIT,
	@ChangeThresholdPct INT,
	@Settings NVARCHAR(MAX)=NULL,
	@NotificationChannelIDs dbo.IDs READONLY
)
AS
SET NOCOUNT ON
SET XACT_ABORT ON

IF NULLIF(LTRIM(RTRIM(@ReportName)),'') IS NULL
	THROW 50000, 'Daily report name is required.', 1;
IF EXISTS(SELECT 1 FROM dbo.DailyReport WHERE ReportName = @ReportName AND DailyReportID <> ISNULL(@DailyReportID,-1))
	THROW 50000, 'A daily report with this name already exists.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.time_zone_info WHERE name = @TimeZone)
	THROW 50000, 'Unknown time zone for daily report.', 1;
IF @ApplyToTagID NOT IN (-1,-2) AND NOT EXISTS(SELECT 1 FROM dbo.Tags WHERE TagID = @ApplyToTagID)
	THROW 50000, 'The tag in the daily report scope does not exist.', 1;
IF @DaysOfWeek < 1 OR @DaysOfWeek > 127
	THROW 50000, 'Select at least one day of the week for the daily report.', 1;
IF @IntervalMinutes IS NOT NULL AND (@IntervalMinutes < 15 OR @IntervalMinutes > 720)
	THROW 50000, 'The daily report interval must be between 15 minutes and 12 hours.', 1;
IF @IntervalMinutes IS NULL
	SET @WindowEndTime = NULL; /* Only a repeating report has an end time */
IF @WindowEndTime < @ScheduleTime
	THROW 50000, 'The daily report end time must be after its start time.', 1;
IF @ReportType NOT IN (0,1)
	THROW 50000, 'Unknown daily report type.', 1;
IF @Settings IS NOT NULL AND ISJSON(@Settings) = 0
	THROW 50000, 'Daily report settings are not valid JSON.', 1;
IF @ChangeThresholdPct < 1 OR @ChangeThresholdPct > 1000
	THROW 50000, 'Change threshold must be between 1 and 1000 percent.', 1;
IF NOT EXISTS(SELECT 1 FROM @NotificationChannelIDs)
	THROW 50000, 'Select at least one notification channel for the daily report.', 1;

BEGIN TRAN
IF @DailyReportID IS NULL
BEGIN
	INSERT INTO dbo.DailyReport(ReportName,IsActive,ScheduleTime,TimeZone,DaysOfWeek,IntervalMinutes,WindowEndTime,ReportType,ApplyToTagID,ApplyToTagIDs,IncludePerformance,IncludeWeekOverWeek,ChangeThresholdPct,Settings)
	VALUES(@ReportName,@IsActive,@ScheduleTime,@TimeZone,@DaysOfWeek,@IntervalMinutes,@WindowEndTime,@ReportType,@ApplyToTagID,@ApplyToTagIDs,@IncludePerformance,@IncludeWeekOverWeek,@ChangeThresholdPct,@Settings);
	SET @DailyReportID = SCOPE_IDENTITY();
END
ELSE
BEGIN
	UPDATE dbo.DailyReport
	SET ReportName = @ReportName,
		IsActive = @IsActive,
		ScheduleTime = @ScheduleTime,
		TimeZone = @TimeZone,
		DaysOfWeek = @DaysOfWeek,
		IntervalMinutes = @IntervalMinutes,
		WindowEndTime = @WindowEndTime,
		ReportType = @ReportType,
		ApplyToTagID = @ApplyToTagID,
		ApplyToTagIDs = @ApplyToTagIDs,
		IncludePerformance = @IncludePerformance,
		IncludeWeekOverWeek = @IncludeWeekOverWeek,
		ChangeThresholdPct = @ChangeThresholdPct,
		Settings = @Settings
	WHERE DailyReportID = @DailyReportID;
	IF @@ROWCOUNT = 0
		THROW 50000, 'Daily report not found.', 1;
END

DELETE FROM dbo.DailyReportChannel
WHERE DailyReportID = @DailyReportID
AND NotificationChannelID NOT IN (SELECT ID FROM @NotificationChannelIDs);

INSERT INTO dbo.DailyReportChannel(DailyReportID,NotificationChannelID)
SELECT @DailyReportID, C.ID
FROM @NotificationChannelIDs C
WHERE NOT EXISTS(SELECT 1 FROM dbo.DailyReportChannel RC WHERE RC.DailyReportID = @DailyReportID AND RC.NotificationChannelID = C.ID);
COMMIT
