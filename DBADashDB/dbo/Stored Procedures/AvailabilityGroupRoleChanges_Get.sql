CREATE PROC dbo.AvailabilityGroupRoleChanges_Get(
	@InstanceIDs IDs READONLY,
	@FromDate DATETIME2(3)=NULL, /* UTC.  NULL for the last 30 days */
	@ToDate DATETIME2(3)=NULL, /* UTC.  NULL for now */
	@IncludeOtherReplicas BIT=0 /* Include role changes reported by other monitored replicas of the same availability groups */
)
AS
/*	Role changes come from two sources:
	XE:			AlwaysOn_health events.  Exact times, with the failover type and reason from the events around the change.
	Snapshot:	Comparing AvailabilityReplicas snapshots.  Only shown when there is no XE row for the same change (AlwaysOn_health
				not running, or not yet collected).  The change happened sometime between the two snapshots.
*/
SET NOCOUNT ON
SELECT	RC.InstanceID,
		I.InstanceDisplayName AS Instance,
		RC.AvailabilityGroupName AS [Availability Group],
		RC.ReplicaServerName AS [Replica Server],
		CASE WHEN RC.NewRole = 1 THEN 'Became Primary'
			WHEN RC.PreviousRole = 1 THEN 'No Longer Primary'
			ELSE CONCAT(RC.PreviousRoleDesc,' => ',RC.NewRoleDesc) END AS [Change],
		RC.PreviousRoleDesc AS [Previous Role],
		RC.NewRoleDesc AS [New Role],
		RC.ChangedAfter AS [Changed After],
		RC.ChangedBefore AS [Changed Before],
		HD.HumanDuration AS [Duration],
		Failover.FailoverType AS [Failover Type],
		Failover.Reason,
		RC.Source
