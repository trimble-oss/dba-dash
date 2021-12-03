CREATE PROC dbo.DBADashAgent_Upd(	
	@AgentHostName NVARCHAR(16),
	@AgentVersion VARCHAR(30),
	@AgentServiceName NVARCHAR(256),
	@AgentPath NVARCHAR(260),
	@DBADashAgentID INT OUT
)
AS
DECLARE @UpdateAgent BIT = 0

SELECT @DBADashAgentID = DBADashAgentID,
	@UpdateAgent = (CASE WHEN @AgentPath <> AgentPath OR @AgentVersion <> AgentVersion THEN 1 ELSE 0 END)
FROM dbo.DBADashAgent
WHERE AgentHostName = @AgentHostName
AND AgentServiceName = @AgentServiceName

IF @DBADashAgentID IS NULL
BEGIN

	INSERT INTO dbo.DBADashAgent(AgentHostName,AgentServiceName,AgentVersion,AgentPath)
	SELECT @AgentHostName,@AgentServiceName,@AgentVersion,@AgentPath
	WHERE NOT EXISTS(SELECT 1 
				FROM dbo.DBADashAgent WITH(UPDLOCK,HOLDLOCK) 
				WHERE AgentHostName = @AgentHostName 
				AND AgentServiceName = @AgentServiceName
				)

	SET @DBADashAgentID = SCOPE_IDENTITY();
END
IF @UpdateAgent=1
BEGIN
	UPDATE dbo.DBADashAgent
		SET AgentPath = @AgentPath,
			AgentVersion = @AgentVersion
	WHERE DBADashAgentID = @DBADashAgentID;
END