CREATE TABLE [dbo].[SnapshotDates] (
    [InstanceID]           INT           NOT NULL,
    [AgentJobsDate]        DATETIME2 (2) NULL,
    [BackupsDate]          DATETIME2 (2) NULL,
    [DatabasesDate]        DATETIME2 (2) NULL,
    [DrivesDate]           DATETIME2 (2) NULL,
    [LogRestoresDate]      DATETIME2 (2) NULL,
    [DBFilesDate]          DATETIME2 (2) NULL,
    [ServerPropertiesDate] DATETIME2 (2) NULL,
    [InstanceDate]         DATETIME2 (2) NULL,
    [DBConfigDate]         DATETIME2 (2) NULL,
    CONSTRAINT [PK_SnapshotDates] PRIMARY KEY CLUSTERED ([InstanceID] ASC),
    CONSTRAINT [FK_SnapshotDates] FOREIGN KEY ([InstanceID]) REFERENCES [dbo].[Instances] ([InstanceID])
);



