CREATE TYPE [dbo].[CPU] AS TABLE (
    [EventTime]     DATETIME2(3) NOT NULL,
    [SQLProcessCPU] INT      NOT NULL,
    [SystemIdleCPU] INT      NOT NULL);

