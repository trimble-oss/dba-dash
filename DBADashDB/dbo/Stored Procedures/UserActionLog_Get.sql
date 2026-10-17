CREATE PROC dbo.UserActionLog_Get(
	@InstanceIDs IDs READONLY,
	@Days INT = 7
)
AS
/*	Actions on the instances, plus actions that weren't to a single instance: those sent to a service that collects
	the instances (e.g. a change to its schedule, which applies to each of its instances), and those with no service
	at all (e.g. repository changes). */
WITH Agents AS (
	SELECT DISTINCT I.CollectAgentID
	FROM dbo.Instances I
	WHERE EXISTS(SELECT 1 FROM @InstanceIDs T WHERE T.ID = I.InstanceID)
)
SELECT	L.log_date,
		L.user_name,
		L.login_name,
		L.action_type,
		I.InstanceID,
		I.InstanceGroupName,
		L.database_name,
		CASE WHEN A.DBADashAgentID IS NOT NULL THEN CONCAT(A.AgentHostName,'\',A.AgentServiceName) END AS service_name,
		L.details,
		L.status,
		L.MessageGroupID
FROM dbo.UserActionLog L
LEFT JOIN dbo.DBADashAgent A ON L.CollectAgentID = A.DBADashAgentID
LEFT JOIN dbo.Instances I ON L.InstanceID = I.InstanceID
WHERE L.log_date >= DATEADD(d,-@Days,SYSUTCDATETIME())
AND (EXISTS(SELECT 1 FROM @InstanceIDs T WHERE T.ID = L.InstanceID)
	OR (L.InstanceID IS NULL AND L.CollectAgentID IS NULL)
	OR (L.InstanceID IS NULL AND EXISTS(SELECT 1 FROM Agents AG WHERE AG.CollectAgentID = L.CollectAgentID))
	)
ORDER BY L.log_date DESC
