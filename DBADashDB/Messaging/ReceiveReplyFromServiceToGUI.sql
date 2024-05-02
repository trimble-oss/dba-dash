CREATE PROC Messaging.ReceiveReplyFromServiceToGUI(
	@ConversationGroupID UNIQUEIDENTIFIER=NULL,
	@Timeout INT=5000
)
AS
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
