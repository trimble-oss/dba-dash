/* The issues in a daily report run, for dbo.DailyReport_IssueState_Upd.  IssueKey identifies an issue across runs - see DBADash.DailyReport.IssueHistory */
CREATE TYPE dbo.DailyReportIssues AS TABLE(
	IssueKey NVARCHAR(400) NOT NULL PRIMARY KEY,
	Description NVARCHAR(400) NULL,
	/* 1 critical, 2 warning (alerts: 10 Critical, 11 High, 12 Medium, 13 Low).  Lower is worse.  NULL when the issue has no severity (performance findings) */
	Status TINYINT NULL,
	/* When the issue was first seen, if it's known to be older than this run - an issue now described differently (instance-level vs object-level) keeps its age */
	FirstSeenUTC DATETIME2(3) NULL
);
