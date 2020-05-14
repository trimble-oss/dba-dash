CREATE TYPE [dbo].[LastGoodCheckDB] AS TABLE (
    [database_id]         INT           NOT NULL,
    [LastGoodCheckDbTime] DATETIME2 (3) NULL,
    PRIMARY KEY CLUSTERED ([database_id] ASC));

