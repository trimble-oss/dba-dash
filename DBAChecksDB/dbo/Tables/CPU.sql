CREATE TABLE [dbo].[CPU] (
    [InstanceID]    INT           NOT NULL,
    [EventTime]     DATETIME2 (3) NOT NULL,
    [SQLProcessCPU] TINYINT       NOT NULL,
    [SystemIdleCPU] TINYINT       NOT NULL,
    [OtherCPU]      AS            ((100)-([SQLProcessCPU]+[SystemIdleCPU])),
    [TotalCPU]      AS            ((100)-[SystemIdleCPU]),
    CONSTRAINT [PK_CPU] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [EventTime] ASC) WITH (DATA_COMPRESSION = PAGE) ON [PS_CPU] ([EventTime]),
    CONSTRAINT [FK_CPU_Instances] FOREIGN KEY ([InstanceID]) REFERENCES [dbo].[Instances] ([InstanceID])
) ON [PS_CPU] ([EventTime]);





