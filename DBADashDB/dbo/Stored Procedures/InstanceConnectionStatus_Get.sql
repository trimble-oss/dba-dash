CREATE PROC dbo.InstanceConnectionStatus_Get(
	@ConnectionIDs NVARCHAR(MAX) /* JSON array of ConnectionIDs */
)
AS
/*
	For connections about to be added from the GUI: whether each instance is already in the repository, marked deleted,
	and which service collects it - so the GUI can warn before an instance is collected twice.
*/
SELECT I.ConnectionID,
	I.InstanceID,
	CAST(ISNULL(I.IsActive,0) AS BIT) AS IsActive,
	I.CollectAgentID,
	A.AgentHostName,
	A.AgentServiceName
FROM OPENJSON(@ConnectionIDs) C
JOIN dbo.Instances I ON I.ConnectionID = C.value
LEFT JOIN dbo.DBADashAgent A ON A.DBADashAgentID = I.CollectAgentID
