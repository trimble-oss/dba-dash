CREATE PROC dbo.DDLHistoryForObject_Get(
	@ObjectID INT,
	@PageSize INT,
	@PageNumber INT
)
AS
WITH T AS (
SELECT O.ObjectName,O.SchemaName,O.ObjectType,H.SnapshotValidFrom,H.SnapshotValidTo,H.ObjectDateCreated,H.ObjectDateModified,H.DDLID,H2.DDLID AS DDLIDOld,CASE WHEN H2.DDLID IS NOT NULL THEN 'Modified' ELSE 'Created' END AS Action
FROM dbo.DDLHistory H
JOIN dbo.DBObjects O ON O.ObjectID = H.ObjectID
LEFT JOIN dbo.DDLHistory H2 ON H2.ObjectID = O.ObjectID AND H2.SnapshotValidTo = H.SnapshotValidFrom
WHERE H.ObjectID=@ObjectID
UNION ALL
SELECT O.ObjectName,O.SchemaName,O.ObjectType,H.SnapshotValidTo AS SnapshotValidFrom,x.SnapshotValidTo,NULL,NULL,NULL AS DDLID,H.DDLID AS DDLIDOld,'Dropped' AS Action
FROM dbo.DDLHistory H
JOIN dbo.DBObjects O ON O.ObjectID = H.ObjectID
OUTER APPLY(SELECT TOP(1) nH.SnapshotValidFrom AS SnapshotValidTo
			FROM dbo.DDLHistory nH 
			WHERE nH.ObjectID = H.ObjectID
			AND nH.SnapshotValidFrom> H.SnapshotValidTo
			ORDER BY nh.SnapshotValidFrom
			) x
WHERE H.ObjectID=@ObjectID
AND NOT EXISTS(SELECT 1 
				FROM dbo.DDLHistory H2 
				WHERE H.ObjectID = H2.ObjectID
				AND H.SnapshotValidTo = H2.SnapshotValidFrom
				)
AND H.SnapshotValidTo<>'9999-12-31 00:00:00.000'
)
SELECT T.ObjectName,
       T.SchemaName,
       T.ObjectType,
       T.SnapshotValidFrom,
       T.SnapshotValidTo,
       T.ObjectDateCreated,
       T.ObjectDateModified,
       T.DDLID,
       T.DDLIDOld,
	   T.Action
FROM T
ORDER BY T.SnapshotValidFrom DESC
OFFSET @PageSize* (@PageNumber-1) ROWS
FETCH NEXT @PageSize ROWS ONLY