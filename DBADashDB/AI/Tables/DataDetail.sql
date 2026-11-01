CREATE TABLE AI.DataDetail (
    -- Single row, like AI.ServiceConfig.  Missing row means parametersAndLiterals.
    ConfigId INT NOT NULL,
    -- The least the built-in AI masks before sending anything to its provider: a request can ask for more, never less.
    -- The same words as DBADashAI.Tools AiDataMask.
    DataDetail NVARCHAR(30) NOT NULL CONSTRAINT DF_AI_DataDetail_DataDetail DEFAULT ('parametersAndLiterals'),
    LastModifiedDate DATETIME2(3) NOT NULL CONSTRAINT DF_AI_DataDetail_LastModifiedDate DEFAULT (SYSDATETIME()),
    LastModifiedBy NVARCHAR(128) NOT NULL CONSTRAINT DF_AI_DataDetail_LastModifiedBy DEFAULT (SUSER_SNAME()),
    CONSTRAINT PK_AI_DataDetail PRIMARY KEY CLUSTERED (ConfigId ASC),
    CONSTRAINT CK_AI_DataDetail_SingleRow CHECK (ConfigId = 1),
    CONSTRAINT CK_AI_DataDetail_Value CHECK (DataDetail IN ('full', 'parameters', 'literals', 'parametersAndLiterals', 'metadataOnly'))
);
GO
