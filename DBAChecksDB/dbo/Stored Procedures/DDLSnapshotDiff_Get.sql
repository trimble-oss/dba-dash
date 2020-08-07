CREATE PROC dbo.DDLSnapshotDiff_Get(
	@DatabaseID INT,
	@SnapshotDate DATETIME2(3)
)
AS
WITH Hold AS (
	SELECT * 
	FROM dbo.DDLHistory H
	WHERE H.DatabaseID = @DatabaseID
	AND H.SnapshotValidTo = @SnapshotDate
),
Hnew AS (
	SELECT * 
	FROM dbo.DDLHistory H
	WHERE H.DatabaseID = @DatabaseID
	AND H.SnapshotValidFrom = @SnapshotDate 
)
SELECT O.ObjectName,O.SchemaName,O.ObjectType, CASE WHEN Hold.ObjectID IS NULL THEN 'Created' WHEN Hnew.ObjectID IS NULL THEN 'Dropped' ELSE 'Modified' END AS Action,
		Hnew.DDLID NewDDLID,
		HOld.DDLID OldDDLID
FROM Hnew
FULL JOIN Hold ON Hold.ObjectID = Hnew.ObjectID
JOIN dbo.DBObjects O ON Hold.ObjectID = O.ObjectID OR Hnew.ObjectID= O.ObjectID