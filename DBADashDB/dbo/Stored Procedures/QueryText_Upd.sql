CREATE PROC dbo.QueryText_Upd(
	@QueryText dbo.QueryText READONLY,
	@InstanceID INT,
	@SnapshotDate DATETIME2(2)
)
AS
INSERT INTO dbo.QueryText
(
    sql_handle,
    dbid,
    object_id,
    encrypted,
    text,
    SnapshotDate
)
SELECT sql_handle,
    dbid,
    object_id,
    encrypted,
    text,
    @SnapshotDate
FROM @QueryText t
WHERE NOT EXISTS(SELECT 1 
			FROM dbo.QueryText QT
			WHERE QT.sql_handle = t.sql_handle
			)
