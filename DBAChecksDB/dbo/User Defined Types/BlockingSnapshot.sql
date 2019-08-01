CREATE TYPE [dbo].[BlockingSnapshot] AS TABLE (
    [SnapshotDateUTC]     DATETIME       NOT NULL,
    [UTCOffset]           INT            NOT NULL,
    [session_id]          SMALLINT       NOT NULL,
    [blocking_session_id] SMALLINT       NULL,
    [Txt]                 NVARCHAR (MAX) NULL,
    [start_time_utc]      DATETIME       NULL,
    [command]             NVARCHAR (32)  NULL,
    [database_id]         SMALLINT       NULL,
    [database_name]       NVARCHAR (128) NULL,
    [host_name]           NVARCHAR (128) NULL,
    [program_name]        NVARCHAR (128) NULL,
    [wait_time]           INT            NULL,
    [login_name]          NVARCHAR (128) NULL,
    [wait_resource]       NVARCHAR (256) NULL,
    [Status]              NVARCHAR (30)  NULL);

