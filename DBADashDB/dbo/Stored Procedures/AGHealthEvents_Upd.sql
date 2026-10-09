CREATE PROC dbo.AGHealthEvents_Upd(
	@AGHealthEvents dbo.AGHealthEvents READONLY,
	@InstanceID INT,
	@SnapshotDate DATETIME2(2)
)
AS
/*	Events read from the AlwaysOn_health extended events session.  The collection follows the event file with a cursor
	so each run normally sends only new events, but a cursor reset or a failed write can send events again - those are
	discarded here on (InstanceID, EventTime, EventHash).

	Role changes are derived from the newly inserted availability_replica_state_change events:
	A replica is in a stable role when its state is PRIMARY_NORMAL or SECONDARY_NORMAL (GLOBAL_PRIMARY or FORWARDER for a
	distributed AG).  When a stable state is reached and the role differs from the replica's previous stable role, a role
	change is recorded.  It happened between the first state change after the previous stable state (when it left its
	role) and the event that reached the new one.
	An instance restart (PRIMARY_NORMAL => NOT_AVAILABLE => RESOLVING_NORMAL => PRIMARY_NORMAL) isn't a role change.
*/
SET NOCOUNT ON
SET XACT_ABORT ON
DECLARE @Ref VARCHAR(30)='AGHealthEvents'
/* Retention for the events table - 365 days if it's not configured, no limit if it's set to 0 or NULL (as data retention treats it) */
DECLARE @RetentionDays INT = 365
SELECT @RetentionDays = RetentionDays
FROM dbo.DataRetention
WHERE SchemaName = 'dbo'
AND TableName = 'AvailabilityGroupHealthEvents'
DECLARE @RetainFrom DATETIME2(3) = CASE WHEN @RetentionDays > 0 THEN DATEADD(d,-@RetentionDays,SYSUTCDATETIME()) ELSE CAST('00010101' AS DATETIME2(3)) END

CREATE TABLE #NewStateChanges(
	EventTime DATETIME2(3) NOT NULL,
	EventHash BINARY(32) NOT NULL,
	EventName NVARCHAR(60) NOT NULL,
	AvailabilityGroupName NVARCHAR(128) NULL,
	ReplicaName NVARCHAR(256) NULL,
	replica_id UNIQUEIDENTIFIER NULL
)

BEGIN TRAN

INSERT INTO dbo.AvailabilityGroupHealthEvents(
	InstanceID,
	EventTime,
	EventName,
	EventHash,
	group_id,
	AvailabilityGroupName,
	replica_id,
	ReplicaName,
	PreviousState,
	CurrentState,
	DDLAction,
	DDLPhase,
	ErrorNumber,
	Details,
	Component,
	database_id,
	group_database_id,
	DatabaseName
)
OUTPUT inserted.EventTime, inserted.EventHash, inserted.EventName, inserted.AvailabilityGroupName, inserted.ReplicaName, inserted.replica_id
INTO #NewStateChanges(EventTime,EventHash,EventName,AvailabilityGroupName,ReplicaName,replica_id)
SELECT	@InstanceID,
		E.EventTime,
		E.EventName,
		E.EventHash,
		E.group_id,
		/* hadr_db_partner_set_sync_state only has IDs */
		ISNULL(E.AvailabilityGroupName,AG.name),
		E.replica_id,
		ISNULL(E.ReplicaName,AR.replica_server_name),
		E.PreviousState,
		E.CurrentState,
		E.DDLAction,
		E.DDLPhase,
		E.ErrorNumber,
		E.Details,
		E.Component,
		E.database_id,
		E.group_database_id,
		D.name
