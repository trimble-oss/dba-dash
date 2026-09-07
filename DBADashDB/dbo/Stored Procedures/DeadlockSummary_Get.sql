/*
	The deadlock report, built on the shredded rows the Deadlocks collection stores.

	Four result sets, all filtered the same way, so drilling in narrows every view at once rather than
	swapping one for another:

		0  Signatures    - deadlocks grouped by shape.  The default view: "what deadlocks do we have",
		                   rather than "what deadlocked at 14:07".  Drills into @Signature.
		1  Grouped       - the same set pivoted by application / database / login / host / procedure,
		                   chosen with @GroupBy.  One proc rather than five reports, because the
		                   dimensions are columns on dbo.DeadlockProcesses.
		2  Deadlocks     - one row per occurrence, victim focused.
		3  Participants  - every process in those deadlocks, for when the victim isn't the interesting one.

	Results 2 and 3 share a DeadlockGroup number - sp_BlitzLock's deadlock_group - so a row in one can be
	matched to its rows in the other, and so a reader can see where one deadlock's processes end and the
	next one's begin.  It is assigned per execution, not stored.

	Neither returns the deadlock graph.  Both carry DeadlockHash, which with the instance and event time
	is what dbo.DeadlockGraph_Get fetches one graph by when a reader asks to see it.

	Note dbo.Deadlocks_Get is a different thing: it drives the deadlock markers on the performance
	chart from a performance counter, and needs no collection enabled.  This reads collected graphs.
*/
CREATE PROC dbo.DeadlockSummary_Get(
	@InstanceIDs IDs READONLY,
	@InstanceID INT = NULL,
	@FromDate DATETIME2(3) = NULL,
	@ToDate DATETIME2(3) = NULL,
	/* Drill-down: the "0x..." hex form, as shown in the Signatures result. */
	@Signature VARCHAR(18) = NULL,
	@GroupBy VARCHAR(20) = 'Application',
	/*	Filters.  A deadlock is in scope when one of its processes matches all of the process level
		filters supplied - the same process, not one each, which is what someone filtering on an
		application and a login means.  Matched exactly rather than by pattern, so that a value drilled
		through from the Grouped result finds precisely the rows it was counted from. */
	@ApplicationName NVARCHAR(128) = NULL,
	@DatabaseName sysname = NULL,
	@LoginName sysname = NULL,
	@HostName sysname = NULL,
	@ProcedureName NVARCHAR(776) = NULL,
	@ObjectName NVARCHAR(776) = NULL,
	/*	Restricts the report to the processes that were rolled back: the Grouped and Participants results
		count and list victims only, and the filters above are narrowed to the victim, so an application
		filter becomes "deadlocks this application was rolled back in" rather than "took part in".

		It deliberately does not select deadlocks by itself - every deadlock has a victim, so as a scope
		filter it would exclude nothing, which is what made it look like it did nothing at all. */
	@VictimsOnly BIT = 0
)
AS
SET NOCOUNT ON


/*	Only meaningful for collected data - on-demand rows arrive without the collection ever having run,
	which is the whole reason for that path.

	Scoped to the instances in context, and scoped the same way #Scope is below.  A global test would go
	quiet as soon as any one instance in the estate collected deadlocks, leaving every other instance
	showing an empty grid that reads as "no deadlocks" rather than "not collecting".

	Only when *none* of the instances in context collect: with a mix, the ones that do have data worth
	showing, and suppressing it to advertise the collection would be the wrong trade. */
IF NOT EXISTS(	SELECT 1
				FROM dbo.CollectionDates CD
				JOIN @InstanceIDs T ON T.ID = CD.InstanceID
				WHERE CD.Reference = 'Deadlocks'
				AND (T.ID = @InstanceID OR @InstanceID IS NULL))
BEGIN
	/*	No Url column here, unlike dbo.FailedLogins_Get: there is no deadlocks page on the docs site yet,
		and a link that opens a 404 is worse than no link.  Add one back when the page exists. */
	SELECT 'Enable the Deadlocks collection in the service config tool to see data here, or use Trigger Collection to read this instance''s system_health session now.' AS Message
	RETURN
END

