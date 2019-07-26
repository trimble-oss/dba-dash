CREATE TYPE [dbo].[CPU] AS TABLE (
    [EventTime]     DATETIME NOT NULL,
    [SQLProcessCPU] INT      NOT NULL,
    [SystemIdleCPU] INT      NOT NULL);

