CREATE TABLE [dbo].[ScheduleInfo] (
    [InstanceID]              INT             NOT NULL,
    [Reference]               VARCHAR (100)   NOT NULL,
    [Schedule]                VARCHAR (100)   NOT NULL,
    [RunOnServiceStart]       BIT             NOT NULL,
    [MaxIntervalMinutes]      DECIMAL (18, 2) NULL,
    [IsInstanceOverride]      BIT             NOT NULL,
    [SnapshotDate]            DATETIME2 (2)   NOT NULL,
    [IsEnabled] AS (CASE WHEN [Schedule] = '' THEN CONVERT(BIT,0) ELSE CONVERT(BIT,1) END),
    CONSTRAINT [PK_ScheduleInfo] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [Reference] ASC),
    CONSTRAINT [FK_ScheduleInfo_Instances] FOREIGN KEY ([InstanceID]) REFERENCES [dbo].[Instances] ([InstanceID])
);
