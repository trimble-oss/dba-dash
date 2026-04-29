CREATE PROC Alert.ActiveAlerts_FixIdentitySeed
AS
/* 
    Fixes AlertID identity seed conflicts between ActiveAlerts and ClosedAlerts.

    This can occur when:
    - A schema change causes a table rebuild with IDENTITY_INSERT
    - ActiveAlerts table is empty during the rebuild (identity resets to 1)
    - ClosedAlerts contains alerts with IDs that will be reused

    The procedure:
    1. Detects if there are conflicting AlertIDs between the tables
    2. Renumbers any active alerts that conflict with closed alerts
    3. Reseeds the identity to a safe value above all existing IDs

    Concurrency: Uses table locks in the same order as existing alert workflows to prevent deadlocks.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @MaxClosedAlertID BIGINT;
DECLARE @MaxActiveAlertID BIGINT;
DECLARE @CurrentIdentityValue BIGINT;

-- Lock tables to prevent concurrent modifications during the fix
-- This is safe during deployment as it should be a maintenance window
BEGIN TRAN

-- Acquire exclusive locks in the same order as existing alert workflows to prevent deadlocks
-- Alert.ClosedAlerts_Add locks in order: ActiveAlerts → ClosedAlerts → CustomThreadKey → NotificationLog
-- (INSERT into ClosedAlerts reads ActiveAlerts, then DELETEs from CustomThreadKey, NotificationLog, ActiveAlerts)
SELECT @MaxActiveAlertID = ISNULL(MAX(AlertID), 0) 
FROM Alert.ActiveAlerts WITH (TABLOCKX);

SELECT @MaxClosedAlertID = ISNULL(MAX(AlertID), 0) 
FROM Alert.ClosedAlerts WITH (TABLOCKX);

SELECT TOP(0) * FROM Alert.CustomThreadKey WITH (TABLOCKX, HOLDLOCK);
SELECT TOP(0) * FROM Alert.NotificationLog WITH (TABLOCKX, HOLDLOCK);

SELECT @CurrentIdentityValue = ISNULL(IDENT_CURRENT('Alert.ActiveAlerts'), 0);

PRINT 'Max ClosedAlerts AlertID: ' + CAST(@MaxClosedAlertID AS NVARCHAR(20));
PRINT 'Max ActiveAlerts AlertID: ' + CAST(@MaxActiveAlertID AS NVARCHAR(20));
PRINT 'Current Identity Value: ' + CAST(@CurrentIdentityValue AS NVARCHAR(20));

-- Check if there are any conflicts or if identity seed is incorrect
IF @MaxClosedAlertID > @CurrentIdentityValue OR EXISTS (
    SELECT 1 
    FROM Alert.ActiveAlerts AA
    WHERE EXISTS (
        SELECT 1 
        FROM Alert.ClosedAlerts CA 
        WHERE CA.AlertID = AA.AlertID
    )
)
BEGIN
    PRINT 'Identity seed issue detected - fixing...'

    -- Find the new starting ID (max of both tables + 1)
    DECLARE @NewStartID BIGINT = (SELECT MAX(MaxID) FROM (
        SELECT @MaxClosedAlertID AS MaxID
        UNION ALL
        SELECT @MaxActiveAlertID
    ) AS T) + 1;

    -- Renumber any active alerts that conflict with closed alerts
    IF EXISTS (
        SELECT 1 
        FROM Alert.ActiveAlerts AA
        WHERE EXISTS (
            SELECT 1 
            FROM Alert.ClosedAlerts CA 
            WHERE CA.AlertID = AA.AlertID
        )
    )
    BEGIN
        DECLARE @ConflictCount INT;
        SELECT @ConflictCount = COUNT(*)
        FROM Alert.ActiveAlerts AA
        WHERE EXISTS (
            SELECT 1 
            FROM Alert.ClosedAlerts CA 
            WHERE CA.AlertID = AA.AlertID
        );

        PRINT 'Found ' + CAST(@ConflictCount AS NVARCHAR(20)) + ' conflicting AlertID(s) in ActiveAlerts'
        PRINT 'Renumbering conflicting alerts...'

        -- Create a mapping table for the renumbering
        DECLARE @AlertIDMapping TABLE (
            OldAlertID BIGINT,
            NewAlertID BIGINT
        );

        -- Generate new IDs for conflicting alerts
        WITH ConflictingAlerts AS (
            SELECT AA.AlertID, 
                   ROW_NUMBER() OVER (ORDER BY AA.AlertID) AS RowNum
            FROM Alert.ActiveAlerts AA
            WHERE EXISTS (
                SELECT 1 
                FROM Alert.ClosedAlerts CA 
                WHERE CA.AlertID = AA.AlertID
            )
        )
        INSERT INTO @AlertIDMapping (OldAlertID, NewAlertID)
        SELECT AlertID, @NewStartID + RowNum - 1
        FROM ConflictingAlerts;

        -- Show the mapping
        SELECT 'Renumbering: ' + CAST(OldAlertID AS NVARCHAR(20)) + ' -> ' + CAST(NewAlertID AS NVARCHAR(20)) AS Mapping
        FROM @AlertIDMapping;

        -- Create temp table to hold the alert data before deletion
        DECLARE @AlertDataToMove TABLE (
            OldAlertID BIGINT,
            NewAlertID BIGINT,
            InstanceID INT,
            Priority TINYINT,
            AlertType VARCHAR(50),
            AlertKey NVARCHAR(256),
            FirstMessage NVARCHAR(MAX),
            LastMessage NVARCHAR(MAX),
            TriggerDate DATETIME2,
            UpdatedDate DATETIME2,
            FirstNotification DATETIME2,
            LastNotification DATETIME2,
            UpdateCount INT,
            ResolvedCount INT,
            NotificationCount INT,
            FailedNotificationCount INT,
            IsAcknowledged BIT,
            IsResolved BIT,
            ResolvedDate DATETIME2,
            IsBlackout BIT,
            Escalated DATETIME2,
            DeEscalated DATETIME2,
            Notes NVARCHAR(MAX),
            RuleID INT,
            GroupID INT,
            AcknowledgedDate DATETIME2
        );

        -- Capture the data with new AlertIDs before deleting
        INSERT INTO @AlertDataToMove
        SELECT 
            M.OldAlertID,
            M.NewAlertID,
            AA.InstanceID,
            AA.Priority,
            AA.AlertType,
            AA.AlertKey,
            AA.FirstMessage,
            AA.LastMessage,
            AA.TriggerDate,
            AA.UpdatedDate,
            AA.FirstNotification,
            AA.LastNotification,
            AA.UpdateCount,
            AA.ResolvedCount,
            AA.NotificationCount,
            AA.FailedNotificationCount,
            AA.IsAcknowledged,
            AA.IsResolved,
            AA.ResolvedDate,
            AA.IsBlackout,
            AA.Escalated,
            AA.DeEscalated,
            AA.Notes,
            AA.RuleID,
            AA.GroupID,
            AA.AcknowledgedDate
        FROM Alert.ActiveAlerts AA
        INNER JOIN @AlertIDMapping M ON AA.AlertID = M.OldAlertID;

        -- Delete the old conflicting alerts first (to satisfy unique index constraint)
        DELETE AA
        FROM Alert.ActiveAlerts AA
        INNER JOIN @AlertIDMapping M ON AA.AlertID = M.OldAlertID;

        PRINT 'Deleted ' + CAST(@@ROWCOUNT AS NVARCHAR(20)) + ' old alert(s) with conflicting IDs'

        -- Update related tables to point to new AlertIDs. No FKs, so we can update in place.
        UPDATE CTK
        SET CTK.AlertID = M.NewAlertID
        FROM Alert.CustomThreadKey CTK
        INNER JOIN @AlertIDMapping M ON CTK.AlertID = M.OldAlertID;

        PRINT 'Updated ' + CAST(@@ROWCOUNT AS NVARCHAR(20)) + ' CustomThreadKey record(s)'

        UPDATE NL
        SET NL.AlertID = M.NewAlertID
        FROM Alert.NotificationLog NL
        INNER JOIN @AlertIDMapping M ON NL.AlertID = M.OldAlertID;

        PRINT 'Updated ' + CAST(@@ROWCOUNT AS NVARCHAR(20)) + ' NotificationLog record(s)'

        -- Insert new rows with new AlertIDs (can't update identity column directly)
        BEGIN TRY
            SET IDENTITY_INSERT Alert.ActiveAlerts ON;

            INSERT INTO Alert.ActiveAlerts (
                AlertID,
                InstanceID,
                Priority,
                AlertType,
                AlertKey,
                FirstMessage,
                LastMessage,
                TriggerDate,
                UpdatedDate,
                FirstNotification,
                LastNotification,
                UpdateCount,
                ResolvedCount,
                NotificationCount,
                FailedNotificationCount,
                IsAcknowledged,
                IsResolved,
                ResolvedDate,
                IsBlackout,
                Escalated,
                DeEscalated,
                Notes,
                RuleID,
                GroupID,
                AcknowledgedDate
            )
            SELECT 
                NewAlertID,
                InstanceID,
                Priority,
                AlertType,
                AlertKey,
                FirstMessage,
                LastMessage,
                TriggerDate,
                UpdatedDate,
                FirstNotification,
                LastNotification,
                UpdateCount,
                ResolvedCount,
                NotificationCount,
                FailedNotificationCount,
                IsAcknowledged,
                IsResolved,
                ResolvedDate,
                IsBlackout,
                Escalated,
                DeEscalated,
                Notes,
                RuleID,
                GroupID,
                AcknowledgedDate
            FROM @AlertDataToMove;

            PRINT 'Inserted ' + CAST(@@ROWCOUNT AS NVARCHAR(20)) + ' alert(s) with new IDs'
        END TRY
        BEGIN CATCH
            BEGIN TRY
                SET IDENTITY_INSERT Alert.ActiveAlerts OFF;
            END TRY
            BEGIN CATCH
                -- Ignore cleanup errors here and rethrow the original failure below
            END CATCH;
            IF @@TRANCOUNT > 0
                ROLLBACK;
            THROW;
        END CATCH

        SET IDENTITY_INSERT Alert.ActiveAlerts OFF;

        -- Update the new starting ID to account for renumbered alerts
        SELECT @NewStartID = MAX(NewAlertID) + 1 FROM @AlertIDMapping;
    END

    -- Reseed the identity to the current value immediately before the next free ID
    -- so the next inserted row gets @NewStartID
    DECLARE @ReseedValue BIGINT = @NewStartID - 1;
    DECLARE @ReseedSQL NVARCHAR(500) = N'DBCC CHECKIDENT (''Alert.ActiveAlerts'', RESEED, ' + CAST(@ReseedValue AS NVARCHAR(20)) + N')';
    PRINT 'Reseeding Alert.ActiveAlerts identity current value to ' + CAST(@ReseedValue AS NVARCHAR(20))
        + ' so the next generated AlertID will be ' + CAST(@NewStartID AS NVARCHAR(20));
    EXEC sp_executesql @ReseedSQL;

    COMMIT;

    PRINT 'Identity seed fix completed successfully'
END
ELSE
BEGIN
    -- No issues found, commit the lock transaction
    COMMIT;
    PRINT 'No identity seed issues detected - no action required'
END
