CREATE PROC dbo.DatabaseDDLCompare_Get(
	@DBID_A INT,
	@DBID_B INT,
	@Date_A DATETIME2(3),
	@Date_B DATETIME2(3)
)
AS
WITH A AS (
	SELECT s.ObjectID,
           s.DDLID,
           s.ObjectType,
           s.ObjectName,
           s.SchemaName,
           s.SnapshotDate,
           OT.TypeDescription 
	FROM dbo.DBSchemaAtDate(@DBID_A,@Date_A) s
	JOIN dbo.ObjectType OT ON S.ObjectType = OT.ObjectType
)
,B AS(
	SELECT s.ObjectID,
           s.DDLID,
           s.ObjectType,
           s.ObjectName,
           s.SchemaName,
           s.SnapshotDate,
           OT.TypeDescription 
	FROM dbo.DBSchemaAtDate(@DBID_B,@Date_B) s
	JOIN dbo.ObjectType OT ON S.ObjectType = OT.ObjectType
)
SELECT ISNULL(A.ObjectName,B.ObjectName) AS ObjectName,
		ISNULL(A.SchemaName,B.SchemaName) SchemaName,
		ISNULL(A.ObjectType,B.ObjectType) ObjectType,
        ISNULL(A.TypeDescription,B.TypeDescription) TypeDescription,
		CASE WHEN A.ObjectID IS NULL THEN 'B Only' WHEN B.ObjectID IS NULL THEN 'A Only' WHEN A.DDLID=B.DDLID THEN 'Equal' ELSE 'Diff' END AS DiffType,
		A.DDLID AS DDLID_A,
		B.DDLID AS DDLID_B
FROM A
FULL JOIN B ON A.ObjectName = B.ObjectName AND A.SchemaName = B.SchemaName AND A.ObjectType = B.ObjectType