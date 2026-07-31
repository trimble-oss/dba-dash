CREATE PROC dbo.DBConfig_Upd(
	@DBConfig dbo.DBConfig READONLY,
	@InstanceID INT,
	@SnapshotDate DATETIME2(2)
)
AS
DECLARE @Ref VARCHAR(30)='DBConfig'
IF NOT EXISTS(SELECT 1 
				FROM dbo.CollectionDates 
				WHERE SnapshotDate>=@SnapshotDate 
				AND InstanceID = @InstanceID 
				AND Reference=@Ref
				)
BEGIN
	IF EXISTS(SELECT 1 
				FROM @DBConfig t
				WHERE NOT EXISTS(SELECT 1 
								FROM dbo.DBConfigOptions o 
								WHERE t.configuration_id = o.configuration_id
								)
	)
	BEGIN
		INSERT INTO dbo.DBConfigOptions(configuration_id,name)
		SELECT configuration_id,MAX(name)
		FROM @DBConfig t
		WHERE NOT EXISTS(SELECT 1 
						FROM dbo.DBConfigOptions o WITH(UPDLOCK,HOLDLOCK)
						WHERE t.configuration_id = o.configuration_id
						)
		GROUP BY t.configuration_id
	END;

	CREATE TABLE #DBConfig(
		DatabaseID INT,
		configuration_id INT,
		value NVARCHAR(128) COLLATE DATABASE_DEFAULT,
		value_for_secondary NVARCHAR(128) COLLATE DATABASE_DEFAULT,
		PRIMARY KEY(DatabaseID,configuration_id)
	)
	INSERT INTO #DBConfig(DatabaseID,configuration_id,value,value_for_secondary)
	SELECT d.DatabaseID,t.configuration_id,t.value,t.value_for_secondary
	FROM @DBConfig t
	JOIN dbo.Databases d ON t.database_id = d.database_id
	WHERE d.InstanceID = @InstanceID
	AND d.IsActive = 1

	INSERT INTO dbo.DBConfig(DatabaseID,configuration_id,value,value_for_secondary,ValidFrom)
	SELECT t.DatabaseID,t.configuration_id,t.value,t.value_for_secondary,@SnapshotDate
	FROM #DBConfig t
	WHERE NOT EXISTS(SELECT 1
					FROM dbo.DBConfig c
					WHERE c.DatabaseID = t.DatabaseID
					AND c.configuration_id = t.configuration_id
					)

	UPDATE c
	SET C.value = cfg.value,
		C.value_for_secondary = cfg.value_for_secondary
	OUTPUT DELETED.DatabaseID,
			DELETED.configuration_id,
			DELETED.value,
			DELETED.value_for_secondary,
			INSERTED.value,
			INSERTED.value_for_secondary,
			DELETED.ValidFrom,
			@SnapshotDate 
	INTO dbo.DBConfigHistory(DatabaseID,
							configuration_id,
							value,
							value_for_secondary,
							new_value,
							new_value_for_secondary,
							ValidFrom,
							ValidTo
							)
	FROM dbo.DBConfig c
	JOIN #DBConfig cfg ON c.configuration_id = cfg.configuration_id AND cfg.DatabaseID = c.DatabaseID
	WHERE  (	EXISTS(SELECT c.value
					EXCEPT SELECT cfg.value
					)
			OR EXISTS(SELECT c.value_for_secondary 
						EXCEPT 
						SELECT cfg.value_for_secondary
						)
		)
	OPTION (MAXDOP 1);


	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										 @Reference = @Ref,
										 @SnapshotDate = @SnapshotDate
END