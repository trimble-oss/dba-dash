CREATE PROC dbo.DBADashAgent_Get(
	@DBADashAgentID  INT
)
AS
SELECT DBADashAgentID,
       AgentHostName,
       AgentServiceName,
       AgentVersion,
       AgentPath,
       MessagingEnabled,
       KillSessionEnabled,
       PlanForcingEnabled,
       AdhocXEMaxDurationSeconds,
       ServiceSQSQueueUrl,
       S3Path,
       AllowedScripts,
       AllowedCustomProcs,
       ManageXESessions,
       WatchXESessions
FROM dbo.DBADashAgent
WHERE DBADashAgentID = @DBADashAgentID
