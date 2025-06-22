CREATE TABLE dbo.DBADashAgent(
	DBADashAgentID INT IDENTITY(1,1) NOT NULL,
	AgentHostName NVARCHAR(16) NOT NULL,
	AgentServiceName NVARCHAR(256) NOT NULL,
	AgentVersion VARCHAR(30) NOT NULL,
	AgentPath NVARCHAR(260) NOT NULL,
	MessagingEnabled BIT NOT NULL CONSTRAINT DF_DBADashAgent_MessagingEnabled DEFAULT(0),
	ServiceSQSQueueUrl NVARCHAR(256) NULL,
	AgentIdentifier CHAR(22) NOT NULL CONSTRAINT DF_DBADashAgent_AgentIdentifier DEFAULT (left(concat('Temp.',replace(newid(),'-','')),(22))),
	S3Path NVARCHAR(256) NULL,
	AllowedScripts VARCHAR(MAX) NULL,
	AllowedCustomProcs NVARCHAR(MAX) NULL,
	CONSTRAINT PK_DBADashAgent PRIMARY KEY(DBADashAgentID),
	INDEX IX_DBADashAgent UNIQUE NONCLUSTERED (AgentHostName,AgentServiceName),
	INDEX IX_DBADashAgent_AgentIdentifier UNIQUE NONCLUSTERED (AgentIdentifier)
)
