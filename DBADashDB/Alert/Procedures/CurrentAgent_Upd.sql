CREATE PROC Alert.CurrentAgent_Upd(
	@DBADashAgentID INT
)
AS
/* 
	Ensure only a single instance of the DBA Dash service is processing alerts at one time
*/
IF NOT EXISTS(SELECT 1 
	FROM Alert.CurrentAgent
	WHERE ID = 1
	)
BEGIN
	INSERT INTO Alert.CurrentAgent(
		ID,
		DBADashAgentID,
		LockAcquired,
		LockUpdated
	)
	SELECT 
		1,
		@DBADashAgentID ,
		SYSUTCDATETIME(),
		SYSUTCDATETIME()
	WHERE NOT EXISTS(SELECT 1 
					FROM Alert.CurrentAgent WITH(UPDLOCK)
					WHERE ID = 1
					)
END

UPDATE Alert.CurrentAgent
	SET DBADashAgentID = @DBADashAgentID,
	LockAcquired = CASE WHEN DBADashAgentID = @DBADashAgentID THEN LockAcquired ELSE SYSUTCDATETIME() END,
	LockUpdated = SYSUTCDATETIME()
WHERE ID = 1
AND (LockUpdated < DATEADD(mi,-10,SYSUTCDATETIME()) /* Lock will expire after 10min allowing another DBA Dash service to take over alert processing */
	OR DBADashAgentID = @DBADashAgentID
	)

SELECT CAST(@@ROWCOUNT AS BIT) AS LockAcquired
