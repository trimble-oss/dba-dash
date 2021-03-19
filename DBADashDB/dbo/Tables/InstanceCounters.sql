CREATE TABLE [dbo].[InstanceCounters] (
    [InstanceID]  INT           NOT NULL,
    [CounterID]   INT           NOT NULL,
    [CreatedDate] DATETIME2 (7) CONSTRAINT [DF_InstanceCounters_CreatedDate] DEFAULT (sysutcdatetime()) NOT NULL,
    [UpdatedDate] DATETIME2 (7) CONSTRAINT [DF_InstanceCounters_UpdatedDate] DEFAULT (sysutcdatetime()) NOT NULL,
    CONSTRAINT [PK_InstanceCounters] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [CounterID] ASC),
    CONSTRAINT [FK_InstanceCounters_Counters] FOREIGN KEY ([CounterID]) REFERENCES [dbo].[Counters] ([CounterID]),
    CONSTRAINT [FK_InstanceCounters_Instances] FOREIGN KEY ([InstanceID]) REFERENCES [dbo].[Instances] ([InstanceID])
);



