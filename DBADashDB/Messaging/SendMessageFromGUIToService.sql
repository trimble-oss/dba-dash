CREATE PROC Messaging.SendMessageFromGUIToService(
	@DBADashAgentID INT,
	@Payload VARBINARY(MAX),
	@ConversationGroup UNIQUEIDENTIFIER,
	@InitDlgHandle UNIQUEIDENTIFIER,
	@Lifetime INT
)
AS
/*
	Used by the GUI to communicate with the service.

	Privileged messages (kill session, plan forcing, schedule changes, XE, job execution, custom tools...) need a role
	as well as Messaging - the same roles the GUI checks.  The check is made here as anyone with Messaging can call this
	proc directly with a message of their own making.  db_owner can send any message.

	This proc runs as the caller so IS_ROLEMEMBER checks the caller.  The send needs permissions the caller doesn't
	have, so it's made by MessagingInternal.SendMessage (EXECUTE AS OWNER, not granted to anyone), which this proc
	reaches by ownership chaining.

	The payload is a message serialized as JSON and gzip compressed (MessageBase.Serialize).  It must be ASCII (other
	characters are escaped), property names must be identifiers (letters, digits and underscores) and can't be repeated,
	ignoring case, so this proc and the service read the same message.  The service checks the same things before it
	processes or relays a message.
*/
SET NOCOUNT ON
DECLARE @Json VARCHAR(MAX)
DECLARE @MessageType NVARCHAR(4000)
DECLARE @ProcedureRole SYSNAME
DECLARE @Roles NVARCHAR(4000)

BEGIN TRY
	SET @Json = CAST(DECOMPRESS(@Payload) AS VARCHAR(MAX))
END TRY
BEGIN CATCH
	SET @Json = NULL
END CATCH

IF @Json IS NULL
	OR @Json COLLATE Latin1_General_BIN2 LIKE '%[^' + CHAR(9) + CHAR(10) + CHAR(13) + ' -~]%'
	OR ISJSON(@Json) = 0
BEGIN
	RAISERROR('Invalid message.',11,1);
	RETURN
END

/*
	Identifiers only: = ignores trailing spaces, so "EmbeddedScript " would match EmbeddedScript here while the service
	ignores it as an unknown property.  LIKE doesn't ignore them.
*/
IF EXISTS(
	SELECT 1
	FROM OPENJSON(@Json)
	WHERE [key] COLLATE Latin1_General_BIN2 LIKE N'%[^A-Za-z0-9_]%'
	OR [key] = N''
	)
BEGIN
	RAISERROR('Invalid message.  Property names must be identifiers.',11,1);
	RETURN
END

IF EXISTS(
	SELECT 1
	FROM OPENJSON(@Json)
	GROUP BY UPPER([key])
	HAVING COUNT(*) > 1
	)
BEGIN
	RAISERROR('Invalid message.  Duplicate property.',11,1);
	RETURN
END

/* The message type is the assembly qualified type name - the assembly isn't checked here, the service only accepts message types from its own assembly */
SELECT @MessageType = LTRIM(RTRIM(LEFT(value, CHARINDEX(N',', value + N',') - 1)))
FROM OPENJSON(@Json)
WHERE [key] = N'__type' COLLATE Latin1_General_BIN2
AND type = 1

IF @MessageType IS NULL
BEGIN
	RAISERROR('Invalid message.  Message type not found.',11,1);
	RETURN
END

IF @MessageType = N'DBADash.Messaging.ProcedureExecutionMessage' COLLATE Latin1_General_BIN2
BEGIN
	/*
		Embedded scripts need Messaging only.  Community scripts need CommunityScripts and anything else is a custom proc
		that needs CustomTools, as ProcedureExecutionMessage.IsCommunityProc decides.  Property names are matched ignoring
		case as the service does, and an absent or null schema is dbo.

		Only a number is an embedded script, as the GUI sends it - the service reads an empty string as no embedded
		script.  Names are compared exactly, including their length as = ignores trailing spaces.
	*/
	DECLARE @IsEmbeddedScript BIT, @SchemaName NVARCHAR(MAX), @ProcedureName NVARCHAR(MAX)

	SELECT	@IsEmbeddedScript = MAX(CASE WHEN UPPER([key]) = N'EMBEDDEDSCRIPT' AND type = 2 THEN 1 ELSE 0 END),
			@SchemaName = MAX(CASE WHEN UPPER([key]) = N'SCHEMANAME' AND type <> 0 THEN value END),
			@ProcedureName = MAX(CASE WHEN UPPER([key]) = N'PROCEDURENAME' AND type <> 0 THEN value END)
	FROM OPENJSON(@Json)

	SET @SchemaName = ISNULL(@SchemaName, N'dbo')

	SELECT @ProcedureRole = CASE
					WHEN @IsEmbeddedScript = 1 THEN NULL
					WHEN @SchemaName = N'dbo' COLLATE Latin1_General_BIN2 AND DATALENGTH(@SchemaName) = DATALENGTH(N'dbo')
						AND EXISTS(
							SELECT 1
							FROM (VALUES(N'sp_WhoIsActive'),(N'sp_Blitz'),(N'sp_BlitzFirst'),(N'sp_BlitzIndex'),(N'sp_BlitzCache'),(N'sp_BlitzWho'),
									(N'sp_LogHunter'),(N'sp_PressureDetector'),(N'sp_BlitzLock'),(N'sp_BlitzBackups'),(N'sp_HumanEvents'),
									(N'sp_HealthParser'),(N'sp_QuickieStore'),(N'sp_HumanEventsBlockViewer'),(N'sp_SrvPermissions'),
									(N'sp_DBPermissions'),(N'sp_IndexCleanup')
								) C(ProcedureName)
							WHERE C.ProcedureName = @ProcedureName COLLATE Latin1_General_BIN2
							AND DATALENGTH(C.ProcedureName) = DATALENGTH(@ProcedureName)
							) THEN N'CommunityScripts'
					ELSE N'CustomTools' END
