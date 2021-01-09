CREATE TABLE [dbo].[LastGoodCheckDBThresholds] (
    [InstanceID]           INT NOT NULL,
    [DatabaseID]           INT NOT NULL,
    [WarningThresholdHrs]  INT NULL,
    [CriticalThresholdHrs] INT NULL,
    CONSTRAINT [PK_LastGoodCheckDBThresholds] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [DatabaseID] ASC)
);

