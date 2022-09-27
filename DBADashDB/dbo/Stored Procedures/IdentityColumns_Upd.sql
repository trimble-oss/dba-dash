CREATE PROC dbo.IdentityColumns_Upd(
		@IdentityColumns dbo.IdentityColumns READONLY,
		@InstanceID INT,
		@SnapshotDate DATETIME2(2)
)
AS
DECLARE @Ref VARCHAR(30)='IdentityColumns'
IF NOT EXISTS(  SELECT 1 
                FROM dbo.CollectionDates 
                WHERE SnapshotDate>=@SnapshotDate 
                AND InstanceID = @InstanceID 
                AND Reference=@Ref)
BEGIN
    BEGIN TRAN;

	DELETE dbo.IdentityColumns 
    WHERE InstanceID = @InstanceID

	INSERT INTO dbo.IdentityColumns(
        InstanceID,
        DatabaseID,
        SnapshotDate,
        object_id,
        object_name,
        column_name,
        last_value,
        row_count,
        system_type_id,
        user_type_id,
        max_length,
        increment_value,
        seed_value,
        schema_name
    )
    SELECT InstanceID,
        DatabaseID,
        @SnapshotDate,
        object_id,
        object_name,
        column_name,
        last_value,
        row_count,
        system_type_id,
        user_type_id,
        max_length,
        increment_value,
        seed_value,
        schema_name
    FROM @IdentityColumns IC 
    JOIN dbo.Databases D ON IC.database_id = D.database_id
    WHERE D.InstanceID = @InstanceID
    AND D.IsActive=1


    INSERT INTO dbo.IdentityColumnsHistory
    (
        InstanceID,
        DatabaseID,
        object_id,
        SnapshotDate,
        last_value,
        row_count
    )
    SELECT InstanceID,
        DatabaseID,
        object_id,
        @SnapshotDate,
        last_value,
        row_count
    FROM @IdentityColumns IC 
    JOIN dbo.Databases D ON IC.database_id = D.database_id
    WHERE D.InstanceID = @InstanceID
    AND D.IsActive=1
	
	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
	                             @Reference = @Ref,
	                             @SnapshotDate = @SnapshotDate;
	COMMIT;
END;