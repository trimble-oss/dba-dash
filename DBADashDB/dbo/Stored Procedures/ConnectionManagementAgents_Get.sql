CREATE PROC dbo.ConnectionManagementAgents_Get
AS
/*
	DBA Dash services that accept source connections added from the GUI.  A new connection has no instance yet, so the
	GUI chooses the service that will collect it.  Messages for a service go through the service that imports its data
	(ImportAgentID) - itself, unless it writes to S3 for another service to import, when the message is relayed.  That's
	taken from the instances it collects, so a remote service is only listed once it has collected something.
*/
SELECT A.DBADashAgentID AS CollectAgentID,
	ISNULL(Import.ImportAgentID, A.DBADashAgentID) AS ImportAgentID,
	A.AgentHostName,
	A.AgentServiceName,
	A.AgentVersion,
	A.AgentPath,
	ISNULL(Instances.InstanceCount, 0) AS InstanceCount,
	CAST(CASE WHEN A.PublicKey IS NULL THEN 0 ELSE 1 END AS BIT) AS HasPublicKey
FROM dbo.DBADashAgent A
OUTER APPLY(
	SELECT COUNT(*) AS InstanceCount
	FROM dbo.Instances I
	WHERE I.CollectAgentID = A.DBADashAgentID
	AND I.IsActive = 1
	) Instances
OUTER APPLY(
	SELECT TOP(1) I.ImportAgentID
	FROM dbo.Instances I
	WHERE I.CollectAgentID = A.DBADashAgentID
	AND I.IsActive = 1
	AND I.ImportAgentID IS NOT NULL
	GROUP BY I.ImportAgentID
	ORDER BY COUNT(*) DESC
	) Import
WHERE A.MessagingEnabled = 1
AND A.ConnectionChangesEnabled = 1
/* The importing service relays the message, so it needs messaging enabled too */
AND (Import.ImportAgentID IS NULL
	OR Import.ImportAgentID = A.DBADashAgentID
	OR EXISTS(SELECT 1 FROM dbo.DBADashAgent IA WHERE IA.DBADashAgentID = Import.ImportAgentID AND IA.MessagingEnabled = 1)
	)
ORDER BY A.AgentHostName, A.AgentServiceName