/*	dbo.DeadlockScope requires a date range - it is what gives the read of dbo.Deadlocks partition
	elimination and a seek, and an optional bound cannot.  NULL keeps its "no bound" meaning by becoming
	the end of the DATETIME2(3) range here rather than being passed through.  The GUI always supplies
	both; this is for the report being run by hand. */
SET @FromDate = ISNULL(@FromDate, '19000101')
SET @ToDate = ISNULL(@ToDate, '99991231 23:59:59.999')

DECLARE @SignatureBin BINARY(8) = CASE WHEN @Signature IS NULL THEN NULL ELSE CONVERT(BINARY(8), @Signature, 1) END

/*	Normalise rather than test for NULL at each use: NULL would make "(@VictimsOnly = 0 OR IsVictim = 1)"
	evaluate as victims-only whenever another filter was also set, which is not what a cleared parameter
	should mean. */
SET @VictimsOnly = ISNULL(@VictimsOnly, 0)

DECLARE @HasProcessFilter BIT = CASE WHEN @ApplicationName IS NOT NULL
									OR @DatabaseName IS NOT NULL
									OR @LoginName IS NOT NULL
									OR @HostName IS NOT NULL
									OR @ProcedureName IS NOT NULL
								THEN 1 ELSE 0 END

/*	The deadlocks in scope, resolved once.  Everything below joins to this rather than repeating the
	instance / date / signature filter four times and risking the four results disagreeing. */
CREATE TABLE #Scope(
	InstanceID INT NOT NULL,
	EventTime DATETIME2(3) NOT NULL,
	DeadlockHash BINARY(16) NOT NULL,
	Signature BINARY(8) NULL,
	ProcessCount SMALLINT NULL,
	VictimCount SMALLINT NULL,
	ResourceCount SMALLINT NULL,
	IsParallel BIT NULL,
	/*	A short number for the deadlock, in the order the results present them - sp_BlitzLock's
		deadlock_group.  A deadlock is keyed by instance, event time and a 16 byte hash, none of which
		is something a reader can hold in their head while scanning a grid, and the Participants result
		puts several rows of one deadlock next to several rows of another.  This is what says which rows
		belong together, and it is the same number in both results, so a row in Deadlocks and its
		processes in Participants can be matched up.

		A number rather than sp_BlitzLock's 'Deadlock #1' text because the grid is sortable and text
		would put #10 before #2.  It is assigned per execution and only means anything within one set of
		results - re-running with a different date range renumbers everything. */
	DeadlockGroup INT NULL,
	PRIMARY KEY(InstanceID, EventTime, DeadlockHash)
)

INSERT INTO #Scope(InstanceID, EventTime, DeadlockHash, Signature, ProcessCount, VictimCount, ResourceCount, IsParallel)
SELECT	InstanceID,
		EventTime,
		DeadlockHash,
		Signature,
		ProcessCount,
		VictimCount,
		ResourceCount,
		IsParallel
/*	Shared with dbo.DeadlockCharts_Get so the grids and the charts can never disagree about what is in
	scope.  Materialised into #Scope here because four result sets read it. */
FROM dbo.DeadlockScope(@InstanceIDs, @InstanceID, @FromDate, @ToDate, @SignatureBin, @ApplicationName,
					@DatabaseName, @LoginName, @HostName, @ProcedureName, @ObjectName, @VictimsOnly,
					@HasProcessFilter)
/*	The function is a set of optional filters, so without this the plan has to serve every combination
	of them at once and none can drive an index - see the note there.  It also gives @InstanceIDs a real
	row count, which a table variable does not have on a SQL 2016 repository.  The report is run by a
	person looking at a screen, so one compile per execution is nothing against reading the table. */
OPTION(RECOMPILE)

/*	Number the deadlocks in the order the Deadlocks and Participants results present them, so #1 is the
	top row of both rather than a number the reader has to hunt for.  ROW_NUMBER rather than DENSE_RANK
	because #Scope already holds one row per deadlock.  The instance and hash are in the ordering only to
	break ties: two deadlocks can share an event time, and without them the numbering would be arbitrary
	between runs. */
