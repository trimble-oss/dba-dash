/*
	The definitions of named objects in a database, as at a point in time.

	Written for the deadlock viewer, which knows the handful of tables and modules a deadlock touched
	and wants their definitions as they were when it happened - the procedure may have been changed
	twice since.  dbo.DBSchemaAtDate does the point-in-time part; this adds the filter, because
	fetching an entire database's schema to pick three objects out of it is not a reasonable way to
	answer that question.

	@ObjectNames is a comma separated list, each entry either "object" or "schema.object".
	@SnapshotDate NULL returns the current schema, which is what dbo.DBSchemaAtDate does with a NULL
	date - the sensible answer when the graph carries no usable time.

	Returns nothing when the database is unknown or schema snapshots have never run for it.  That is
	an ordinary state - snapshots are optional - so the caller treats an empty result as "no schema
	available" rather than an error.
*/
CREATE PROC dbo.DeadlockObjectDDL_Get
(
	@InstanceID INT,
	@DatabaseName sysname,
	@ObjectNames NVARCHAR(MAX),
	@SnapshotDate DATETIME2(3) = NULL
)
AS
SET NOCOUNT ON

DECLARE @DatabaseID INT

/* A database name can have been dropped and recreated; the most recent is the one meant. */
SELECT @DatabaseID = MAX(DatabaseID)
FROM dbo.Databases
WHERE InstanceID = @InstanceID
AND name = @DatabaseName

IF @DatabaseID IS NULL RETURN

;WITH Requested AS
(
	SELECT DISTINCT LTRIM(RTRIM(value)) AS ObjectName
	FROM STRING_SPLIT(@ObjectNames, ',')
	WHERE LTRIM(RTRIM(value)) <> ''
)
SELECT S.SchemaName,
	S.ObjectName,
	OT.TypeDescription,
	S.SnapshotDate,
	DDL.DDL
FROM dbo.DBSchemaAtDate(@DatabaseID, @SnapshotDate) S
JOIN dbo.ObjectType OT ON S.ObjectType = OT.ObjectType
JOIN dbo.DDL ON S.DDLID = DDL.DDLID
JOIN Requested R
	ON R.ObjectName = S.ObjectName COLLATE DATABASE_DEFAULT
	OR R.ObjectName = CONCAT(S.SchemaName, '.', S.ObjectName) COLLATE DATABASE_DEFAULT
ORDER BY S.SchemaName, S.ObjectName
