CREATE TYPE [dbo].[DBFiles] AS TABLE (
    [database_id]       INT            NULL,
    [file_id]           INT            NULL,
    [data_space_id]     INT            NULL,
    [name]              [sysname]      NOT NULL,
    [filegroup_name]    [sysname]      NULL,
    [physical_name]     NVARCHAR (260) NULL,
    [type]              TINYINT        NULL,
    [size]              BIGINT         NULL,
    [space_used]        BIGINT         NULL,
    [max_size]          BIGINT         NULL,
    [growth]            BIGINT         NULL,
    [is_percent_growth] BIT            NULL,
    [is_read_only]      BIT            NULL);



