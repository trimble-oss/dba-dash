CREATE TABLE [dbo].[DataRetention] (
    [TableName]     [sysname] NOT NULL,
    [RetentionDays] INT       NULL,
    CONSTRAINT [PK_DataRetention] PRIMARY KEY CLUSTERED ([TableName] ASC)
);

