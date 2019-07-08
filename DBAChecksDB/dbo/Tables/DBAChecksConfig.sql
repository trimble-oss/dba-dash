CREATE TABLE [dbo].[DBAChecksConfig] (
    [ID]                     TINYINT        NOT NULL,
    [DriveWarningThreshold]  DECIMAL (9, 3) NULL,
    [DriveCriticalThreshold] DECIMAL (9, 3) NULL,
    [DriveCheckType]         CHAR (1)       NULL,
    CONSTRAINT [PK_DBAChecksConfig] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [CK_DBAChecksConfig] CHECK ([ID]=(1))
);

