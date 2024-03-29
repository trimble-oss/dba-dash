﻿CREATE TABLE [dbo].[DDL] (
    [DDLID]   BIGINT          IDENTITY (1, 1) NOT NULL,
    [DDLHash] BINARY (32) NOT NULL,
    [DDL]     VARBINARY (MAX) NULL,
    CONSTRAINT PK_DDL PRIMARY KEY CLUSTERED ([DDLID] ASC)
);
GO
CREATE UNIQUE NONCLUSTERED INDEX IX_DDL_DDLHash ON dbo.DDL(DDLHash)