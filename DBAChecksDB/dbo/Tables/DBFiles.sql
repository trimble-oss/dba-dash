CREATE TABLE [dbo].[DBFiles] (
    [FileID]            INT            IDENTITY (1, 1) NOT NULL,
    [DatabaseID]        INT            NOT NULL,
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
    [is_read_only]      BIT            NULL,
    [IsActive]          BIT            NULL,
    [state]             TINYINT        NULL,
    CONSTRAINT [PK_DBFiles] PRIMARY KEY CLUSTERED ([FileID] ASC),
    CONSTRAINT [FK_DBFiles_Databases] FOREIGN KEY ([DatabaseID]) REFERENCES [dbo].[Databases] ([DatabaseID])
);
GO
CREATE NONCLUSTERED INDEX IX_DBFiles_DatabaseID_IsActive ON dbo.DBFiles(DatabaseID,IsActive) INCLUDE(file_id);