FROM @AGHealthEvents E
/* group_id and replica_id are the same on every replica, so another monitored replica can name them if this instance can't */
OUTER APPLY(SELECT TOP(1) AG.name FROM dbo.AvailabilityGroups AG WHERE E.AvailabilityGroupName IS NULL AND AG.group_id = E.group_id ORDER BY CASE WHEN AG.InstanceID = @InstanceID THEN 0 ELSE 1 END) AG
OUTER APPLY(SELECT TOP(1) AR.replica_server_name FROM dbo.AvailabilityReplicas AR WHERE E.ReplicaName IS NULL AND AR.replica_id = E.replica_id ORDER BY CASE WHEN AR.InstanceID = @InstanceID THEN 0 ELSE 1 END) AR
/*	Database name from group_database_id, which is stable and the same on every replica.  database_id can be reused after a
	database is dropped, so it's only used for an event without a group_database_id - otherwise a back-filled event could be
	given the name of a database created since.
*/
OUTER APPLY(
	SELECT TOP(1) D.name
	FROM dbo.DatabasesHADR H
	JOIN dbo.Databases D ON D.DatabaseID = H.DatabaseID
	WHERE H.group_database_id = E.group_database_id
	ORDER BY CASE WHEN H.InstanceID = @InstanceID THEN 0 ELSE 1 END
) GD
OUTER APPLY(SELECT TOP(1) D.name FROM dbo.Databases D WHERE E.group_database_id IS NULL AND E.database_id IS NOT NULL AND D.InstanceID = @InstanceID AND D.database_id = E.database_id AND D.IsActive = 1) DID
OUTER APPLY(SELECT ISNULL(GD.name,DID.name) AS name) D
WHERE NOT EXISTS(
		SELECT 1
		FROM dbo.AvailabilityGroupHealthEvents X WITH(UPDLOCK,HOLDLOCK)
		WHERE X.InstanceID = @InstanceID
		AND X.EventTime = E.EventTime
		AND X.EventHash = E.EventHash
		)
/* Events older than the retention period aren't imported.  The first collection reads the whole AlwaysOn_health file set,
   which can go back years, and partition cleanup only removes a partition once all of it is past retention - so old events
   imported into the first partition would otherwise be kept long after they should have been purged. */
AND E.EventTime >= @RetainFrom

/*	Only state changes are needed to derive role changes, and only for this instance's own replica.  If the local
	replica isn't known (an agent that doesn't report is_local), every replica reported is used.
*/
DELETE #NewStateChanges
WHERE EventName <> N'availability_replica_state_change'
OR AvailabilityGroupName IS NULL
OR ReplicaName IS NULL

IF EXISTS(
	SELECT 1
	FROM dbo.AvailabilityReplicas AR
	WHERE AR.InstanceID = @InstanceID
	AND AR.is_local = 1
	)
BEGIN
	DELETE N
	FROM #NewStateChanges N
	WHERE NOT EXISTS(
		SELECT 1
		FROM dbo.AvailabilityReplicas AR
		WHERE AR.InstanceID = @InstanceID
		AND AR.is_local = 1
		AND (AR.replica_id = N.replica_id OR AR.replica_server_name = N.ReplicaName)
		)
END

