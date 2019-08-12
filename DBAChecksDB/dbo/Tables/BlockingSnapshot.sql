CREATE TABLE [dbo].[BlockingSnapshot] (
    [BlockingSnapshotID]  INT            NOT NULL,
    [session_id]          SMALLINT       NOT NULL,
    [blocking_session_id] SMALLINT       NOT NULL,
    [Txt]                 NVARCHAR (MAX) NULL,
    [start_time_utc]      DATETIME2(3)       NULL,
    [command]             NVARCHAR (32)  NULL,
    [database_id]         SMALLINT       NULL,
    [database_name]       NVARCHAR (128) NULL,
    [host_name]           NVARCHAR (128) NULL,
    [program_name]        NVARCHAR (128) NULL,
    [wait_time]           INT            NULL,
    [login_name]          NVARCHAR (128) NULL,
    [wait_resource]       NVARCHAR (256) NULL,
    [Status]              NVARCHAR (30)  NULL,
    [wait_type]           NVARCHAR (60)  NULL,
    CONSTRAINT [PK_BlockingSnapshot] PRIMARY KEY CLUSTERED ([BlockingSnapshotID] ASC, [session_id] ASC),
    CONSTRAINT [FK_BlockingSnapshot_BlockingSnapshotSummary] FOREIGN KEY ([BlockingSnapshotID]) REFERENCES [dbo].[BlockingSnapshotSummary] ([BlockingSnapshotID])
);





