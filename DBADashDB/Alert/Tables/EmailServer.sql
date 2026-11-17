CREATE TABLE Alert.EmailServer(
	EmailServerID INT IDENTITY(1,1) NOT NULL,
	EmailServerName NVARCHAR(100) NOT NULL,
	ServerDetails NVARCHAR(MAX) MASKED WITH(FUNCTION = 'default()') NOT NULL,
	CONSTRAINT PK_EmailServer PRIMARY KEY(EmailServerID),
	CONSTRAINT UQ_EmailServer_EmailServerName UNIQUE(EmailServerName)
)
