CREATE PROC dbo.DBADashAgent_Upd(	
	@AgentHostName NVARCHAR(16),
	@AgentVersion VARCHAR(30),
	@AgentServiceName NVARCHAR(256),
	@AgentPath NVARCHAR(260),
	@ServiceSQSQueueUrl NVARCHAR(256)=NULL,
	@AgentIdentifier CHAR(22)=NULL,
	@S3Path NVARCHAR(256)=NULL,
	@MessagingEnabled BIT=0,
	@KillSessionEnabled BIT=0,
	@PlanForcingEnabled BIT=0,
	@AdhocXEMaxDurationSeconds INT=600,
	@AllowedScripts VARCHAR(MAX)=NULL,
	@AllowedCustomProcs NVARCHAR(MAX)=NULL,
	@ManageXESessions NVARCHAR(MAX)=NULL,
	@WatchXESessions NVARCHAR(MAX)=NULL,
	@AdhocXEEnabled BIT=0,
	@ScheduleChangesEnabled BIT=0,
	@DBADashAgentID INT OUT
)
AS
/*
	Called by the service, which is db_owner.  An agent's registration decides where messages are relayed (SQS queue)
	and where results are written (S3 path), so other users can't change it - App has EXECUTE on dbo, and could call
	this directly or through Instance_Upd.
*/
IF ISNULL(IS_ROLEMEMBER('db_owner'),0) = 0
BEGIN
	RAISERROR('db_owner membership is required to register a DBA Dash agent.',11,1);
	RETURN
END

DECLARE @UpdateAgent BIT = 0

SELECT @DBADashAgentID = DBADashAgentID,
	@UpdateAgent = CASE WHEN 
						EXISTS(
								SELECT @AgentPath, @AgentVersion, @ServiceSQSQueueUrl, @AgentIdentifier,@S3Path,@MessagingEnabled,@KillSessionEnabled,@PlanForcingEnabled,@AdhocXEMaxDurationSeconds,@AllowedScripts, @AllowedCustomProcs, @ManageXESessions, @WatchXESessions, @AdhocXEEnabled, @ScheduleChangesEnabled
								EXCEPT
								SELECT AgentPath, AgentVersion, ServiceSQSQueueUrl, AgentIdentifier, S3Path, MessagingEnabled, KillSessionEnabled, PlanForcingEnabled, AdhocXEMaxDurationSeconds, AllowedScripts, AllowedCustomProcs, ManageXESessions, WatchXESessions, AdhocXEEnabled, ScheduleChangesEnabled
								)
					THEN 1 ELSE 0 END
FROM dbo.DBADashAgent
WHERE AgentHostName = @AgentHostName
AND AgentServiceName = @AgentServiceName

IF @DBADashAgentID IS NULL
BEGIN

	INSERT INTO dbo.DBADashAgent(AgentHostName,AgentServiceName,AgentVersion,AgentPath,ServiceSQSQueueUrl,AgentIdentifier,S3Path,MessagingEnabled,KillSessionEnabled,PlanForcingEnabled,AdhocXEMaxDurationSeconds,AllowedScripts, AllowedCustomProcs, ManageXESessions, WatchXESessions, AdhocXEEnabled, ScheduleChangesEnabled)
	SELECT @AgentHostName,@AgentServiceName,@AgentVersion,@AgentPath,@ServiceSQSQueueUrl,ISNULL(@AgentIdentifier,LEFT(CONCAT('Temp.',REPLACE(NEWID(),'-','')),22)), @S3Path, @MessagingEnabled,@KillSessionEnabled,@PlanForcingEnabled,@AdhocXEMaxDurationSeconds,@AllowedScripts,@AllowedCustomProcs, @ManageXESessions, @WatchXESessions, @AdhocXEEnabled, @ScheduleChangesEnabled
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
			KillSessionEnabled = @KillSessionEnabled,
			PlanForcingEnabled = @PlanForcingEnabled,
			AdhocXEMaxDurationSeconds = @AdhocXEMaxDurationSeconds,
			AllowedScripts = @AllowedScripts,
			AllowedCustomProcs = @AllowedCustomProcs,
			ManageXESessions = @ManageXESessions,
			WatchXESessions = @WatchXESessions,
			AdhocXEEnabled = @AdhocXEEnabled,
			ScheduleChangesEnabled = @ScheduleChangesEnabled,
			ModifiedDate = SYSUTCDATETIME()
	WHERE DBADashAgentID = @DBADashAgentID;
END