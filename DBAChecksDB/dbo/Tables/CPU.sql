CREATE TABLE [dbo].[CPU] (
    [InstanceID]    INT      NOT NULL,
    [EventTime]     DATETIME NOT NULL,
    [SQLProcessCPU] INT      NOT NULL,
    [SystemIdleCPU] INT      NOT NULL,
    CONSTRAINT [PK_CPU] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [EventTime] ASC) WITH (DATA_COMPRESSION = PAGE),
    CONSTRAINT [FK_CPU_Instances] FOREIGN KEY ([InstanceID]) REFERENCES [dbo].[Instances] ([InstanceID])
);

