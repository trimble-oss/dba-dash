CREATE TYPE [dbo].[Waits] AS TABLE (
    [wait_type]           NVARCHAR (60) NOT NULL,
    [waiting_tasks_count] BIGINT        NOT NULL,
    [wait_time_ms]        BIGINT        NOT NULL,
    [signal_wait_time_ms] BIGINT        NOT NULL);

