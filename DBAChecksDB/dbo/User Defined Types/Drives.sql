CREATE TYPE [dbo].[Drives] AS TABLE (
    [Name]      NVARCHAR (256) NOT NULL,
    [Capacity]  BIGINT         NOT NULL,
    [FreeSpace] BIGINT         NOT NULL,
    [Label]     NVARCHAR (256) NULL,
    PRIMARY KEY CLUSTERED ([Name] ASC));

