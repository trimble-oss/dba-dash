CREATE PROC Messaging.SendMessageFromGUIToService(
	@DBADashAgentID INT,
	@Payload VARBINARY(MAX),
	@ConversationGroup UNIQUEIDENTIFIER, 
	@InitDlgHandle UNIQUEIDENTIFIER,
	@Lifetime INT
)
AS
/* 
	Used by the GUI to communicate with the service 
*/
DECLARE @Service NVARCHAR(128) = CONCAT('//dbadash.com/DBADashService/ComsTarget',@DBADashAgentID)
DECLARE @SQL NVARCHAR(MAX)
IF NOT EXISTS(SELECT 1 FROM sys.services WHERE name = @Service)
BEGIN
	RAISERROR('Target service doesn''t exist. Enable messaging to allow communication to service.',11,1);
	RETURN
END

SET @SQL = N'
BEGIN TRANSACTION;

BEGIN DIALOG @InitDlgHandle 
	FROM SERVICE [//dbadash.com/DBADashService/ComsInit]
    TO SERVICE @Service
	ON CONTRACT [//dbadash.com/DBADashService/ComsContract] 
	WITH ENCRYPTION = OFF,RELATED_CONVERSATION_GROUP = @ConversationGroup,LIFETIME = @LifeTime;

SEND ON CONVERSATION @InitDlgHandle
        MESSAGE TYPE
        [//dbadash.com/DBADashService/Send]
        (@Payload);

COMMIT TRANSACTION;'

EXEC sp_executesql @SQL,N'@Service NVARCHAR(128),@Payload VARBINARY(MAX), @InitDlgHandle UNIQUEIDENTIFIER,@ConversationGroup UNIQUEIDENTIFIER,@Lifetime INT',@Service,@Payload,@InitDlgHandle,@ConversationGroup,@Lifetime

GO