END

/*
	Message types the service accepts and the roles that can send them - any one of the roles is enough.  A NULL role
	needs Messaging only.  A message type that isn't listed is rejected, so a new message type must be added here
	(DBADash.Test checks every message type is listed).
*/
DECLARE @Permissions TABLE(
	MessageType NVARCHAR(4000) COLLATE Latin1_General_BIN2 NOT NULL,
	RoleName SYSNAME NULL
)
INSERT INTO @Permissions(MessageType,RoleName)
VALUES	(N'DBADash.Messaging.AgentJobExecutionMessage',N'AllowJobExecution'),
		(N'DBADash.Messaging.CancellationMessage',NULL),
		(N'DBADash.Messaging.CollectionMessage',NULL),
		(N'DBADash.Messaging.CollectionPauseMessage',N'AllowScheduleChanges'),
		(N'DBADash.Messaging.CollectionScheduleGetMessage',N'AllowScheduleChanges'),
		(N'DBADash.Messaging.CollectionScheduleUpdateMessage',N'AllowScheduleChanges'),
		(N'DBADash.Messaging.DeadlockStatementPlansMessage',NULL),
		(N'DBADash.Messaging.DecipherWaitResourceMessage',NULL),
		(N'DBADash.Messaging.FlushPlanMessage',N'AllowPlanForcing'),
		(N'DBADash.Messaging.HeartbeatMessage',NULL),
		(N'DBADash.Messaging.KillSessionMessage',N'AllowKillSession'),
		(N'DBADash.Messaging.MultiCollectionMessage',NULL),
		(N'DBADash.Messaging.PlanCollectionMessage',NULL),
		(N'DBADash.Messaging.ProcedureExecutionMessage',@ProcedureRole),
		(N'DBADash.Messaging.QueryPlanCollectionMessage',NULL),
		(N'DBADash.Messaging.QueryStatsPlanMessage',NULL),
		(N'DBADash.Messaging.QueryStoreForcedPlansMessage',NULL),
		(N'DBADash.Messaging.QueryStorePlanForcingMessage',N'AllowPlanForcing'),
		(N'DBADash.Messaging.QueryStoreTopQueriesMessage',NULL),
		(N'DBADash.Messaging.XEObjectCatalogMessage',N'AdhocXE'),
		(N'DBADash.Messaging.XESessionControlMessage',N'ManageXE'),
		(N'DBADash.Messaging.XESessionListMessage',N'ManageXE'),
		(N'DBADash.Messaging.XESessionListMessage',N'WatchXE'),
		(N'DBADash.Messaging.XESessionScriptMessage',N'ManageXE'),
		(N'DBADash.Messaging.XESessionScriptMessage',N'WatchXE'),
		(N'DBADash.Messaging.XETraceMessage',N'AdhocXE'),
		(N'DBADash.Messaging.XETraceStopMessage',N'AdhocXE'),
		(N'DBADash.Messaging.XEViewTargetDataMessage',N'WatchXE'),
		(N'DBADash.Messaging.XEWatchSessionMessage',N'WatchXE')

IF NOT EXISTS(
	SELECT 1
	FROM @Permissions
	WHERE MessageType = @MessageType
	)
BEGIN
	SET @MessageType = LEFT(@MessageType,200)
	RAISERROR('Invalid message.  Unknown message type %s.',11,1,@MessageType);
	RETURN
END

IF ISNULL(IS_ROLEMEMBER('db_owner'),0) = 0
	AND NOT EXISTS(
		SELECT 1
		FROM @Permissions
		WHERE MessageType = @MessageType
		AND (RoleName IS NULL OR IS_ROLEMEMBER(RoleName) = 1)
	)
BEGIN
	/* STUFF/FOR XML rather than STRING_AGG: the repository database supports SQL 2016 */
	SET @Roles = STUFF((
				SELECT N' or ' + RoleName
				FROM @Permissions
				WHERE MessageType = @MessageType
				ORDER BY RoleName
				FOR XML PATH(''),TYPE
				).value('.','NVARCHAR(4000)'),1,4,N'')

	SET @MessageType = PARSENAME(@MessageType,1)
	RAISERROR('You don''t have permission to send %s.  It requires the %s role.',11,1,@MessageType,@Roles);
	RETURN
END

EXEC MessagingInternal.SendMessage @DBADashAgentID = @DBADashAgentID,
                                   @Payload = @Payload,
                                   @ConversationGroup = @ConversationGroup,
                                   @InitDlgHandle = @InitDlgHandle,
                                   @Lifetime = @Lifetime
