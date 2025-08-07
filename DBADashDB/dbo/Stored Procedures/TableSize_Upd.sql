CREATE PROCEDURE dbo.TableSize_Upd
(
	@InstanceID INT,
	@SnapshotDate DATETIME2,
	@TableSize TableSize READONLY
)
AS
SET XACT_ABORT ON
SET NOCOUNT ON
DECLARE @Ref VARCHAR(30)='TableSize'

UPDATE O
	SET O.IsActive = CAST(1 AS BIT)
FROM @TableSize t
JOIN dbo.Databases d ON t.DB = d.name AND D.InstanceID=@InstanceID
JOIN dbo.DBObjects O ON O.DatabaseID = d.DatabaseID 
						AND O.ObjectName = t.object_name 
						AND O.SchemaName = t.schema_name
						AND O.ObjectType = t.type
WHERE O.IsActive = CAST(0 AS BIT);

INSERT INTO dbo.DBObjects
(
    DatabaseID,
	ObjectName,
    object_id,
	ObjectType,
    SchemaName,
    IsActive
)
SELECT d.DatabaseID,
	t.object_name,
	t.object_id,
	t.type,
	t.schema_name,
	CAST(1 AS BIT)
FROM @TableSize t
JOIN dbo.Databases d ON t.DB = d.name AND D.InstanceID=@InstanceID
WHERE D.IsActive=1
AND NOT EXISTS(
				SELECT 1 
				FROM dbo.DBObjects O 
				WHERE O.DatabaseID = d.DatabaseID 
				AND O.ObjectName = t.object_name 
				AND O.SchemaName = t.schema_name
				AND O.ObjectType = t.type
			)
AND t.schema_name <> '{DBADashError}'
GROUP BY d.DatabaseID,
	t.object_name,
	t.type,
	t.schema_name,
	t.object_id;

INSERT INTO dbo.TableSize
(
	InstanceID,
	SnapshotDate,
	DatabaseID,
	ObjectID,
	row_count,
	reserved_pages,
	used_pages,
	data_pages
)
SELECT
	@InstanceID AS InstanceID,
	t.SnapshotDate,
	D.DatabaseID,
	O.ObjectID,
	t.row_count,
	t.reserved_pages,
	t.used_pages,
	t.data_pages
FROM @TableSize t
JOIN dbo.Databases D ON t.DB = D.name AND D.InstanceID = @InstanceID
JOIN dbo.DBObjects O ON O.SchemaName = t.schema_name 
					AND O.ObjectName = t.object_name 
					AND O.DatabaseID = D.DatabaseID 
					AND t.type = O.ObjectType
WHERE t.schema_name <> '{DBADashError}'
AND O.IsActive = CAST(1 AS BIT);

DECLARE @ExcludedDBs NVARCHAR(MAX)

SET @ExcludedDBs = STUFF(
	(
	SELECT ', ' + QUOTENAME(t.DB)
	FROM @TableSize t
	WHERE schema_name = '{DBADashError}'
	AND object_name = '{TableCountExceededThreshold}'
	ORDER BY t.DB
	FOR XML PATH(''),TYPE
	).value('.','NVARCHAR(MAX)'),
	1,2,'')

IF @ExcludedDBs IS NOT NULL
BEGIN
	INSERT INTO dbo.CollectionErrorLog(ErrorDate,InstanceID, ErrorSource, ErrorMessage, ErrorContext)
	SELECT @SnapshotDate,@InstanceID,'TableSize',CONCAT('Warning: ',@ExcludedDBs,' were excluded from TableSize collection due to table count threshold'),'Collect'
END

/* Log the data collection */
EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,
		@Reference = @Ref,
		@SnapshotDate = @SnapshotDate
GO


