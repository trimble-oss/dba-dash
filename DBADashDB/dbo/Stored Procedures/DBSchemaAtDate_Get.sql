CREATE PROC dbo.DBSchemaAtDate_Get(
	@DBID INT,
	@SnapshotDate DATETIME2(3)
)
AS
SELECT s.ObjectID,
        s.DDLID,
        s.ObjectType,
        s.ObjectName,
        s.SchemaName,
        s.SnapshotDate,
        OT.TypeDescription,
		DDL.DDL
FROM dbo.DBSchemaAtDate(@DBID,@SnapshotDate) s
JOIN dbo.ObjectType OT ON S.ObjectType = OT.ObjectType
JOIN dbo.DDL ON s.DDLID = DDL.DDLID
