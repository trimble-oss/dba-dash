CREATE FUNCTION dbo.DBSchemaAtDate(@DatabaseID INT,@Date DATETIME2(3))
RETURNS TABLE
AS
RETURN 
SELECT H.ObjectID,H.DDLID,O.ObjectType,O.ObjectName,O.SchemaName,MAX(H.SnapshotValidFrom) OVER() AS SnapshotDate
FROM dbo.DDLHistory H
JOIN dbo.DBObjects O ON O.ObjectID = H.ObjectID
WHERE H.SnapshotValidFrom<=@Date
AND H.SnapshotValidTo>@Date
AND H.DatabaseID = @DatabaseID
AND @Date IS NOT NULL
UNION ALL
SELECT H.ObjectID,H.DDLID,O.ObjectType,O.ObjectName,O.SchemaName,MAX(H.SnapshotValidFrom) OVER() AS SnapshotDate
FROM dbo.DDLHistory H
JOIN dbo.DBObjects O ON O.ObjectID = H.ObjectID
WHERE H.SnapshotValidTo = '9999-12-31 00:00:00.000'
AND H.DatabaseID = @DatabaseID
AND @Date IS NULL