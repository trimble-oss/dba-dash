CREATE PROC dbo.UserActionLog_Add(
	@user_name NVARCHAR(256),
	@action_type VARCHAR(50),
	@InstanceIDs IDs READONLY, /* Empty for an action that isn't to a single instance */
	@CollectAgentID INT=NULL,
	@database_name NVARCHAR(128)=NULL,
	@details NVARCHAR(MAX)=NULL, /* JSON */
	@status NVARCHAR(MAX)=NULL, /* NULL while a request is outstanding - see UserActionLog_Upd */
	@MessageGroupID UNIQUEIDENTIFIER=NULL
)
AS
INSERT INTO dbo.UserActionLog(user_name,action_type,InstanceID,CollectAgentID,database_name,details,status,MessageGroupID)
SELECT @user_name,@action_type,T.ID,@CollectAgentID,@database_name,@details,ISNULL(@status,'REQUEST'),@MessageGroupID
FROM @InstanceIDs T
UNION ALL
SELECT @user_name,@action_type,NULL,@CollectAgentID,@database_name,@details,ISNULL(@status,'REQUEST'),@MessageGroupID
WHERE NOT EXISTS(SELECT 1 FROM @InstanceIDs)
