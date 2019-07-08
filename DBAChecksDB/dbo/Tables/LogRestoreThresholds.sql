CREATE TABLE [dbo].[LogRestoreThresholds] (
    [InstanceID]                     INT NOT NULL,
    [DatabaseID]                     INT NOT NULL,
    [LatencyWarningThreshold]        INT NULL,
    [LatencyCriticalThreshold]       INT NULL,
    [TimeSinceLastWarningThreshold]  INT NULL,
    [TimeSinceLastCriticalThreshold] INT NULL,
    CONSTRAINT [PK_LogRestoreThresholds] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [DatabaseID] ASC)
);