;WITH Numbered AS (
	SELECT	DeadlockGroup,
			ROW_NUMBER() OVER (ORDER BY EventTime DESC, InstanceID, DeadlockHash) AS RN
	FROM #Scope
)
UPDATE Numbered SET DeadlockGroup = RN

/*	The children, materialised so that every result set below reads one place regardless of where the
	data came from.  In the collected case this is a copy of the rows for the deadlocks in scope, which
	is work worth doing anyway: three of the four result sets read them. */
CREATE TABLE #Processes(
	InstanceID INT NOT NULL,
	EventTime DATETIME2(3) NOT NULL,
	DeadlockHash BINARY(16) NOT NULL,
	ProcessIndex SMALLINT NOT NULL,
	IsVictim BIT NOT NULL,
	DatabaseID INT NULL,
	SPID INT NULL,
	Ecid INT NULL,
	LoginName sysname NULL,
	HostName sysname NULL,
	ClientApp sysname NULL,
	ProcedureName NVARCHAR(776) NULL,
	StatementText NVARCHAR(MAX) NULL,
	IsolationLevel VARCHAR(50) NULL,
	LockMode VARCHAR(20) NULL,
	WaitResource NVARCHAR(512) NULL,
	WaitTimeMs BIGINT NULL,
	LogUsed BIGINT NULL,
	TransactionName NVARCHAR(128) NULL,
	Priority SMALLINT NULL,
	LastBatchStarted DATETIME2(3) NULL,
	LastBatchCompleted DATETIME2(3) NULL,
	LastTransactionStarted DATETIME2(3) NULL,
	Status VARCHAR(30) NULL,
	TransactionCount INT NULL,
	HostPid INT NULL,
	InputBuffer NVARCHAR(MAX) NULL,
	ClientOption1 INT NULL,
	ClientOption2 INT NULL,
	PRIMARY KEY(InstanceID, EventTime, DeadlockHash, ProcessIndex)
)

CREATE TABLE #Resources(
	InstanceID INT NOT NULL,
	EventTime DATETIME2(3) NOT NULL,
	DeadlockHash BINARY(16) NOT NULL,
	ResourceIndex SMALLINT NOT NULL,
	ResourceType VARCHAR(50) NOT NULL,
	DatabaseID INT NULL,
	ObjectName NVARCHAR(776) NULL,
	IndexName sysname NULL,
	LockMode VARCHAR(20) NULL,
	OwnerModes VARCHAR(200) NULL,
	WaiterModes VARCHAR(200) NULL,
	OwnerCount SMALLINT NULL,
	WaiterCount SMALLINT NULL,
	IsParallelismResource BIT NULL,
	PRIMARY KEY(InstanceID, EventTime, DeadlockHash, ResourceIndex)
)

INSERT INTO #Processes(InstanceID, EventTime, DeadlockHash, ProcessIndex, IsVictim, DatabaseID, SPID, Ecid,
	LoginName, HostName, ClientApp, ProcedureName, StatementText, IsolationLevel, LockMode, WaitResource,
	WaitTimeMs, LogUsed, TransactionName, Priority, LastBatchStarted, LastBatchCompleted,
	LastTransactionStarted, Status, TransactionCount, HostPid, InputBuffer, ClientOption1, ClientOption2)
SELECT	P.InstanceID, P.EventTime, P.DeadlockHash, P.ProcessIndex, P.IsVictim, P.DatabaseID, P.SPID, P.Ecid,
		P.LoginName, P.HostName, P.ClientApp, P.ProcedureName, P.StatementText, P.IsolationLevel, P.LockMode,
		P.WaitResource, P.WaitTimeMs, P.LogUsed, P.TransactionName, P.Priority, P.LastBatchStarted,
		P.LastBatchCompleted, P.LastTransactionStarted, P.Status, P.TransactionCount, P.HostPid,
		P.InputBuffer, P.ClientOption1, P.ClientOption2
FROM dbo.DeadlockProcesses P
JOIN #Scope S ON S.InstanceID = P.InstanceID AND S.EventTime = P.EventTime AND S.DeadlockHash = P.DeadlockHash

INSERT INTO #Resources(InstanceID, EventTime, DeadlockHash, ResourceIndex, ResourceType, DatabaseID,
	ObjectName, IndexName, LockMode, OwnerModes, WaiterModes, OwnerCount, WaiterCount, IsParallelismResource)
