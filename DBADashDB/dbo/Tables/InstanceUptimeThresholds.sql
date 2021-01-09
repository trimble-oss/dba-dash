CREATE TABLE [dbo].[InstanceUptimeThresholds] (
    [InstanceID]        INT NOT NULL,
    [WarningThreshold]  INT NULL,
    [CriticalThreshold] INT NULL,
    CONSTRAINT [PK_InstanceUptimeThresholds] PRIMARY KEY CLUSTERED ([InstanceID] ASC)
);

