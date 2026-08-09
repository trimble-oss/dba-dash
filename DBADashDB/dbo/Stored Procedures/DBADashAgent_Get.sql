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
       ServiceSQSQueueUrl,
       S3Path,
       AllowedScripts,
       AllowedCustomProcs
FROM dbo.DBADashAgent
WHERE DBADashAgentID = @DBADashAgentID
