CREATE TYPE [dbo].[Corruption] AS TABLE (
    [SourceTable]      TINYINT  NOT NULL,
    [database_id]      INT      NOT NULL,
    [last_update_date] DATETIME NOT NULL);

