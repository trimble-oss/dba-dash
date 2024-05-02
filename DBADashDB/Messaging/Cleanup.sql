CREATE PROC Messaging.Cleanup
AS
IF OBJECT_ID('dbo.DBADashServiceInitiatorQueue') IS NOT NULL
BEGIN
	DECLARE @SQL NVARCHAR(MAX)
	SET @SQL = '
	DECLARE @handle UNIQUEIDENTIFIER
	DECLARE cConversations CURSOR LOCAL FAST_FORWARD READ_ONLY
		FOR
		/* GUI might have disconnected before processing the reply message */
		SELECT conversation_handle 
		FROM [dbo].[DBADashServiceInitiatorQueue] 
		WHERE message_type_name IN(''http://schemas.microsoft.com/SQL/ServiceBroker/EndDialog'',
									''http://schemas.microsoft.com/SQL/ServiceBroker/Error'')
		AND message_enqueue_time < DATEADD(mi,-120,GETDATE())
		UNION ALL
		/* Cleanup old conversations if needed */
		SELECT conversation_handle
		FROM sys.conversation_endpoints
		WHERE far_service LIKE ''//dbadash.com/DBADashService/%''
		AND state IN(''CO'',''ER'')
		AND lifetime < DATEADD(mi,-1440,GETDATE())

	OPEN cConversations
	WHILE 1=1
	BEGIN
		FETCH NEXT FROM cConversations INTO @handle
		IF @@FETCH_STATUS<>0
			BREAK
		PRINT @handle
		END CONVERSATION @handle

	END
	CLOSE cConversations
	DEALLOCATE cConversations'

	EXEC sp_executesql @SQL

END