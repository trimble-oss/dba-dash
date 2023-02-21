CREATE PROC dbo.Corruption_Upd(
		@Corruption dbo.Corruption READONLY,
		@InstanceID INT,
		@SnapshotDate DATETIME2(2)
)
AS
DECLARE @Ref VARCHAR(30)='Corruption'
IF NOT EXISTS	(	
				SELECT 1 
				FROM dbo.CollectionDates 
				WHERE SnapshotDate>=@SnapshotDate 
				AND InstanceID = @InstanceID 
				AND Reference=@Ref				
				)
BEGIN
	CREATE TABLE #CorruptionTemp(
		DatabaseID INT NOT NULL,
		SourceTable TINYINT NOT NULL,
		UpdateDate DATETIME2(3) NOT NULL
	)
	INSERT INTO #CorruptionTemp
	(
	    DatabaseID,
	    SourceTable,
	    UpdateDate
	)
	SELECT	D.DatabaseID, 
			C.SourceTable,
			C.last_update_date 
	FROM @Corruption C 
	JOIN dbo.Databases D ON C.database_id = D.database_id AND D.InstanceID  = @InstanceID
	WHERE D.IsActive=1

	UPDATE C
		SET C.UpdateDate = T.UpdateDate
	FROM dbo.Corruption C 
	JOIN #CorruptionTemp T ON C.DatabaseID = T.DatabaseID AND C.SourceTable = T.SourceTable
	WHERE T.UpdateDate>C.UpdateDate

	INSERT INTO dbo.Corruption
	(
		DatabaseID,
		SourceTable,
		UpdateDate
	)
	SELECT DatabaseID,
		SourceTable,
		UpdateDate 
	FROM #CorruptionTemp T
	WHERE NOT EXISTS(
					SELECT 1 
					FROM dbo.Corruption C
					WHERE C.DatabaseID = T.DatabaseID
					AND C.SourceTable = T.SourceTable
					)

	DELETE  C 
	FROM dbo.Corruption C 
	JOIN dbo.Databases D ON C.DatabaseID = D.DatabaseID 
	WHERE D.InstanceID = @InstanceID
	AND NOT EXISTS(SELECT 1
					FROM #CorruptionTemp T 
					WHERE T.DatabaseID = D.DatabaseID 
					AND T.SourceTable = C.SourceTable)

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
									 @Reference = @Ref,
									 @SnapshotDate = @SnapshotDate

END