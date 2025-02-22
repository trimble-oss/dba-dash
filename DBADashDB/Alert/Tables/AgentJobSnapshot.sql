CREATE TABLE Alert.AgentJobSnapshot(
	InstanceID INT NOT NULL,
	job_id UNIQUEIDENTIFIER NOT  NULL,
	LastFailed DATETIME2 NULL,
	LastSucceeded DATETIME2 NULL,
	CONSTRAINT PK_Alert_AgentJobSnapshot PRIMARY KEY(InstanceID,job_id)
)