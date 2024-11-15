CREATE PROC Alert.ActiveAlertsAck_Upd(
	@AlertIDs BigIDs READONLY,
	@AlertID BIGINT=NULL,
	@IsAcknowledged BIT
)
AS 
/* Toggle the acknowledged status for alerts specified */
UPDATE AA
    SET AA.IsAcknowledged = @IsAcknowledged
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