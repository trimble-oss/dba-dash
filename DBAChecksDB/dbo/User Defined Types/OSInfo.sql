CREATE TYPE [dbo].[OSInfo] AS TABLE (
    [affinity_type]          INT      NULL,
    [cores_per_socket]       INT      NULL,
    [cpu_count]              INT      NULL,
    [hyperthread_ratio]      INT      NULL,
    [max_workers_count]      INT      NULL,
    [ms_ticks]               BIGINT   NULL,
    [numa_node_count]        INT      NULL,
    [os_priority_class]      INT      NULL,
    [physical_memory_kb]     BIGINT   NULL,
    [scheduler_count]        INT      NULL,
    [socket_count]           INT      NULL,
    [softnuma_configuration] INT      NULL,
    [sql_memory_model]       INT      NULL,
    [sqlserver_start_time]   DATETIME NULL,
    [UTCOffset]              INT      NULL);



