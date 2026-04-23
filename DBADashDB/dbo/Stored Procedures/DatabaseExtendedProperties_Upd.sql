CREATE PROC [dbo].[DatabaseExtendedProperties_Upd] (
    @DatabaseExtendedProperties [dbo].[DatabaseExtendedProperties] READONLY,
    @InstanceID INT,
    @SnapshotDate DATETIME2(2)
)
AS
DECLARE @Ref VARCHAR(50) = 'DatabaseExtendedProperties'
IF NOT EXISTS (
    SELECT 1
    FROM dbo.CollectionDates
    WHERE SnapshotDate >= @SnapshotDate
    AND InstanceID = @InstanceID
    AND Reference = @Ref
)
BEGIN
    -- Insert new properties not yet tracked
    INSERT INTO dbo.DatabaseExtendedProperties (DatabaseID, Name, Value, ValidFrom)
    SELECT d.DatabaseID, t.name, t.value, @SnapshotDate
    FROM @DatabaseExtendedProperties t
    JOIN dbo.Databases d ON t.database_id = d.database_id
    WHERE d.InstanceID = @InstanceID
    AND NOT EXISTS (
        SELECT 1
        FROM dbo.DatabaseExtendedProperties ep
        WHERE ep.DatabaseID = d.DatabaseID
        AND ep.Name = t.name
    )

    -- Update changed values; record old value to history via OUTPUT
    UPDATE ep
    SET ep.Value = t.value,
        ep.ValidFrom = @SnapshotDate
    OUTPUT
        DELETED.DatabaseID,
        DELETED.Name,
        DELETED.Value,
        INSERTED.Value,
        DELETED.ValidFrom,
        @SnapshotDate
    INTO dbo.DatabaseExtendedPropertiesHistory (DatabaseID, Name, Value, NewValue, ValidFrom, ValidTo)
    FROM dbo.DatabaseExtendedProperties ep
    JOIN dbo.Databases d ON d.DatabaseID = ep.DatabaseID
    JOIN @DatabaseExtendedProperties t ON t.database_id = d.database_id AND t.name = ep.Name
    WHERE d.InstanceID = @InstanceID
    AND d.IsActive = 1
    AND EXISTS (
        SELECT ep.Value
        EXCEPT
        SELECT t.value
    )

    -- Remove properties that no longer exist on the database; record deletion to history
    INSERT INTO dbo.DatabaseExtendedPropertiesHistory (DatabaseID, Name, Value, NewValue, ValidFrom, ValidTo)
    SELECT ep.DatabaseID, ep.Name, ep.Value, NULL, ep.ValidFrom, @SnapshotDate
    FROM dbo.DatabaseExtendedProperties ep
    JOIN dbo.Databases d ON d.DatabaseID = ep.DatabaseID
    WHERE d.InstanceID = @InstanceID
    AND d.IsActive = 1
    AND NOT EXISTS (
        SELECT 1
        FROM @DatabaseExtendedProperties t
        WHERE t.database_id = d.database_id
        AND t.name = ep.Name
    )

    DELETE ep
    FROM dbo.DatabaseExtendedProperties ep
    JOIN dbo.Databases d ON d.DatabaseID = ep.DatabaseID
    WHERE d.InstanceID = @InstanceID
    AND d.IsActive = 1
    AND NOT EXISTS (
        SELECT 1
        FROM @DatabaseExtendedProperties t
        WHERE t.database_id = d.database_id
        AND t.name = ep.Name
    )

    EXEC dbo.CollectionDates_Upd
        @InstanceID = @InstanceID,
        @Reference = @Ref,
        @SnapshotDate = @SnapshotDate
END
