CREATE TABLE [Switch].[AzureDBResourceStats] (
    [InstanceID]                  INT            NOT NULL,
    [end_time]                    DATETIME2 (3)  NOT NULL,
    [avg_cpu_percent]             DECIMAL (5, 2) NOT NULL,
    [avg_data_io_percent]         DECIMAL (5, 2) NOT NULL,
    [avg_log_write_percent]       DECIMAL (5, 2) NOT NULL,
    [avg_memory_usage_percent]    DECIMAL (5, 2) NOT NULL,
    [xtp_storage_percent]         DECIMAL (5, 2) NOT NULL,
    [max_worker_percent]          DECIMAL (5, 2) NOT NULL,
    [max_session_percent]         DECIMAL (5, 2) NOT NULL,
    [dtu_limit]                   INT            NULL,
    [avg_instance_cpu_percent]    DECIMAL (5, 2) NOT NULL,
    [avg_instance_memory_percent] DECIMAL (5, 2) NOT NULL,
    [cpu_limit]                   DECIMAL (5, 2) NULL,
    [replica_role]                INT            NOT NULL,
    CONSTRAINT [PK_AzureDBResourceStats] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [end_time] ASC) WITH (DATA_COMPRESSION = PAGE)
);

