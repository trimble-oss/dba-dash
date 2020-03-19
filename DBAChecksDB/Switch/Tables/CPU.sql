CREATE TABLE [Switch].[CPU] (
    [InstanceID]    INT           NOT NULL,
    [EventTime]     DATETIME2 (3) NOT NULL,
    [SQLProcessCPU] INT           NOT NULL,
    [SystemIdleCPU] INT           NOT NULL,
    CONSTRAINT [PK_CPU] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [EventTime] ASC) WITH (DATA_COMPRESSION = PAGE)
);

