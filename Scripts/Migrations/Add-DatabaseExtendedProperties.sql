-- ============================================================
-- Migration: Add DatabaseExtendedProperties collection (v2)
-- Apply to: DBADash repository database
-- Handles: fresh install and upgrade from v1 (no InstanceID)
-- ============================================================

SET NOCOUNT ON
GO

-- 1. User-defined table type (TVP)
IF NOT EXISTS (SELECT 1 FROM sys.table_types WHERE schema_id = SCHEMA_ID('dbo') AND name = 'DatabaseExtendedProperties')
BEGIN
    EXEC sp_executesql N'CREATE TYPE [dbo].[DatabaseExtendedProperties] AS TABLE ([database_id] INT NOT NULL, [name] SYSNAME NOT NULL, [value] NVARCHAR(MAX) NULL, PRIMARY KEY CLUSTERED ([database_id] ASC, [name] ASC))'
    PRINT 'Created TYPE dbo.DatabaseExtendedProperties'
END
ELSE PRINT 'TYPE dbo.DatabaseExtendedProperties already exists - skipped'
GO

-- 2. Drop SP first (depends on tables we are about to alter)
IF EXISTS (SELECT 1 FROM sys.procedures WHERE schema_id = SCHEMA_ID('dbo') AND name = 'DatabaseExtendedProperties_Upd')
BEGIN DROP PROCEDURE [dbo].[DatabaseExtendedProperties_Upd]; PRINT 'Dropped PROCEDURE' END
GO

-- 3. Drop and recreate history table (OUTPUT INTO cannot target tables with FK; also adding InstanceID)
IF EXISTS (SELECT 1 FROM sys.tables WHERE schema_id = SCHEMA_ID('dbo') AND name = 'DatabaseExtendedPropertiesHistory')
BEGIN DROP TABLE [dbo].[DatabaseExtendedPropertiesHistory]; PRINT 'Dropped old history table' END
GO
CREATE TABLE [dbo].[DatabaseExtendedPropertiesHistory] ([InstanceID] INT NOT NULL, [DatabaseID] INT NOT NULL, [Name] SYSNAME NOT NULL, [Value] NVARCHAR(MAX) NULL, [NewValue] NVARCHAR(MAX) NULL, [ValidFrom] DATETIME2(2) NOT NULL, [ValidTo] DATETIME2(2) NOT NULL, CONSTRAINT [PK_DatabaseExtendedPropertiesHistory] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [DatabaseID] ASC, [Name] ASC, [ValidTo] ASC))
PRINT 'Created TABLE dbo.DatabaseExtendedPropertiesHistory (with InstanceID)'
GO

-- 4. Drop and recreate current-state table (adding InstanceID)
IF EXISTS (SELECT 1 FROM sys.tables WHERE schema_id = SCHEMA_ID('dbo') AND name = 'DatabaseExtendedProperties')
BEGIN DROP TABLE [dbo].[DatabaseExtendedProperties]; PRINT 'Dropped old current table' END
GO
CREATE TABLE [dbo].[DatabaseExtendedProperties] ([InstanceID] INT NOT NULL, [DatabaseID] INT NOT NULL, [Name] SYSNAME NOT NULL, [Value] NVARCHAR(MAX) NULL, [ValidFrom] DATETIME2(2) NOT NULL, CONSTRAINT [PK_DatabaseExtendedProperties] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [DatabaseID] ASC, [Name] ASC), CONSTRAINT [FK_DatabaseExtendedProperties_Instances] FOREIGN KEY ([InstanceID]) REFERENCES [dbo].[Instances] ([InstanceID]), CONSTRAINT [FK_DatabaseExtendedProperties_Databases] FOREIGN KEY ([DatabaseID]) REFERENCES [dbo].[Databases] ([DatabaseID]))
PRINT 'Created TABLE dbo.DatabaseExtendedProperties (with InstanceID)'
GO

