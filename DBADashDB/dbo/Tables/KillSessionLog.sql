CREATE TABLE dbo.KillSessionLog(
	MessageGroupID UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_KillSessionLog PRIMARY KEY NONCLUSTERED,
	InstanceID INT NOT NULL,
	session_id INT NOT NULL,
	SnapshotDate DATETIME2(7) NULL, /* Links back to the RunningQueries snapshot (InstanceID + SnapshotDate + session_id).  Session detail is joined from dbo.RunningQueries rather than duplicated here. */
	killed_by NVARCHAR(256) NOT NULL,
	log_date DATETIME2(7) NOT NULL CONSTRAINT DF_KillSessionLog_log_date DEFAULT(SYSUTCDATETIME()),
	status VARCHAR(200) NULL CONSTRAINT DF_KillSessionLog_Status DEFAULT('REQUEST'),
	CONSTRAINT UX_KillSessionLog_InstanceID_log_date_session_id UNIQUE CLUSTERED(InstanceID,log_date,session_id),
	CONSTRAINT FK_KillSessionLog_Instances FOREIGN KEY(InstanceID) REFERENCES dbo.Instances(InstanceID)
)
