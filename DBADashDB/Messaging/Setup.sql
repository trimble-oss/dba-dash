CREATE PROC Messaging.Setup(
	@DBADashAgentID INT,
	@Enable BIT=1
)
AS
DECLARE @SQL NVARCHAR(MAX)
IF  ServerProperty('EngineEdition') = 5
BEGIN
	IF @Enable = 1
	BEGIN
		RAISERROR('Service Broker is not supported on SQL Azure DB',11,1)
	END
	RETURN
END
IF NOT EXISTS(SELECT 1 
			FROM dbo.DBADashAgent
			WHERE DBADashAgentID = @DBADashAgentID)
BEGIN
	RAISERROR('Invalid @DBADashAgentID',11,1)
	RETURN
END
IF @Enable=1
BEGIN
	/* Enable service broker if not enabled.  Note: This will kick users out of the database. */
	IF NOT EXISTS(SELECT 1 
					FROM sys.databases 
					WHERE database_id = DB_ID()
					AND is_broker_enabled=1
	) 
	BEGIN
		PRINT 'Enable service broker'
		SET @SQL = CONCAT('ALTER DATABASE ', QUOTENAME(DB_NAME()), ' SET ENABLE_BROKER WITH ROLLBACK IMMEDIATE')
		EXEC sp_executesql @SQL
	END

	/* Create message type, contract & initiator queue if they don't exist */
	SET @SQL = 'IF NOT EXISTS(SELECT 1 FROM sys.service_message_types WHERE name = ''//dbadash.com/DBADashService/Send'')
	BEGIN
		CREATE MESSAGE TYPE [//dbadash.com/DBADashService/Send];
	END
	IF NOT EXISTS(SELECT 1 FROM sys.service_message_types WHERE name = ''//dbadash.com/DBADashService/Reply'')
	BEGIN
		CREATE MESSAGE TYPE [//dbadash.com/DBADashService/Reply];
	END
	IF NOT EXISTS(SELECT 1 FROM sys.service_contracts WHERE name = ''//dbadash.com/DBADashService/ComsContract'')
	BEGIN
		CREATE CONTRACT [//dbadash.com/DBADashService/ComsContract]           
			( [//dbadash.com/DBADashService/Send]  SENT BY INITIATOR,           
			  [//dbadash.com/DBADashService/Reply] SENT BY TARGET  
			) ;  
	END
	IF NOT EXISTS(SELECT 1 FROM sys.service_queues WHERE name = ''DBADashServiceInitiatorQueue'')
	BEGIN
		CREATE QUEUE DBADashServiceInitiatorQueue
	END
	IF NOT EXISTS(SELECT 1 FROM sys.services WHERE name = ''//dbadash.com/DBADashService/ComsInit'')
	BEGIN
		CREATE SERVICE [//dbadash.com/DBADashService/ComsInit]
			   ON QUEUE DBADashServiceInitiatorQueue

	END'

	EXEC sp_executesql @SQL

	/* 
		Create target queue & service for specified agent 
		If multiple services are used, each gets it's own target queue * service
	*/
	SET @SQL = CONCAT('IF NOT EXISTS(SELECT 1 FROM sys.service_queues WHERE name = ''DBADashServiceTargetQueue',@DBADashAgentID ,''')
	BEGIN
		CREATE QUEUE DBADashServiceTargetQueue',@DBADashAgentID ,'
	END
	IF NOT EXISTS(SELECT 1 FROM sys.services WHERE name = ''//dbadash.com/DBADashService/ComsTarget',@DBADashAgentID ,''')
	BEGIN
		CREATE SERVICE [//dbadash.com/DBADashService/ComsTarget',@DBADashAgentID ,']
				ON QUEUE DBADashServiceTargetQueue',@DBADashAgentID,'
				([//dbadash.com/DBADashService/ComsContract])

	END')
	EXEC sp_executesql @SQL
END
ELSE
BEGIN
	SET @SQL = CONCAT('IF EXISTS(SELECT 1 FROM sys.services WHERE name = ''//dbadash.com/DBADashService/ComsTarget',@DBADashAgentID ,''')
	BEGIN
		DROP SERVICE [//dbadash.com/DBADashService/ComsTarget',@DBADashAgentID ,']
	END
	IF EXISTS(SELECT 1 FROM sys.service_queues WHERE name = ''DBADashServiceTargetQueue',@DBADashAgentID ,''')
	BEGIN
		DROP QUEUE DBADashServiceTargetQueue',@DBADashAgentID ,'
	END')
	EXEC sp_executesql @SQL
END

UPDATE dbo.DBADashAgent
	SET MessagingEnabled = @Enable
WHERE DBADashAgentID = @DBADashAgentID