/*
	Individual deadlocks, newest first, for MCP clients to pick one from.  AI.Deadlocks_Get counts them from a
	performance counter; this lists the ones the Deadlocks collection stored, with the victim's details, and the
	key (InstanceID, EventTime, DeadlockHash) that AI.DeadlockGraph_Get takes to fetch one graph.

	The graph's retention is shorter than the shredded rows', so GraphAvailable says whether there is still one to
	fetch.
*/
CREATE PROC AI.DeadlockList_Get(
	@MaxRows INT = 50,
	@InstanceFilter NVARCHAR(256) = NULL,
	@HoursBack INT = 24,
	/* When the window ends, for looking at a past incident.  NULL ends it now, as before. */
	@ToDate DATETIME2(3) = NULL
)
AS
SET NOCOUNT ON
SELECT TOP (@MaxRows)
	D.InstanceID,
	I.InstanceDisplayName,
	D.EventTime,
	CONVERT(VARCHAR(34), D.DeadlockHash, 1) AS DeadlockHash,
	CONVERT(VARCHAR(18), D.Signature, 1) AS Signature,
	D.ProcessCount,
	D.VictimCount,
	D.ResourceCount,
	D.IsParallel,
	CAST(CASE WHEN EXISTS(
			SELECT 1
			FROM dbo.DeadlockXml X
			WHERE X.InstanceID = D.InstanceID
			AND X.EventTime = D.EventTime
			AND X.DeadlockHash = D.DeadlockHash
			AND X.DeadlockXmlCompressed IS NOT NULL
		) THEN 1 ELSE 0 END AS BIT) AS GraphAvailable,
	V.VictimDatabase,
	V.VictimApp,
	V.VictimLogin,
	V.VictimHost,
	V.VictimProcedure,
	V.VictimStatement
FROM dbo.Deadlocks D
JOIN dbo.Instances I ON I.InstanceID = D.InstanceID
OUTER APPLY(
	SELECT TOP(1)
		DB.name AS VictimDatabase,
		P.ClientApp AS VictimApp,
		P.LoginName AS VictimLogin,
		P.HostName AS VictimHost,
		P.ProcedureName AS VictimProcedure,
		LEFT(P.StatementText, 300) AS VictimStatement
	FROM dbo.DeadlockProcesses P
	LEFT JOIN dbo.Databases DB ON DB.DatabaseID = P.DatabaseID
	WHERE P.InstanceID = D.InstanceID
	AND P.EventTime = D.EventTime
	AND P.DeadlockHash = D.DeadlockHash
	AND P.IsVictim = 1
	ORDER BY P.ProcessIndex
) V
WHERE D.EventTime >= DATEADD(HOUR, -@HoursBack, ISNULL(@ToDate, SYSUTCDATETIME())) AND (@ToDate IS NULL OR D.EventTime < @ToDate)
AND I.IsActive = 1
AND (@InstanceFilter IS NULL OR I.InstanceDisplayName LIKE @InstanceFilter + '%')
ORDER BY D.EventTime DESC
