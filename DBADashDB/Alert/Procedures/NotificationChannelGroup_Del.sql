CREATE PROC Alert.NotificationChannelGroup_Del(
	@GroupID INT
)
AS
SET XACT_ABORT ON
IF @GroupID = 0
BEGIN
	RAISERROR('The default notification channel group cannot be deleted.',16,1)
	RETURN
END
BEGIN TRAN
/* Channels and rules revert to the default group (0) when their group is deleted */
UPDATE Alert.NotificationChannel SET GroupID = 0 WHERE GroupID = @GroupID

/* 
	Close existing alerts associated with group.  Avoids duplicates in Alert.ActiveAlerts that might be possible if we updated them to default group.
	Closed alerts are not updated as they are historical records.
*/
DECLARE @AlertIDs BigIDs
INSERT INTO @AlertIDs(ID)
SELECT AlertID
FROM Alert.ActiveAlerts
WHERE GroupID = @GroupID

EXEC  Alert.ClosedAlerts_Add @AlertIDs= @AlertIDs

/* Remove rules that would become duplicates in the default group, then revert the rest */
DELETE R
FROM Alert.Rules R
WHERE R.GroupID = @GroupID
AND EXISTS(
	SELECT 1
	FROM Alert.Rules R0
	WHERE R0.GroupID = 0
	AND R0.RuleHash = R.RuleHash
)
UPDATE Alert.Rules SET GroupID = 0 WHERE GroupID = @GroupID


DELETE FROM Alert.NotificationChannelGroup WHERE GroupID = @GroupID

COMMIT
