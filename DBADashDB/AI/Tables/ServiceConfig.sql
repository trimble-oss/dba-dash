CREATE TABLE AI.ServiceConfig (
    ConfigId INT NOT NULL,
    ServiceUrl NVARCHAR(500) NOT NULL,
    ApiKey NVARCHAR(256) MASKED WITH (FUNCTION = 'default()') NOT NULL,
    IsEnabled BIT NOT NULL CONSTRAINT DF_AI_ServiceConfig_IsEnabled DEFAULT (1),
    LastHeartbeat DATETIME2(3) NULL,
    ServiceVersion NVARCHAR(50) NULL,
    PreviousApiKey NVARCHAR(256) MASKED WITH (FUNCTION = 'default()') NULL,
    PreviousApiKeyExpiryUtc DATETIME2(3) NULL,
    ApiKeyCreatedDate DATETIME2(3) NOT NULL CONSTRAINT DF_AI_ServiceConfig_ApiKeyCreatedDate DEFAULT (SYSDATETIME()),
    ApiKeyCreatedBy NVARCHAR(128) NOT NULL CONSTRAINT DF_AI_ServiceConfig_ApiKeyCreatedBy DEFAULT (SUSER_SNAME()),
    CreatedDate DATETIME2(3) NOT NULL CONSTRAINT DF_AI_ServiceConfig_CreatedDate DEFAULT (SYSDATETIME()),
    LastModifiedDate DATETIME2(3) NOT NULL CONSTRAINT DF_AI_ServiceConfig_LastModifiedDate DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_AI_ServiceConfig PRIMARY KEY CLUSTERED (ConfigId ASC),
    CONSTRAINT CK_AI_ServiceConfig_SingleRow CHECK (ConfigId = 1)
);
GO
