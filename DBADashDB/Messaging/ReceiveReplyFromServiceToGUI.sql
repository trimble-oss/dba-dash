CREATE PROC Messaging.ReceiveReplyFromServiceToGUI(
	@ConversationGroupID UNIQUEIDENTIFIER=NULL,
	@Timeout INT=5000
)
WITH EXECUTE AS OWNER
AS
/*
	Used by the GUI to receive replies from the service.  Runs as owner as Messaging members don't have RECEIVE on the
	queue - it would allow them to send to the service without the checks in Messaging.SendMessageFromGUIToService.
*/
DECLARE @SQL NVARCHAR(MAX)
SET @SQL = N'
WAITFOR
( RECEIVE TOP(1)
        conversation_handle,
        message_body,
		message_type_name
    FROM DBADashServiceInitiatorQueue
	WHERE conversation_group_id =@ConversationGroupID
), TIMEOUT @Timeout;
'

EXEC sp_executesql @SQL,N'@ConversationGroupID UNIQUEIDENTIFIER,@Timeout INT',@ConversationGroupID, @Timeout


GO
