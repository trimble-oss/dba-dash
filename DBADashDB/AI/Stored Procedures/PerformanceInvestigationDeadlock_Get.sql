/*
	One deadlock signature in a window, in enough detail to say what to fix: the processes and
	resources of its latest @Occurrences occurrences.  Run by the AI service when the model asks about
	a signature it was given in the analysis; see DBADashAI PerformanceInvestigation.

	Statements and input buffers are cut short - the model needs to recognise the statement, not
	read the batch - and the graph itself is not returned.

		0  Occurrences in the window
		1  Processes
		2  Resources
*/
CREATE PROC AI.PerformanceInvestigationDeadlock_Get(
	@InstanceID INT,
	@FromDate DATETIME2(3),
	@ToDate DATETIME2(3),
	@Signature VARCHAR(18),
	@Occurrences INT = 3
)
AS
SET NOCOUNT ON

DECLARE @SignatureBin BINARY(8) = TRY_CONVERT(BINARY(8), @Signature, 1)

CREATE TABLE #Deadlocks(EventTime DATETIME2(3) NOT NULL, DeadlockHash BINARY(16) NOT NULL, PRIMARY KEY(EventTime, DeadlockHash))

INSERT INTO #Deadlocks(EventTime, DeadlockHash)
SELECT TOP (@Occurrences) D.EventTime, D.DeadlockHash
FROM dbo.Deadlocks D
WHERE D.InstanceID = @InstanceID
AND D.Signature = @SignatureBin
AND D.EventTime >= @FromDate
AND D.EventTime < @ToDate
ORDER BY D.EventTime DESC

SELECT COUNT(*) AS Occurrences
FROM dbo.Deadlocks D
WHERE D.InstanceID = @InstanceID
AND D.Signature = @SignatureBin
AND D.EventTime >= @FromDate
AND D.EventTime < @ToDate

SELECT	P.EventTime AS EventTimeUTC,
		P.ProcessIndex,
		P.IsVictim,
		DB.name AS DatabaseName,
		P.ProcedureName,
		LEFT(P.StatementText, 800) AS StatementText,
		LEFT(P.InputBuffer, 300) AS InputBuffer,
		P.ClientApp,
		P.IsolationLevel,
		P.LockMode,
		P.WaitResource,
		P.WaitTimeMs,
		P.TransactionName,
		P.TransactionCount
FROM #Deadlocks D
JOIN dbo.DeadlockProcesses P ON P.InstanceID = @InstanceID AND P.EventTime = D.EventTime AND P.DeadlockHash = D.DeadlockHash
LEFT JOIN dbo.Databases DB ON DB.DatabaseID = P.DatabaseID
ORDER BY P.EventTime DESC, P.ProcessIndex

SELECT	R.EventTime AS EventTimeUTC,
		R.ResourceType,
		DB.name AS DatabaseName,
		R.ObjectName,
		R.IndexName,
		R.LockMode,
		R.OwnerModes,
		R.WaiterModes
FROM #Deadlocks D
JOIN dbo.DeadlockResources R ON R.InstanceID = @InstanceID AND R.EventTime = D.EventTime AND R.DeadlockHash = D.DeadlockHash
LEFT JOIN dbo.Databases DB ON DB.DatabaseID = R.DatabaseID
ORDER BY R.EventTime DESC, R.ResourceIndex
