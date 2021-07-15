CREATE TABLE [dbo].[CPU_60MIN] (
    [InstanceID]         INT           NOT NULL,
    [EventTime]          DATETIME2 (3) NOT NULL,
    [SumSQLProcessCPU]   INT           NOT NULL,
    [SumSystemIdleCPU]   INT           NOT NULL,
    [SampleCount]        SMALLINT      NOT NULL,
    [MaxSQLProcessCPU]   TINYINT       NOT NULL,
    [MaxOtherProcessCPU] TINYINT       NOT NULL,
    [MaxTotalCPU]        TINYINT       NOT NULL,
    [SumOtherCPU]        AS            ([SampleCount]*(100)-([SumSQLProcessCPU]+[SumSystemIdleCPU])),
    [SumTotalCPU]        AS            ([SampleCount]*(100)-[SumSystemIdleCPU]),
    [CPU10]              SMALLINT      NULL,
    [CPU20]              SMALLINT      NULL,
    [CPU30]              SMALLINT      NULL,
    [CPU40]              SMALLINT      NULL,
    [CPU50]              SMALLINT      NULL,
    [CPU60]              SMALLINT      NULL,
    [CPU70]              SMALLINT      NULL,
    [CPU80]              SMALLINT      NULL,
    [CPU90]              SMALLINT      NULL,
    [CPU100]             SMALLINT      NULL,
    CONSTRAINT [PK_CPU_60MIN] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [EventTime] ASC) WITH (DATA_COMPRESSION = PAGE) ON PS_CPU_60MIN(EventTime)
) ON PS_CPU_60MIN(EventTime);

