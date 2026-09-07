/*
	The set of deadlocks matching a report's instance, date and filter selection.

	Factored out so that dbo.DeadlockSummary_Get and dbo.DeadlockCharts_Get cannot drift apart: the two
	present the same data as grids and as charts, and a filter that means one thing on one and something
	else on the other would be worse than having no charts at all.

	Inline (RETURNS TABLE ... AS RETURN) so it folds into the caller's plan rather than materialising.

	The filters below are "@p IS NULL OR col = @p" pairs, which are opaque to the optimizer: it must
	build one plan that works for every combination, so none of them can drive an index.  The callers
	therefore run the statement that reads this with OPTION(RECOMPILE), which lets the branches for the
	parameters that are NULL on this execution be simplified away and the rest be costed on the values
	actually supplied - notably a signature drill-down, which has IX_Deadlocks_Signature to seek.
	@FromDate and @ToDate are the exception and are required, because EventTime, which they bound, is
	what the table is partitioned and clustered on - see the WHERE clause.

	A deadlock is in scope when one of its processes matches all of the process level filters supplied -
	the same process, not one each, which is what someone filtering on an application and a login means.
	Values are matched exactly rather than by pattern, so a value drilled through from a grouped result
	finds precisely the rows it was counted from.
*/
CREATE FUNCTION dbo.DeadlockScope
(
	@InstanceIDs IDs READONLY,
	@InstanceID INT,
	/*	Required, both of them.  A NULL selects nothing rather than meaning "no bound" - the callers
		turn one into an open bound before calling.  See the WHERE clause for why. */
	@FromDate DATETIME2(3),
	@ToDate DATETIME2(3),
	@SignatureBin BINARY(8),
	@ApplicationName NVARCHAR(128),
	@DatabaseName sysname,
	@LoginName sysname,
	@HostName sysname,
	@ProcedureName NVARCHAR(776),
	@ObjectName NVARCHAR(776),
	/*	Narrows the other process filters to the victim - "deadlocks where this application was rolled
		back" rather than "deadlocks it took part in".  On its own it selects nothing, because every
		deadlock has a victim; the callers apply it to the processes they *show* and *group*, which is
		where it is visible.  See dbo.DeadlockSummary_Get. */
	@VictimsOnly BIT,
	/*	Passed in rather than derived here so the caller states it once.  1 when any process filter has a
		value; when 0 the EXISTS below is skipped entirely, because a truncated graph can reach us with
		no process rows at all and dropping those silently on an unfiltered report would be wrong.
		@VictimsOnly deliberately does not set it - it qualifies the other filters rather than being one. */
	@HasProcessFilter BIT
)
RETURNS TABLE
AS
RETURN
SELECT	D.InstanceID,
		D.EventTime,
		D.DeadlockHash,
		D.Signature,
		D.ProcessCount,
		D.VictimCount,
		D.ResourceCount,
		D.IsParallel
FROM dbo.Deadlocks D
/*	Written as a plain range rather than as the "IS NULL OR" pair the other filters use, because this is
	the one predicate that can drive the access path: dbo.Deadlocks is partitioned on EventTime and
	clustered on (InstanceID, EventTime, DeadlockHash), so a range here eliminates partitions and seeks
	once per instance in @InstanceIDs.  In the OR form it is a residual and the whole table is read,
	which is why the parameters are required.  @ToDate is exclusive - dbo.DeadlockCharts_Get relies on
	that to drill from one time bucket into the deadlocks it counted. */
WHERE D.EventTime >= @FromDate
AND D.EventTime < @ToDate
AND (@SignatureBin IS NULL OR D.Signature = @SignatureBin)
AND EXISTS(	SELECT 1
			FROM @InstanceIDs T
			WHERE T.ID = D.InstanceID
			AND (T.ID = @InstanceID OR @InstanceID IS NULL)
			)
AND (@HasProcessFilter = 0 OR EXISTS(
			SELECT 1
			FROM dbo.DeadlockProcesses P
			LEFT JOIN dbo.Databases DB ON DB.DatabaseID = P.DatabaseID
			WHERE P.InstanceID = D.InstanceID
			AND P.EventTime = D.EventTime
			AND P.DeadlockHash = D.DeadlockHash
			AND (@ApplicationName IS NULL OR P.ClientApp = @ApplicationName)
			AND (@DatabaseName IS NULL OR DB.name = @DatabaseName)
			AND (@LoginName IS NULL OR P.LoginName = @LoginName)
			AND (@HostName IS NULL OR P.HostName = @HostName)
			AND (@ProcedureName IS NULL OR P.ProcedureName = @ProcedureName)
			AND (@VictimsOnly = 0 OR P.IsVictim = 1)
			))
AND (@ObjectName IS NULL OR EXISTS(
			SELECT 1
			FROM dbo.DeadlockResources R
			WHERE R.InstanceID = D.InstanceID
			AND R.EventTime = D.EventTime
			AND R.DeadlockHash = D.DeadlockHash
			AND R.ObjectName = @ObjectName
			))