-- 5. Create stored procedure
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
    INSERT INTO dbo.DatabaseExtendedProperties (InstanceID, DatabaseID, Name, Value, ValidFrom)
    SELECT @InstanceID, d.DatabaseID, t.name, t.value, @SnapshotDate
    FROM @DatabaseExtendedProperties t
    JOIN dbo.Databases d ON t.database_id = d.database_id
    WHERE d.InstanceID = @InstanceID
    AND NOT EXISTS (
        SELECT 1
        FROM dbo.DatabaseExtendedProperties ep
        WHERE ep.InstanceID = @InstanceID
        AND ep.DatabaseID = d.DatabaseID
        AND ep.Name = t.name
    )

    -- Update changed values; record old value to history via OUTPUT
    UPDATE ep
    SET ep.Value = t.value,
        ep.ValidFrom = @SnapshotDate
    OUTPUT
        DELETED.InstanceID,
        DELETED.DatabaseID,
        DELETED.Name,
        DELETED.Value,
        INSERTED.Value,
        DELETED.ValidFrom,
        @SnapshotDate
    INTO dbo.DatabaseExtendedPropertiesHistory (InstanceID, DatabaseID, Name, Value, NewValue, ValidFrom, ValidTo)
    FROM dbo.DatabaseExtendedProperties ep
    JOIN dbo.Databases d ON d.DatabaseID = ep.DatabaseID
    JOIN @DatabaseExtendedProperties t ON t.database_id = d.database_id AND t.name = ep.Name
    WHERE ep.InstanceID = @InstanceID
    AND d.IsActive = 1
    AND EXISTS (
        SELECT ep.Value
        EXCEPT
        SELECT t.value
    )

    -- Remove properties that no longer exist on the database; record deletion to history
    INSERT INTO dbo.DatabaseExtendedPropertiesHistory (InstanceID, DatabaseID, Name, Value, NewValue, ValidFrom, ValidTo)
    SELECT @InstanceID, ep.DatabaseID, ep.Name, ep.Value, NULL, ep.ValidFrom, @SnapshotDate
    FROM dbo.DatabaseExtendedProperties ep
    JOIN dbo.Databases d ON d.DatabaseID = ep.DatabaseID
    WHERE ep.InstanceID = @InstanceID
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
    WHERE ep.InstanceID = @InstanceID
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


GO
PRINT 'Created PROCEDURE dbo.DatabaseExtendedProperties_Upd'
GO

-- 6. Verify
SELECT OBJECT_NAME(object_id) AS ObjectName, type_desc FROM sys.objects WHERE name IN ('DatabaseExtendedProperties','DatabaseExtendedPropertiesHistory','DatabaseExtendedProperties_Upd') ORDER BY type_desc, name
GO


-- 7. Get SP for GUI
IF EXISTS (SELECT 1 FROM sys.procedures WHERE schema_id = SCHEMA_ID('dbo') AND name = 'DatabaseExtendedProperties_Get') DROP PROCEDURE [dbo].[DatabaseExtendedProperties_Get]
GO
CREATE PROC [dbo].[DatabaseExtendedProperties_Get] (
    @InstanceIDs VARCHAR(MAX) = NULL,
    @DatabaseID INT = -1
)
AS
SELECT I.InstanceDisplayName AS Instance,
       D.name AS [Database],
       EP.Name AS [Property],
       EP.Value,
       EP.ValidFrom
FROM dbo.DatabaseExtendedProperties EP
JOIN dbo.Databases D ON D.DatabaseID = EP.DatabaseID
JOIN dbo.Instances I ON I.InstanceID = EP.InstanceID
WHERE (
        @InstanceIDs IS NULL
        OR EXISTS (
            SELECT 1
            FROM STRING_SPLIT(@InstanceIDs, ',') s
            WHERE CAST(s.value AS INT) = EP.InstanceID
        )
      )
AND (@DatabaseID = -1 OR EP.DatabaseID = @DatabaseID)
AND D.IsActive = 1
ORDER BY I.InstanceDisplayName, D.name, EP.Name


GO
PRINT 'Created PROCEDURE dbo.DatabaseExtendedProperties_Get'