SELECT	R.InstanceID, R.EventTime, R.DeadlockHash, R.ResourceIndex, R.ResourceType, R.DatabaseID,
		R.ObjectName, R.IndexName, R.LockMode, R.OwnerModes, R.WaiterModes, R.OwnerCount, R.WaiterCount,
		R.IsParallelismResource
FROM dbo.DeadlockResources R
JOIN #Scope S ON S.InstanceID = R.InstanceID AND S.EventTime = R.EventTime AND S.DeadlockHash = R.DeadlockHash

/*	0 - Signatures.  A deadlock that happens two hundred times is one problem, so this is what the
	report opens on.  AI.DeadlockAnalysis is matched on the same BINARY(8) value, so a pattern that has
	already been explained says so without a second lookup.

	The object and module names are aggregated across the whole signature rather than per occurrence -
	applying them before the grouping would split one signature into a row per distinct set of names. */
SELECT	CONVERT(VARCHAR(18), A.Signature, 1) AS Signature,
		A.Occurrences,
		A.FirstSeen,
		A.LastSeen,
		A.Instances,
		A.Victims,
		A.IsParallel,
		Objects.ObjectNames,
		Modules.ProcedureNames,
		CASE WHEN EXISTS(SELECT 1 FROM AI.DeadlockAnalysis AN WHERE AN.Signature = A.Signature)
			THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS HasAnalysis
FROM (
	SELECT	S.Signature,
			COUNT(*) AS Occurrences,
			MIN(S.EventTime) AS FirstSeen,
			MAX(S.EventTime) AS LastSeen,
			COUNT(DISTINCT S.InstanceID) AS Instances,
			SUM(CAST(S.VictimCount AS INT)) AS Victims,
			MAX(CAST(S.IsParallel AS INT)) AS IsParallel
	FROM #Scope S
	GROUP BY S.Signature
	) A
/*	STUFF/FOR XML rather than STRING_AGG: the repository database supports SQL 2016 (see the same note
	in dbo.TagReport_Get).  Capped at five names so a wide parallel deadlock can't fill the cell. */
