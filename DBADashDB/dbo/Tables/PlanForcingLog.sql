﻿CREATE TABLE dbo.PlanForcingLog(
	MessageGroupID UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_PlanForcingLog PRIMARY KEY NONCLUSTERED,
	InstanceID INT NOT NULL,
	database_name NVARCHAR(128) NOT NULL,
	log_date DATETIME2(7) NOT NULL CONSTRAINT DF_PlanForcingLog_log_date DEFAULT(SYSUTCDATETIME()),
	log_type VARCHAR(20) NOT NULL,
	user_name NVARCHAR(256) NOT NULL,
	query_id BIGINT NOT NULL,
	plan_id BIGINT NOT NULL,
	object_name NVARCHAR(128) NULL,
	query_sql_text NVARCHAR(MAX) NULL,
	query_hash BINARY(8) NOT NULL,
	query_plan_hash BINARY(8) NOT NULL,
	notes NVARCHAR(MAX) NULL,
	status VARCHAR(200) NULL CONSTRAINT DF_PlanForcingLog_Status DEFAULT('REQUEST'),
	CONSTRAINT UX_PlanForcingLog_InstanceID_log_date_plan_id UNIQUE CLUSTERED(InstanceID,log_date,plan_id),
	CONSTRAINT FK_PlanForcingLog_Instances FOREIGN KEY(InstanceID) REFERENCES dbo.Instances(InstanceID),
)