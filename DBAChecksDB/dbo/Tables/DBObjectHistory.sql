CREATE TABLE [dbo].[DBObjectHistory] (
    [ObjectID]        BIGINT        NOT NULL,
    [DDLIDOld]        BIGINT        NOT NULL,
    [DDLIDNew]        BIGINT        NOT NULL,
    [ValidFrom]       DATETIME2 (3) NOT NULL,
    [ValidTo]         DATETIME2 (3) NOT NULL,
    [ObjectValidFrom] DATETIME2 (3) NULL,
    [ObjectValidTo]   DATETIME2 (3) NULL,
    CONSTRAINT [PK_DBObjectHistory] PRIMARY KEY CLUSTERED ([ObjectID] ASC, [ValidTo] ASC)
);

