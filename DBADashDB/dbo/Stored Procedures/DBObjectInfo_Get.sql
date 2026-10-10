CREATE PROC dbo.DBObjectInfo_Get(
    @ObjectID BIGINT = NULL,
    @InstanceID INT = NULL,
    @DatabaseID INT = NULL,
    @SchemaName sysname = NULL,
    @ObjectName sysname = NULL
)
AS
/*
    Identifies an object from whatever a caller knows about it, for the object detail window.

    Callers know different things.  A running query has the repository's ObjectID, a slow query only its database and
    bare name, and a deadlock or query stats row a qualified name.  With @ObjectID the object is returned as is.
    Otherwise it is looked up by name on the instance, narrowed by database and schema where they are given.  More than
    one row back means the name is ambiguous - the same name in several databases or schemas - and the caller decides
    what to do with that rather than this guessing.  Active objects are returned first.
*/
SET NOCOUNT ON

SELECT O.ObjectID,
       D.InstanceID,
       I.InstanceDisplayName AS Instance,
       O.DatabaseID,
       D.name AS DatabaseName,
       O.SchemaName,
       O.ObjectName,
       O.ObjectType,
       OT.TypeDescription,
       O.IsActive
FROM dbo.DBObjects O
JOIN dbo.Databases D ON D.DatabaseID = O.DatabaseID
JOIN dbo.Instances I ON I.InstanceID = D.InstanceID
LEFT JOIN dbo.ObjectType OT ON OT.ObjectType = O.ObjectType
WHERE (
        O.ObjectID = @ObjectID
        OR (
            @ObjectID IS NULL
            AND D.InstanceID = @InstanceID
            AND O.ObjectName = @ObjectName
            AND (O.DatabaseID = @DatabaseID OR @DatabaseID IS NULL)
            AND (O.SchemaName = @SchemaName OR @SchemaName IS NULL)
            AND D.IsActive = 1
        )
      )
ORDER BY O.IsActive DESC,
         D.name,
         O.SchemaName
