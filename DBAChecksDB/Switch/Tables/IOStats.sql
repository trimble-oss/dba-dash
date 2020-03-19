CREATE TABLE [Switch].[IOStats] (
    [InstanceID]           INT           NOT NULL,
    [FileID]               INT           NULL,
    [database_id]          SMALLINT      NOT NULL,
    [file_id]              SMALLINT      NOT NULL,
    [SnapshotDate]         DATETIME2 (2) NOT NULL,
    [sample_ms_diff]       BIGINT        NOT NULL,
    [num_of_reads]         BIGINT        NOT NULL,
    [num_of_bytes_read]    BIGINT        NOT NULL,
    [io_stall_read_ms]     BIGINT        NOT NULL,
    [num_of_writes]        BIGINT        NOT NULL,
    [num_of_bytes_written] BIGINT        NOT NULL,
    [io_stall_write_ms]    BIGINT        NOT NULL,
    [io_stall]             BIGINT        NOT NULL,
    [size_on_disk_bytes]   BIGINT        NOT NULL,
    CONSTRAINT [PK_IOStats] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [SnapshotDate] ASC, [database_id] ASC, [file_id] ASC) WITH (DATA_COMPRESSION = PAGE)
);

