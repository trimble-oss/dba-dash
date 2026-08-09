CREATE TABLE dbo.FlushPlanLog(
	MessageGroupID UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_FlushPlanLog PRIMARY KEY NONCLUSTERED,
	InstanceID INT NOT NULL,
	session_id INT NOT NULL,
	SnapshotDate DATETIME2(7) NULL, /* Links back to the RunningQueries snapshot (InstanceID + SnapshotDate + session_id).  Session detail is joined from dbo.RunningQueries rather than duplicated here. */
	plan_handle VARCHAR(130) NULL,
	flushed_by NVARCHAR(256) NOT NULL,
	log_date DATETIME2(7) NOT NULL CONSTRAINT DF_FlushPlanLog_log_date DEFAULT(SYSUTCDATETIME()),
	status VARCHAR(200) NULL CONSTRAINT DF_FlushPlanLog_Status DEFAULT('REQUEST'),
	CONSTRAINT UX_FlushPlanLog_InstanceID_log_date_session_id UNIQUE CLUSTERED(InstanceID,log_date,session_id),
	CONSTRAINT FK_FlushPlanLog_Instances FOREIGN KEY(InstanceID) REFERENCES dbo.Instances(InstanceID)
)
