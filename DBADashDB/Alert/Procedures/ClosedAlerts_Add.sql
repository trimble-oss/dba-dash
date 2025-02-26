CREATE PROC Alert.ClosedAlerts_Add(
	@AlertIDs BigIDs READONLY,
    @AlertID BIGINT=NULL
)
AS
SET XACT_ABORT ON
BEGIN TRAN
INSERT INTO Alert.ClosedAlerts(
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
           Notes,
           RuleID,
           AcknowledgedDate)
SELECT AlertID,
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
           Notes,
           RuleID,
           AcknowledgedDate
FROM Alert.ActiveAlerts AA
WHERE EXISTS(
            SELECT 1 
            FROM @AlertIDs T
            WHERE T.ID = AA.AlertID
            AND @AlertID IS NULL
            UNION ALL
            SELECT 1
            WHERE AA.AlertID = @AlertID
            AND @AlertID IS NOT NULL
            )

DELETE CTK 
FROM Alert.CustomThreadKey CTK
WHERE EXISTS(
            SELECT 1 
            FROM @AlertIDs T
            WHERE T.ID = CTK.AlertID
            AND @AlertID IS NULL
            UNION ALL
            SELECT 1
            WHERE CTK.AlertID = @AlertID
            AND @AlertID IS NOT NULL
            )

DELETE NL 
FROM Alert.NotificationLog NL
WHERE EXISTS(
            SELECT 1 
            FROM @AlertIDs T
            WHERE T.ID = NL.AlertID
            AND @AlertID IS NULL
            UNION ALL
            SELECT 1
            WHERE NL.AlertID = @AlertID
            AND @AlertID IS NOT NULL
            )

DELETE AA
FROM Alert.ActiveAlerts AA
WHERE EXISTS(
            SELECT 1 
            FROM @AlertIDs T
            WHERE T.ID = AA.AlertID
            AND @AlertID IS NULL
            UNION ALL
            SELECT 1
            WHERE AA.AlertID = @AlertID
            AND @AlertID IS NOT NULL
            )

COMMIT