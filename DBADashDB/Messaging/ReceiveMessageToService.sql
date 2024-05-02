CREATE PROC Messaging.ReceiveMessageToService(
    @DBADashAgentID INT
)
AS
DECLARE @SQL NVARCHAR(MAX)
DECLARE @Queue NVARCHAR(128) = CONCAT('DBADashServiceTargetQueue',@DBADashAgentID)
DECLARE @RecvReqDlgHandle UNIQUEIDENTIFIER;

SET @SQL = CONCAT(N'
    WAITFOR
    ( RECEIVE TOP(1)
			conversation_handle,
			message_body,
			message_type_name
      FROM ',@Queue,'
    ), TIMEOUT 30000;')

EXEC sp_executesql @SQL

GO