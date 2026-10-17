CREATE PROC dbo.DBADashAgentRelay_Get(
	@AgentIdentifier CHAR(22)
)
AS
/*
	Where a remote agent registered to receive messages (SQS queue) and write its results (S3).  Messages relayed to the
	agent use these rather than the ones in the message, so a message can only be relayed to a registered agent's
	queue and its results written to that agent's S3 path.
*/
SELECT ServiceSQSQueueUrl,
       S3Path
FROM dbo.DBADashAgent
WHERE AgentIdentifier = @AgentIdentifier
