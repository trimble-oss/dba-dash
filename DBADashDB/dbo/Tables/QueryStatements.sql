CREATE TABLE dbo.QueryStatements (
    StatementID BIGINT IDENTITY(1, 1) NOT NULL,
    InstanceID INT NOT NULL,
    /*
        Identity is per instance, and computed rather than composite.  Per instance because neither half of
        a module sql_handle means anything estate wide - it is built from the database id and the object id,
        both of which differ between instances hosting the same database.  Computed because the statement
        types are identified by different columns, and one BINARY(20) key turns a join per type into one
        equality.  See QueryStats_Upd for how it is built.
    */
    StatementKey BINARY(20) NOT NULL,
    /*
        5 = ad hoc query shape, identified by database and query_hash: every literal variant of a query is one
            statement.  Not by the handle: an ad hoc handle is a hash of the batch text, so each literal value
            makes a new one, and a query that sends its values as literals would be a new statement - and a
            new batch text - for every value it ran with.  Its handle and offsets are one variant's, kept as an
            example, and it is shown as StatementTemplate rather than as that variant's text.
        0 = ad hoc statement with no query_hash, identified by sql_handle and offsets.
        1 = module statement, identified by database, schema, object name and offsets.  Not by the handle:
            a module handle contains the object_id, which changes when the object is dropped and recreated,
            so a handle keyed history would split at every redeploy.
        2 = the rollup of every family that missed the ranking cut, one per database per interval.
        3 = the rollup of a kept family's statements beyond the per family cap - the same query in more
            procedures than the cap keeps.
        4 = the rollup of the databases whose own rollups (2) missed the cap on them, one per interval.
    */
    StatementType TINYINT NOT NULL,
    DatabaseID INT NULL,
    SchemaName NVARCHAR(128) NULL,
    ObjectName NVARCHAR(128) NULL,
    /* Attributes, not identity: both move without the statement changing. */
    object_id INT NULL,
    sql_handle VARBINARY(64) NULL,
    statement_start_offset INT NOT NULL,
    statement_end_offset INT NOT NULL,
    /*
        The family this statement belongs to.  A shape rather than an identity: the same hash is shared by
        every literal variant of a query, and also by an equivalent statement in another database or inside
        a procedure.  Grouping by it gives the family view; it is never used to identify a statement.
    */
    query_hash BINARY(8) NULL,
    FirstSeen DATETIME2(3) NOT NULL,
    LastSeen DATETIME2(3) NOT NULL,
    /*
        What an ad hoc shape (5) is shown as: its example's statement with the literal values replaced - '?',
        N'?', ? - and the comments removed, which is what its variants differ in.  Any one variant's own text
        would present its values as though every execution had used them.  Made by the collector, once per
        shape, and kept from the first collection that sends one; the example's handle and offsets are set
        with it, so the example batch is the one the template was made from.  Null until then, and for every
        other type, which is shown as its own text.
    */
    StatementTemplate NVARCHAR(MAX) NULL,
    CONSTRAINT PK_QueryStatements PRIMARY KEY CLUSTERED (StatementID ASC),
    CONSTRAINT UQ_QueryStatements_InstanceID_StatementKey UNIQUE (InstanceID ASC, StatementKey ASC),
    CONSTRAINT FK_QueryStatements_Instances FOREIGN KEY (InstanceID) REFERENCES dbo.Instances (InstanceID),
    CONSTRAINT FK_QueryStatements_Databases FOREIGN KEY (DatabaseID) REFERENCES dbo.Databases (DatabaseID)
);


GO
/*
    Templates off the row, so the rows stay narrow.  The table is read a few hundred rows at a time by the
    reports, which pay one extra page per template they show, but scanned whole by the purge and written by
    every import - and a template in row would make most rows of an instance running ad hoc SQL several times
    wider, and grow a row in place when its template arrives after the statement.
*/
EXECUTE sp_tableoption @TableNamePattern = N'[dbo].[QueryStatements]', @OptionName = N'large value types out of row', @OptionValue = 1;

GO
/* The family rollup: group statements by shape within an instance. */
CREATE NONCLUSTERED INDEX IX_QueryStatements_InstanceID_query_hash
ON dbo.QueryStatements (InstanceID, query_hash)
INCLUDE (DatabaseID, StatementType);

GO
/* PurgeQueryText checks this before deleting a batch's text, and the report joins on it. */
CREATE NONCLUSTERED INDEX IX_QueryStatements_sql_handle
ON dbo.QueryStatements (sql_handle)
INCLUDE (statement_start_offset, statement_end_offset);

GO
/* Drill down from an object to the statements inside it. */
CREATE NONCLUSTERED INDEX IX_QueryStatements_DatabaseID_ObjectName
ON dbo.QueryStatements (DatabaseID, ObjectName, SchemaName)
WHERE ObjectName IS NOT NULL;
