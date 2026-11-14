CREATE TABLE dbo.DailyReportChannel(
	DailyReportID INT NOT NULL CONSTRAINT FK_DailyReportChannel_DailyReport REFERENCES dbo.DailyReport(DailyReportID),
	NotificationChannelID INT NOT NULL CONSTRAINT FK_DailyReportChannel_NotificationChannel REFERENCES Alert.NotificationChannel(NotificationChannelID),
	CONSTRAINT PK_DailyReportChannel PRIMARY KEY(DailyReportID, NotificationChannelID)
);
