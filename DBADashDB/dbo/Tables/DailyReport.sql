CREATE TABLE dbo.DailyReport(
	DailyReportID INT IDENTITY(1,1) CONSTRAINT PK_DailyReport PRIMARY KEY,
	ReportName NVARCHAR(100) NOT NULL CONSTRAINT UQ_DailyReport_ReportName UNIQUE,
	IsActive BIT NOT NULL CONSTRAINT DF_DailyReport_IsActive DEFAULT(1),
	/* Time of day the report is sent, in TimeZone */
	ScheduleTime TIME(0) NOT NULL CONSTRAINT DF_DailyReport_ScheduleTime DEFAULT('08:00'),
	TimeZone NVARCHAR(64) NOT NULL CONSTRAINT DF_DailyReport_TimeZone DEFAULT('UTC'),
	/* Days the report is sent, in TimeZone: a bit per day - Monday 1, Tuesday 2, Wednesday 4, Thursday 8, Friday 16, Saturday 32, Sunday 64.  127 is every day */
	DaysOfWeek TINYINT NOT NULL CONSTRAINT DF_DailyReport_DaysOfWeek DEFAULT(127),
	/* NULL: once a day at ScheduleTime.  Otherwise every IntervalMinutes from ScheduleTime until WindowEndTime (NULL: the end of the day) */
	IntervalMinutes INT NULL,
	WindowEndTime TIME(0) NULL,
	/* 0: the full report.  1: changes only - issues that are new, worse, better or resolved since its last send, and nothing is sent when there are none */
	ReportType TINYINT NOT NULL CONSTRAINT DF_DailyReport_ReportType DEFAULT(0),
	/* Instances in scope.  Same convention as alert rules: -1 all instances, >0 single tag, -2 multiple tags listed in ApplyToTagIDs */
	ApplyToTagID INT NOT NULL CONSTRAINT DF_DailyReport_ApplyToTagID DEFAULT(-1),
	ApplyToTagIDs VARCHAR(800) NULL,
	IncludePerformance BIT NOT NULL CONSTRAINT DF_DailyReport_IncludePerformance DEFAULT(0),
	IncludeWeekOverWeek BIT NOT NULL CONSTRAINT DF_DailyReport_IncludeWeekOverWeek DEFAULT(0),
	/* Relative change vs the same time last week that is reported */
	ChangeThresholdPct INT NOT NULL CONSTRAINT DF_DailyReport_ChangeThresholdPct DEFAULT(50),
	/* Everything else, as JSON - see DBADash.DailyReport.DailyReportSettings.  NULL takes the defaults */
	Settings NVARCHAR(MAX) NULL,
	/* Set once a scheduled run has recorded its issues (dbo.DailyReportIssueState), so later runs can mark issues as new */
	IssueStateInitialized BIT NOT NULL CONSTRAINT DF_DailyReport_IssueStateInitialized DEFAULT(0),
	/* Set when the service claims the report for a run - once per scheduled time (see dbo.DailyReport_Claim) */
	LastRunUTC DATETIME2(3) NULL,
	LastRunError NVARCHAR(MAX) NULL,
	/* Set by dbo.DailyReport_Claim while a run is in progress, cleared by dbo.DailyReport_RunResult_Upd, so a repeating report isn't claimed for its
	   next slot (by another service) while the last run is still going.  A run that never finishes stops blocking after an hour */
	RunningSinceUTC DATETIME2(3) NULL,
	/* The last send, scheduled or Send now: when it reached at least one channel, and each channel's outcome (the mail server's reply, or why it wasn't sent) */
	LastSentUTC DATETIME2(3) NULL,
	LastSendResult NVARCHAR(MAX) NULL,
	LastSendAttemptUTC DATETIME2(3) NULL,
	/* Set when the last run sent nothing, so dbo.DailyReport_Claim tries again later the same day */
	RetryDue BIT NOT NULL CONSTRAINT DF_DailyReport_RetryDue DEFAULT(0),
	/* Retries of today's run so far */
	RetryAttempts TINYINT NOT NULL CONSTRAINT DF_DailyReport_RetryAttempts DEFAULT(0),
	CreatedDate DATETIME2(3) NOT NULL CONSTRAINT DF_DailyReport_CreatedDate DEFAULT(SYSUTCDATETIME())
);
