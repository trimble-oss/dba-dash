/*	Audit log of changes users make from the GUI - who did what, to which instance, and the outcome.  One row per
	instance affected, or one row with no instance for a change that isn't to a single instance (e.g. a service's
	schedule, or a repository setting).

	For an action sent to a DBA Dash service by messaging, the row is written before the message is sent and updated
	with the outcome when the reply arrives, matched on MessageGroupID - the same ID the service logs, so the two can be
	tied together.  An action made directly in the repository has no message and is logged with its outcome.

	Action specific data is held as JSON in details, so a new kind of action doesn't need new columns. */
CREATE TABLE dbo.UserActionLog(
	LogID BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UserActionLog PRIMARY KEY NONCLUSTERED,
	log_date DATETIME2(7) NOT NULL CONSTRAINT DF_UserActionLog_log_date DEFAULT(SYSUTCDATETIME()),
	user_name NVARCHAR(256) NOT NULL, /* Windows user running the GUI */
	login_name NVARCHAR(128) NOT NULL CONSTRAINT DF_UserActionLog_login_name DEFAULT(SUSER_SNAME()), /* Login the GUI used to connect to the repository */
	action_type VARCHAR(50) NOT NULL,
	InstanceID INT NULL,
	CollectAgentID INT NULL, /* The service the action was sent to, for an action made by messaging */
	database_name NVARCHAR(128) NULL,
	details NVARCHAR(MAX) NULL CONSTRAINT CK_UserActionLog_details CHECK(details IS NULL OR ISJSON(details) = 1),
	status NVARCHAR(MAX) NULL,
	MessageGroupID UNIQUEIDENTIFIER NULL, /* NULL for an action made directly in the repository */
	INDEX IX_UserActionLog_log_date CLUSTERED(log_date),
	INDEX IX_UserActionLog_InstanceID_log_date NONCLUSTERED(InstanceID,log_date),
	INDEX IX_UserActionLog_MessageGroupID NONCLUSTERED(MessageGroupID) WHERE MessageGroupID IS NOT NULL,
	CONSTRAINT FK_UserActionLog_Instances FOREIGN KEY(InstanceID) REFERENCES dbo.Instances(InstanceID),
	CONSTRAINT FK_UserActionLog_DBADashAgent FOREIGN KEY(CollectAgentID) REFERENCES dbo.DBADashAgent(DBADashAgentID)
)
