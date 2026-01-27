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
    InstanceID          INT            NOT NULL CONSTRAINT DF_DBFiles_InstanceID DEFAULT (-1),
    CONSTRAINT [PK_DBFiles] PRIMARY KEY NONCLUSTERED ([FileID] ASC),
    CONSTRAINT [FK_DBFiles_Databases] FOREIGN KEY ([DatabaseID]) REFERENCES [dbo].[Databases] ([DatabaseID])
);
GO
CREATE UNIQUE NONCLUSTERED INDEX IX_DBFiles_DatabaseID_file_id ON dbo.DBFiles(DatabaseID,file_id) INCLUDE(IsActive);
GO
CREATE UNIQUE CLUSTERED INDEX IX_DBFiles_InstanceID_DatabaseID_file_id ON dbo.DBFiles(InstanceID,DatabaseID,file_id);