FROM dbo.AvailabilityGroupRoleChanges RC
JOIN dbo.Instances I ON I.InstanceID = RC.InstanceID
OUTER APPLY dbo.SecondsToHumanDuration(DATEDIFF(s,RC.ChangedAfter,RC.ChangedBefore)) HD
/* Search window for related AlwaysOn_health events */
OUTER APPLY(
	SELECT	DATEADD(mi,-2,ISNULL(RC.ChangedAfter,RC.ChangedBefore)) AS FromTime,
			DATEADD(s,30,RC.ChangedBefore) AS ToTime
) W
/* AG events reported by any replica: the failover statement (run on the new primary), lease expiry and automatic failover validation */
OUTER APPLY(
	SELECT	MAX(CASE WHEN HE.EventName = N'alwayson_ddl_executed' AND HE.Details LIKE N'%FORCE[_]FAILOVER[_]ALLOW[_]DATA[_]LOSS%' THEN 1 ELSE 0 END) AS IsForced,
			MAX(CASE WHEN HE.EventName = N'alwayson_ddl_executed' AND HE.Details LIKE N'%FAILOVER%' THEN 1 ELSE 0 END) AS IsManual,
			MAX(CASE WHEN HE.EventName = N'availability_group_lease_expired' THEN 1 ELSE 0 END) AS IsLeaseExpired,
			MAX(CASE WHEN HE.EventName = N'availability_replica_automatic_failover_validation' THEN 1 ELSE 0 END) AS IsFailoverValidation,
			NULLIF(MAX(CASE WHEN HE.EventName = N'alwayson_ddl_executed' AND HE.Details LIKE N'%FAILOVER%' THEN HE.Details ELSE N'' END),N'') AS FailoverStatement
	FROM dbo.AvailabilityGroupHealthEvents HE
	WHERE HE.AvailabilityGroupName = RC.AvailabilityGroupName
	AND HE.EventTime >= W.FromTime
	AND HE.EventTime <= W.ToTime
	AND HE.EventName IN(N'alwayson_ddl_executed',N'availability_group_lease_expired',N'availability_replica_automatic_failover_validation')
	AND ISNULL(HE.DDLPhase,N'') <> N'ROLLBACK'
	/*	The same AG name could exist in different clusters, so match on group_id.  An event without one is matched through the
		reporting instance's AG of that name.  If the role change's group isn't known, only its own instance's events are used.
	*/
	AND (	HE.group_id = RC.group_id
			OR (HE.group_id IS NULL AND RC.group_id IS NOT NULL
				AND EXISTS(
					SELECT 1
					FROM dbo.AvailabilityGroups AG
					WHERE AG.InstanceID = HE.InstanceID
					AND AG.name = HE.AvailabilityGroupName
					AND AG.group_id = RC.group_id
					)
				)
			OR (RC.group_id IS NULL AND HE.InstanceID = RC.InstanceID)
		)
) AGE
/*	Instance level events on any monitored replica of the AG - the cause is usually on the old primary.
	sp_server_diagnostics health check errors, the replica stopping, the HADR manager going offline and HADR errors.
*/
OUTER APPLY(
	SELECT TOP(1)	CASE HE.EventName
						WHEN N'sp_server_diagnostics_component_result' THEN CONCAT('Health check ',HE.Component,' ',HE.CurrentState,' on ',RI.InstanceDisplayName)
						WHEN N'availability_replica_state' THEN CONCAT('Replica stopping on ',RI.InstanceDisplayName)
						WHEN N'availability_replica_manager_state_change' THEN CONCAT('HADR manager ',HE.CurrentState,' on ',RI.InstanceDisplayName)
						ELSE CONCAT('Error ',HE.ErrorNumber,' on ',RI.InstanceDisplayName,': ',HE.Details) END AS Reason,
					CASE HE.EventName
						WHEN N'sp_server_diagnostics_component_result' THEN 1
						WHEN N'availability_replica_state' THEN 2
						WHEN N'availability_replica_manager_state_change' THEN 3
						ELSE 4 END AS Priority
	FROM (
		SELECT I2.InstanceID, I2.InstanceDisplayName
		FROM dbo.Instances I2
		WHERE I2.InstanceID = RC.InstanceID
		OR EXISTS(
			SELECT 1
			FROM dbo.AvailabilityReplicas AR
			WHERE AR.InstanceID = I2.InstanceID
			AND AR.group_id = RC.group_id
			)
	) RI
	JOIN dbo.AvailabilityGroupHealthEvents HE ON HE.InstanceID = RI.InstanceID
	WHERE HE.EventTime >= W.FromTime
	AND HE.EventTime <= W.ToTime
	AND (	HE.EventName IN(N'sp_server_diagnostics_component_result',N'error_reported')
			OR (HE.EventName = N'availability_replica_manager_state_change' AND HE.CurrentState <> N'ONLINE')
			OR (HE.EventName = N'availability_replica_state' AND HE.CurrentState = N'STOPPING'
				AND (HE.group_id = RC.group_id OR HE.AvailabilityGroupName = RC.AvailabilityGroupName))
		)
	ORDER BY Priority, HE.EventTime
) IE
/* Most specific first: the failover statement, then what triggered an automatic failover */
OUTER APPLY(
	SELECT	CASE WHEN AGE.IsForced = 1 THEN 'Forced'
				WHEN AGE.IsManual = 1 THEN 'Manual'
				WHEN AGE.IsLeaseExpired = 1 OR AGE.IsFailoverValidation = 1 OR IE.Priority <= 3 THEN 'Automatic'
				ELSE NULL END AS FailoverType,
			COALESCE(	CASE WHEN AGE.IsForced = 1 OR AGE.IsManual = 1 THEN AGE.FailoverStatement END,
						CASE WHEN IE.Priority <= 3 THEN IE.Reason END,
						CASE WHEN AGE.IsLeaseExpired = 1 THEN 'Lease expired' END,
						IE.Reason
					) AS Reason
) Failover
/* Role changes that overlap the date range */
WHERE RC.ChangedBefore >= ISNULL(@FromDate,DATEADD(d,-30,GETUTCDATE()))
AND ISNULL(RC.ChangedAfter,RC.ChangedBefore) < ISNULL(@ToDate,'99991231')
AND (
	EXISTS(
		SELECT 1
		FROM @InstanceIDs T
		WHERE T.ID = RC.InstanceID
	)
	OR (
		@IncludeOtherReplicas = 1
		AND EXISTS(
			SELECT 1
			FROM dbo.AvailabilityReplicas AR
			JOIN @InstanceIDs T ON T.ID = AR.InstanceID
			WHERE AR.group_id = RC.group_id
		)
	)
)
/* Hide snapshot rows for changes that AlwaysOn_health also captured */
AND NOT (
	RC.Source = 'Snapshot'
	AND EXISTS(
		SELECT 1
		FROM dbo.AvailabilityGroupRoleChanges XE
		WHERE XE.InstanceID = RC.InstanceID
		AND XE.Source = 'XE'
		AND (XE.group_id = RC.group_id OR XE.AvailabilityGroupName = RC.AvailabilityGroupName)
		AND XE.ChangedBefore > DATEADD(mi,-2,ISNULL(RC.ChangedAfter,RC.ChangedBefore))
		AND XE.ChangedBefore <= DATEADD(mi,2,RC.ChangedBefore)
	)
)
ORDER BY RC.ChangedBefore DESC,
		RC.AvailabilityGroupName,
		I.InstanceDisplayName