OUTER APPLY(
	SELECT STUFF((SELECT ', ' + X.ObjectName
					FROM (
						SELECT DISTINCT TOP (5) R.ObjectName
						FROM #Scope S2
						JOIN #Resources R ON R.InstanceID = S2.InstanceID AND R.EventTime = S2.EventTime AND R.DeadlockHash = S2.DeadlockHash
						WHERE S2.Signature = A.Signature
						AND R.ObjectName IS NOT NULL
						ORDER BY R.ObjectName
						) X
					ORDER BY X.ObjectName
					FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,2,'') AS ObjectNames
	) Objects
OUTER APPLY(
	SELECT STUFF((SELECT ', ' + X.ProcedureName
					FROM (
						SELECT DISTINCT TOP (5) P.ProcedureName
						FROM #Scope S3
						JOIN #Processes P ON P.InstanceID = S3.InstanceID AND P.EventTime = S3.EventTime AND P.DeadlockHash = S3.DeadlockHash
						WHERE S3.Signature = A.Signature
						AND P.ProcedureName IS NOT NULL
						ORDER BY P.ProcedureName
						) X
					ORDER BY X.ProcedureName
					FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,2,'') AS ProcedureNames
	) Modules
ORDER BY A.Occurrences DESC, A.LastSeen DESC

/*	1 - Grouped.  Counted at two granularities: a deadlock with two processes from the same
	application is one deadlock involving that application, but two processes.  The inner grouping
	collapses to one row per (value, deadlock), so the outer COUNT(*) is a distinct deadlock count -
	which COUNT(DISTINCT) cannot express over a composite key. */
SELECT	ISNULL(G.RawValue, '(none)') AS GroupValue,
		COUNT(*) AS Deadlocks,
		SUM(G.VictimProcesses) AS VictimProcesses,
		SUM(G.Processes) AS Processes,
		MIN(G.EventTime) AS FirstSeen,
		MAX(G.EventTime) AS LastSeen,
		/*	Drill-down carriers.  Only the column for the selected dimension is populated; the link maps
			all five, and the framework skips the nulls - which is how one static column-to-parameter map
			targets whichever filter the picker is currently grouping by.  The raw value is used rather
			than the displayed one so a '(none)' row drills to "no extra filter" instead of searching for
			a literal '(none)'. */
		CASE WHEN @GroupBy = 'Application' THEN G.RawValue END AS FilterApplication,
		CASE WHEN @GroupBy = 'Database' THEN G.RawValue END AS FilterDatabase,
		CASE WHEN @GroupBy = 'Login' THEN G.RawValue END AS FilterLogin,
		CASE WHEN @GroupBy = 'Host' THEN G.RawValue END AS FilterHost,
		CASE WHEN @GroupBy = 'Procedure' THEN G.RawValue END AS FilterProcedure
FROM (
	SELECT	CASE @GroupBy
				WHEN 'Application' THEN P.ClientApp
				WHEN 'Database' THEN DB.name
				WHEN 'Login' THEN P.LoginName
				WHEN 'Host' THEN P.HostName
				WHEN 'Procedure' THEN P.ProcedureName
			END AS RawValue,
			S.InstanceID,
			S.EventTime,
			S.DeadlockHash,
			COUNT(*) AS Processes,
			SUM(CASE WHEN P.IsVictim = 1 THEN 1 ELSE 0 END) AS VictimProcesses
	FROM #Scope S
	JOIN #Processes P ON P.InstanceID = S.InstanceID AND P.EventTime = S.EventTime AND P.DeadlockHash = S.DeadlockHash
									AND (@VictimsOnly = 0 OR P.IsVictim = 1) /* group the rolled back processes only */
	LEFT JOIN dbo.Databases DB ON DB.DatabaseID = P.DatabaseID
	GROUP BY CASE @GroupBy
				WHEN 'Application' THEN P.ClientApp
				WHEN 'Database' THEN DB.name
				WHEN 'Login' THEN P.LoginName
				WHEN 'Host' THEN P.HostName
				WHEN 'Procedure' THEN P.ProcedureName
			END,
			S.InstanceID,
			S.EventTime,
			S.DeadlockHash
	) G
GROUP BY G.RawValue
ORDER BY Deadlocks DESC, GroupValue

/*	2 - Deadlocks.  One row per occurrence.  Victim focused because the rolled back statement is what
	the person looking at this was told about; result 3 has the rest.  A parallel deadlock can name
	more than one victim, hence the aggregate rather than a join to a single row. */
SELECT	S.DeadlockGroup,
		S.InstanceID,
		I.InstanceDisplayName,
		S.EventTime,
		CONVERT(VARCHAR(18), S.Signature, 1) AS Signature,
		S.ProcessCount,
		S.VictimCount,
		/*	Derived, not stored - it is arithmetic on two columns we already have.  Note it counts
			process entries, and a parallel query contributes one per worker thread, so on a parallel
			deadlock this is threads that were not rolled back rather than sessions that survived.
			Sessions below is the figure to read in that case. */
		CAST(S.ProcessCount AS INT) - CAST(S.VictimCount AS INT) AS Survivors,
		Sessions.Sessions,
		S.ResourceCount,
		S.IsParallel,
		Victim.ProcedureName AS VictimProcedure,
		Victim.StatementText AS VictimStatement,
		Victim.ClientApp AS VictimApp,
		Victim.LoginName AS VictimLogin,
		Victim.HostName AS VictimHost,
		Victim.DatabaseName AS VictimDatabase,
		Objects.ObjectNames,
		/*	The key the viewer fetches the graph with, rather than the graph itself - see
			dbo.DeadlockGraph_Get.  A graph is by far the largest thing stored per deadlock, and
			returning one per row would put every graph in the grid's memory to serve the one the
			reader clicks. */
		S.DeadlockHash
FROM #Scope S
JOIN dbo.Instances I ON I.InstanceID = S.InstanceID
/*	The first victim by process order rather than a concatenation of all of them: a deadlock names one
	victim in all but the parallel case, and one readable statement is more use than several run
	together.  VictimCount says when there were more, and the Participants result has them. */
OUTER APPLY(
	SELECT TOP (1)
			P.ProcedureName,
			P.StatementText,
			P.ClientApp,
			P.LoginName,
			P.HostName,
			DB.name AS DatabaseName
	FROM #Processes P
	LEFT JOIN dbo.Databases DB ON DB.DatabaseID = P.DatabaseID
	WHERE P.InstanceID = S.InstanceID
	AND P.EventTime = S.EventTime
	AND P.DeadlockHash = S.DeadlockHash
	AND P.IsVictim = 1
	ORDER BY P.ProcessIndex
	) Victim
/*	Distinct spids rather than process rows: for a parallel deadlock the process-list is one entry per
	worker thread, so this is what answers "how many sessions were involved". */
OUTER APPLY(
	SELECT COUNT(DISTINCT P.SPID) AS Sessions
	FROM #Processes P
	WHERE P.InstanceID = S.InstanceID
	AND P.EventTime = S.EventTime
	AND P.DeadlockHash = S.DeadlockHash
	) Sessions
OUTER APPLY(
	SELECT STUFF((SELECT ', ' + X2.ObjectName
					FROM (
						SELECT DISTINCT TOP (5) R.ObjectName
						FROM #Resources R
						WHERE R.InstanceID = S.InstanceID
						AND R.EventTime = S.EventTime
						AND R.DeadlockHash = S.DeadlockHash
						AND R.ObjectName IS NOT NULL
						ORDER BY R.ObjectName
						) X2
					ORDER BY X2.ObjectName
					FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,2,'') AS ObjectNames
	) Objects
ORDER BY S.EventTime DESC

/*	3 - Participants.  Every process in the deadlocks above, victim or not - the report that answers
	"what else was involved", which a victim focused list cannot.

	Several rows per deadlock, so DeadlockGroup leads: it is what separates one deadlock's processes
	from the next one's, and it matches the number the Deadlocks result gave the same deadlock. */
SELECT	S.DeadlockGroup,
		S.InstanceID,
		I.InstanceDisplayName,
		S.EventTime,
		/* Carried here too so the graph can be opened from a participant row, not just from result 2. */
		S.DeadlockHash,
		CONVERT(VARCHAR(18), S.Signature, 1) AS Signature,
		P.ProcessIndex,
		P.IsVictim,
		DB.name AS DatabaseName,
		P.ProcedureName,
		P.StatementText,
		P.ClientApp,
		P.LoginName,
		P.HostName,
		P.SPID,
		P.Ecid,
		P.LockMode,
		P.WaitResource,
		P.WaitTimeMs,
		P.LogUsed,
		P.IsolationLevel,
		P.TransactionName,
		P.TransactionCount,
		P.Priority,
		P.Status,
		P.HostPid,
		P.LastTransactionStarted,
		P.LastBatchStarted,
		P.LastBatchCompleted,
		P.InputBuffer,
		/*	The session SET options and the inherited database options, decoded from the two bitmasks
			the same way sp_BlitzLock does.  Worth reading rather than skipping past: whether the
			deadlocking session had IMPLICIT_TRANSACTIONS, XACT_ABORT or ARITHABORT on is often the
			difference between a deadlock in production and a statement that runs clean by hand. */
		CO.ClientOption1Description AS ClientOptions1,
		CO.ClientOption2Description AS ClientOptions2,
		/*	The raw masks kept alongside, hidden in the grid.  They carry bits SQL Server does not name,
			and they are what to check a decode against if one is ever doubted. */
		P.ClientOption1,
		P.ClientOption2
FROM #Scope S
JOIN dbo.Instances I ON I.InstanceID = S.InstanceID
JOIN #Processes P ON P.InstanceID = S.InstanceID AND P.EventTime = S.EventTime AND P.DeadlockHash = S.DeadlockHash
								AND (@VictimsOnly = 0 OR P.IsVictim = 1) /* show the rolled back processes only */
LEFT JOIN dbo.Databases DB ON DB.DatabaseID = P.DatabaseID
CROSS APPLY dbo.DecodeClientOptions(P.ClientOption1, P.ClientOption2) CO
ORDER BY S.EventTime DESC, P.ProcessIndex
