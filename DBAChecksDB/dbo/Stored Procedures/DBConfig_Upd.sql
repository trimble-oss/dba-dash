
CREATE PROC [dbo].[DBConfig_Upd](@DBConfig dbo.DBConfig READONLY,@InstanceID INT,@SnapshotDate DATETIME2(2))
AS
DECLARE @Ref VARCHAR(30)='DBConfig'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN
	IF EXISTS(SELECT 1 
				FROM @DBConfig t
				WHERE NOT EXISTS(SELECT 1 FROM dbo.DBConfigOptions o WHERE t.configuration_id = o.configuration_id)
	)
	BEGIN
		INSERT INTO dbo.DBConfigOptions(configuration_id,name)
		SELECT configuration_id,MAX(name)
		FROM @DBConfig t
		WHERE NOT EXISTS(SELECT 1 FROM dbo.DBConfigOptions o WHERE t.configuration_id = o.configuration_id)
		GROUP BY t.configuration_id
	END;

	INSERT INTO dbo.DBConfig(DatabaseID,configuration_id,value,value_for_secondary,ValidFrom)
	SELECT d.DatabaseID,t.configuration_id,t.value,t.value_for_secondary,@SnapshotDate
	FROM @DBConfig t
	JOIN dbo.Databases d ON t.database_id = d.database_id
	WHERE d.InstanceID = @InstanceID
	AND NOT EXISTS(SELECT 1 FROM dbo.DBConfig c WHERE c.DatabaseID = d.DatabaseID AND c.configuration_id = t.configuration_id);

	WITH T AS (
		SELECT c.DatabaseID,
			   c.configuration_id,
			   c.value,
			   c.value_for_secondary ,
			   c.ValidFrom
		FROM dbo.DBConfig c
		JOIN dbo.Databases d ON d.database_id = c.DatabaseID
		WHERE d.InstanceID = @InstanceID
		AND d.IsActive=1
	)
	MERGE T USING(
		SELECT d.DatabaseID,
				c.configuration_id,
				c.name,
				c.value,
				c.value_for_secondary
		FROM @DBConfig c
		JOIN dbo.Databases d ON d.database_id = c.database_id
		WHERE d.IsActive=1
		) AS S
		ON S.configuration_id = T.configuration_id AND s.DatabaseID = T.DatabaseID
	WHEN MATCHED AND NOT ( 
						(t.value = S.value OR (t.value IS NULL AND S.value IS NULL))
					AND (t.value_for_secondary = S.value_for_secondary OR (t.value_for_secondary IS NULL AND S.value_for_secondary IS NULL))
					) THEN
	UPDATE 
	SET T.value = S.value,
	T.value_for_secondary = S.value_for_secondary
	WHEN NOT MATCHED BY SOURCE THEN 
	DELETE
	OUTPUT DELETED.DatabaseID,DELETED.configuration_id,DELETED.value,DELETED.value_for_secondary,INSERTED.value,INSERTED.value_for_secondary,DELETED.ValidFrom,@SnapshotDate INTO dbo.DBConfigHistory(DatabaseID,configuration_id,value,value_for_secondary,new_value,new_value_for_secondary,ValidFrom,ValidTo);

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										 @Reference = @Ref,
										 @SnapshotDate = @SnapshotDate
END