/* A list of database names.  Shared by any procedure that takes database names, rather than a collection-specific type. */
CREATE TYPE dbo.DatabaseNames AS TABLE (
    database_name NVARCHAR(128) NOT NULL PRIMARY KEY
);
