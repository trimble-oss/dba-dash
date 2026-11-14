/*
	The issues each daily report listed on its last scheduled run, so the next run can say which are new, which are ongoing (and since
	when) and which have been resolved.  An issue that goes away is removed, so if it comes back it's new again.
*/
CREATE TABLE dbo.DailyReportIssueState(
	DailyReportID INT NOT NULL CONSTRAINT FK_DailyReportIssueState_DailyReport REFERENCES dbo.DailyReport(DailyReportID),
	IssueKey NVARCHAR(400) NOT NULL,
	/* For the "Resolved since the last report" list, once the issue is no longer there to describe itself */
	Description NVARCHAR(400) NULL,
	/* 1 critical, 2 warning at the last run, so the next one can tell an issue that got worse or better.  NULL for performance findings */
	Status TINYINT NULL,
	FirstSeenUTC DATETIME2(3) NOT NULL,
	LastSeenUTC DATETIME2(3) NOT NULL,
	CONSTRAINT PK_DailyReportIssueState PRIMARY KEY(DailyReportID, IssueKey)
);
