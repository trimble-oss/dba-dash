CREATE TABLE [dbo].[AzureDBElasticPoolResourceStats] (
    [PoolID]                        INT            NOT NULL,
    [start_time]                    DATETIME2 (7)  NOT NULL,
    [end_time]                      DATETIME2 (7)  NOT NULL,
    [avg_cpu_percent]               DECIMAL (5, 2) NULL,
    [avg_data_io_percent]           DECIMAL (5, 2) NULL,
    [avg_log_write_percent]         DECIMAL (5, 2) NULL,
    [avg_storage_percent]           DECIMAL (5, 2) NULL,
    [max_worker_percent]            DECIMAL (5, 2) NULL,
    [max_session_percent]           DECIMAL (5, 2) NULL,
    [elastic_pool_dtu_limit]        INT            NULL,
    [elastic_pool_storage_limit_mb] BIGINT         NULL,
    [avg_allocated_storage_percent] DECIMAL (5, 2) NULL,
    [elastic_pool_cpu_limit]        DECIMAL (5, 2) NULL,
    CONSTRAINT [PK_AzureDBElasticPoolResourceStats] PRIMARY KEY CLUSTERED ([PoolID] ASC, [end_time] ASC) WITH (DATA_COMPRESSION = PAGE) ON [PS_AzureDBElasticPoolResourceStats] ([end_time]),
    CONSTRAINT [FK_AzureDBElasticPoolResourceStats_AzureDBElasticPool] FOREIGN KEY ([PoolID]) REFERENCES [dbo].[AzureDBElasticPool] ([PoolID])
) ON [PS_AzureDBElasticPoolResourceStats] ([end_time]);





