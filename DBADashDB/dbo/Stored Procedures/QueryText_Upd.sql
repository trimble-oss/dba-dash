CREATE PROC dbo.QueryText_Upd(
	@QueryText dbo.QueryText READONLY,
	@InstanceID INT,
	@SnapshotDate DATETIME2(2)
)
AS
DECLARE @Ref VARCHAR(30)='QueryText'

INSERT INTO dbo.QueryText
(
    sql_handle,
    dbid,
    object_id,
    encrypted,
    text,
    SnapshotDate,
    CollectInstanceID
)
SELECT sql_handle,
    dbid,
    object_id,
    encrypted,
    text,
    @SnapshotDate,
    @InstanceID
FROM @QueryText t
WHERE NOT EXISTS(SELECT 1 
			FROM dbo.QueryText QT
			WHERE QT.sql_handle = t.sql_handle
			)

EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										@Reference = @Ref,
										@SnapshotDate = @SnapshotDate;