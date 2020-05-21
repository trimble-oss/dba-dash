CREATE TABLE [Staging].[ProcStats] (
    [InstanceID]           INT            NOT NULL,
    [object_id]            INT            NOT NULL,
    [database_id]          INT            NOT NULL,
    [object_name]          NVARCHAR (128) NULL,
    [total_worker_time]    BIGINT         NOT NULL,
    [total_elapsed_time]   BIGINT         NOT NULL,
    [total_logical_reads]  BIGINT         NOT NULL,
    [total_logical_writes] BIGINT         NOT NULL,
    [total_physical_reads] BIGINT         NOT NULL,
    [cached_time]          DATETIME       NOT NULL,
    [execution_count]      BIGINT         NOT NULL,
    [current_time_utc]     DATETIME2(3)       NOT NULL,
    CONSTRAINT [PK_Staging_ProcStats] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [object_id] ASC, [database_id] ASC, [cached_time] ASC)
);

