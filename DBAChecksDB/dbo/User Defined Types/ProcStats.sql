CREATE TYPE [dbo].[ProcStats] AS TABLE (
    [object_id]            INT            NOT NULL,
    [database_id]          INT            NOT NULL,
    [object_name]          NVARCHAR (128) NULL,
    [total_worker_time]    BIGINT         NOT NULL,
    [total_elapsed_time]   BIGINT         NOT NULL,
    [total_logical_reads]  BIGINT         NOT NULL,
    [total_logical_writes] BIGINT         NOT NULL,
    [total_physical_reads] BIGINT         NOT NULL,
    [cached_time]          DATETIME       NULL,
    [execution_count]      BIGINT         NOT NULL,
    [current_time_utc]     DATETIME2(3)   NOT NULL,
    [type]                 CHAR(2)        NULL,
    schema_name            NVARCHAR(128)  NULL
);

