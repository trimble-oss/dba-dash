CREATE PROC dbo.DBADashAgent_Upd(	
	@AgentHostName NVARCHAR(16),
	@AgentVersion VARCHAR(30),
	@AgentServiceName NVARCHAR(256),
	@AgentPath NVARCHAR(260),
	@ServiceSQSQueueUrl NVARCHAR(256)=NULL,
	@AgentIdentifier CHAR(22)=NULL,
	@S3Path NVARCHAR(256)=NULL,
	@MessagingEnabled BIT=0,
	@AllowedScripts VARCHAR(MAX)=NULL,
	@DBADashAgentID INT OUT
)
AS
DECLARE @UpdateAgent BIT = 0

SELECT @DBADashAgentID = DBADashAgentID,
	@UpdateAgent = CASE WHEN 
						EXISTS(
								SELECT @AgentPath, @AgentVersion, @ServiceSQSQueueUrl, @AgentIdentifier,@S3Path,@MessagingEnabled,@AllowedScripts
								EXCEPT
								SELECT AgentPath, AgentVersion, ServiceSQSQueueUrl, AgentIdentifier, S3Path, MessagingEnabled, AllowedScripts
								)
					THEN 1 ELSE 0 END
FROM dbo.DBADashAgent
WHERE AgentHostName = @AgentHostName
AND AgentServiceName = @AgentServiceName

IF @DBADashAgentID IS NULL
BEGIN

	INSERT INTO dbo.DBADashAgent(AgentHostName,AgentServiceName,AgentVersion,AgentPath,ServiceSQSQueueUrl,AgentIdentifier,S3Path,MessagingEnabled,AllowedScripts)
	SELECT @AgentHostName,@AgentServiceName,@AgentVersion,@AgentPath,@ServiceSQSQueueUrl,ISNULL(@AgentIdentifier,LEFT(CONCAT('Temp.',REPLACE(NEWID(),'-','')),22)), @S3Path, @MessagingEnabled,@AllowedScripts
	WHERE NOT EXISTS(SELECT 1 
				FROM dbo.DBADashAgent WITH(UPDLOCK,HOLDLOCK) 
				WHERE AgentHostName = @AgentHostName 
				AND AgentServiceName = @AgentServiceName
				)

	/* 
		Not using SCOPE_IDENTITY as there is potential for no rows to be inserted if two processes attempt the insert
	*/
	SELECT @DBADashAgentID = DBADashAgentID
	FROM dbo.DBADashAgent
	WHERE AgentHostName = @AgentHostName
	AND AgentServiceName = @AgentServiceName
END
IF @UpdateAgent=1
BEGIN
	UPDATE dbo.DBADashAgent
		SET AgentPath = @AgentPath,
			AgentVersion = @AgentVersion,
			ServiceSQSQueueUrl = @ServiceSQSQueueUrl,
			AgentIdentifier = ISNULL(@AgentIdentifier,LEFT(CONCAT('Temp.',REPLACE(NEWID(),'-','')),22)),
			S3Path = @S3Path,
			MessagingEnabled = @MessagingEnabled,
			AllowedScripts = @AllowedScripts
	WHERE DBADashAgentID = @DBADashAgentID;
END