/* Most runs bring no new state changes */
IF EXISTS(SELECT 1 FROM #NewStateChanges)
BEGIN
	/* State change history for the replicas with new state changes */
	SELECT	HE.EventTime,
			HE.EventHash,
			HE.group_id,
			HE.AvailabilityGroupName,
			HE.replica_id,
			HE.ReplicaName,
			CASE WHEN HE.PreviousState IN(N'PRIMARY_NORMAL',N'GLOBAL_PRIMARY') THEN 1 WHEN HE.PreviousState IN(N'SECONDARY_NORMAL',N'FORWARDER') THEN 2 END AS PreviousStableRole,
			CASE WHEN HE.CurrentState IN(N'PRIMARY_NORMAL',N'GLOBAL_PRIMARY') THEN 1 WHEN HE.CurrentState IN(N'SECONDARY_NORMAL',N'FORWARDER') THEN 2 END AS CurrentStableRole,
			CAST(CASE WHEN EXISTS(SELECT 1 FROM #NewStateChanges N WHERE N.EventTime = HE.EventTime AND N.EventHash = HE.EventHash) THEN 1 ELSE 0 END AS BIT) AS IsNew
	INTO #StateChanges
	FROM dbo.AvailabilityGroupHealthEvents HE
	WHERE HE.InstanceID = @InstanceID
	AND HE.EventName = N'availability_replica_state_change'
	AND HE.EventTime <= (SELECT MAX(EventTime) FROM #NewStateChanges)
	AND EXISTS(
		SELECT 1
		FROM #NewStateChanges N
		WHERE N.AvailabilityGroupName = HE.AvailabilityGroupName
		AND N.ReplicaName = HE.ReplicaName
		)

	CREATE CLUSTERED INDEX IX_StateChanges ON #StateChanges(AvailabilityGroupName,ReplicaName,EventTime)

	INSERT INTO dbo.AvailabilityGroupRoleChanges(
		InstanceID,
		group_id,
		replica_id,
		AvailabilityGroupName,
		ReplicaServerName,
		PreviousRole,
		NewRole,
		ChangedAfter,
		ChangedBefore,
		Source
	)
	SELECT	@InstanceID,
			ISNULL(E.group_id,AG.group_id),
			E.replica_id,
			E.AvailabilityGroupName,
			E.ReplicaName,
			Calc.PreviousRole,
			E.CurrentStableRole,
			FirstChange.EventTime,
			E.EventTime,
			'XE'
	FROM #StateChanges E
	/* The previous stable state for the replica */
	OUTER APPLY(
		SELECT TOP(1) P.EventTime,
				P.CurrentStableRole
		FROM #StateChanges P
		WHERE P.AvailabilityGroupName = E.AvailabilityGroupName
		AND P.ReplicaName = E.ReplicaName
		/* Names can be reused when an AG is dropped and recreated, so match the IDs where both events have them */
		AND (P.group_id = E.group_id OR P.group_id IS NULL OR E.group_id IS NULL)
		AND (P.replica_id = E.replica_id OR P.replica_id IS NULL OR E.replica_id IS NULL)
		AND P.CurrentStableRole IS NOT NULL
		AND P.EventTime < E.EventTime
		ORDER BY P.EventTime DESC
	) Prev
	/* The first state change after the previous stable state - when the replica left its previous role */
	OUTER APPLY(
		SELECT TOP(1) F.EventTime,
				F.PreviousStableRole
		FROM #StateChanges F
		WHERE F.AvailabilityGroupName = E.AvailabilityGroupName
		AND F.ReplicaName = E.ReplicaName
		AND (F.group_id = E.group_id OR F.group_id IS NULL OR E.group_id IS NULL)
		AND (F.replica_id = E.replica_id OR F.replica_id IS NULL OR E.replica_id IS NULL)
		AND F.EventTime <= E.EventTime
		AND (F.EventTime > Prev.EventTime OR Prev.EventTime IS NULL)
		ORDER BY F.EventTime
	) FirstChange
	/* Where the replica was coming from: the state it left, or its last stable state if it left one that wasn't stable (NOT_AVAILABLE after a restart) */
	OUTER APPLY(SELECT ISNULL(FirstChange.PreviousStableRole,Prev.CurrentStableRole) AS PreviousRole) Calc
	OUTER APPLY(
		SELECT TOP(1) AG.group_id
		FROM dbo.AvailabilityGroups AG
		WHERE AG.InstanceID = @InstanceID
		AND AG.name = E.AvailabilityGroupName
	) AG
	WHERE E.IsNew = 1
	AND E.CurrentStableRole IS NOT NULL
	AND Calc.PreviousRole IS NOT NULL
	AND Calc.PreviousRole <> E.CurrentStableRole
	AND NOT EXISTS(
		SELECT 1
		FROM dbo.AvailabilityGroupRoleChanges RC
		WHERE RC.InstanceID = @InstanceID
		AND RC.ChangedBefore = E.EventTime
		AND RC.Source = 'XE'
		AND RC.AvailabilityGroupName = E.AvailabilityGroupName
		AND RC.ReplicaServerName = E.ReplicaName
		)
END

EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,
							 @Reference = @Ref,
							 @SnapshotDate = @SnapshotDate

COMMIT
