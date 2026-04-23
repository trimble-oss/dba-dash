CREATE PROC DBADash.AI_AgDrRisk_Get(
	@MaxRows INT = 300,
	@InstanceFilter NVARCHAR(256) = NULL,
	@HoursBack INT = NULL
)
AS
/* Result set 1: AG-related alerts */
SELECT TOP (@MaxRows)
	i.InstanceDisplayName,
	aa.AlertKey,
	aa.LastMessage,
	aa.IsResolved,
	aa.IsAcknowledged,
	aa.TriggerDate,
	aa.UpdatedDate
FROM Alert.ActiveAlerts aa
INNER JOIN dbo.Instances i ON i.InstanceID = aa.InstanceID
WHERE i.IsActive = 1
  AND (@InstanceFilter IS NULL OR i.InstanceDisplayName LIKE @InstanceFilter + '%')
  AND (
		aa.AlertKey LIKE '%AG%'
	 OR aa.AlertType LIKE '%Availability%'
	 OR aa.AlertType LIKE '%HADR%'
	  )
ORDER BY aa.IsResolved ASC, aa.Priority ASC, aa.UpdatedDate DESC

/* Result set 2: AG replica status from DatabasesHADR */
SELECT TOP (@MaxRows)
	i.InstanceDisplayName,
	ag.name AS AvailabilityGroupName,
	ar.replica_server_name AS ReplicaServerName,
	ar.availability_mode_desc AS AvailabilityMode,
	ar.failover_mode_desc AS FailoverMode,
	d.name AS DatabaseName,
	h.is_primary_replica AS IsPrimary,
	h.synchronization_state_desc AS SyncState,
	h.synchronization_health_desc AS SyncHealth,
	h.is_suspended AS IsSuspended,
	h.suspend_reason_desc AS SuspendReason,
	h.database_state_desc AS DatabaseState,
	h.secondary_lag_seconds AS SecondaryLagSeconds,
	h.log_send_queue_size AS LogSendQueueKB,
	h.log_send_rate AS LogSendRateKBSec,
	h.redo_queue_size AS RedoQueueKB,
	h.redo_rate AS RedoRateKBSec,
	h.last_sent_time AS LastSentTime,
	h.last_received_time AS LastReceivedTime,
	h.last_hardened_time AS LastHardenedTime,
	h.last_redone_time AS LastRedoneTime,
	h.last_commit_time AS LastCommitTime
FROM dbo.DatabasesHADR h
INNER JOIN dbo.Instances i ON i.InstanceID = h.InstanceID
INNER JOIN dbo.Databases d ON d.DatabaseID = h.DatabaseID
LEFT JOIN dbo.AvailabilityGroups ag ON ag.InstanceID = h.InstanceID AND ag.group_id = h.group_id
LEFT JOIN dbo.AvailabilityReplicas ar ON ar.InstanceID = h.InstanceID AND ar.replica_id = h.replica_id
WHERE i.IsActive = 1
  AND (@InstanceFilter IS NULL OR i.InstanceDisplayName LIKE @InstanceFilter + '%')
ORDER BY h.synchronization_health ASC, h.synchronization_state ASC, h.log_send_queue_size DESC

/* Result set 3: Database mirroring status */
SELECT TOP (@MaxRows)
	i.InstanceDisplayName,
	d.name AS DatabaseName,
	dm.mirroring_state_desc AS MirroringState,
	dm.mirroring_role_desc AS MirroringRole,
	dm.mirroring_safety_level_desc AS SafetyLevel,
	dm.mirroring_partner_instance AS PartnerInstance,
	dm.mirroring_witness_name AS WitnessName,
	dm.mirroring_witness_state_desc AS WitnessState,
	dm.mirroring_redo_queue AS RedoQueue,
	dm.mirroring_connection_timeout AS ConnectionTimeoutSec
FROM dbo.DatabaseMirroring dm
INNER JOIN dbo.Instances i ON i.InstanceID = dm.InstanceID
INNER JOIN dbo.Databases d ON d.DatabaseID = dm.DatabaseID
WHERE i.IsActive = 1
  AND dm.mirroring_guid IS NOT NULL
  AND (@InstanceFilter IS NULL OR i.InstanceDisplayName LIKE @InstanceFilter + '%')
ORDER BY dm.mirroring_state ASC

/* Result set 4: Backup risk */
SELECT TOP (@MaxRows)
	bs.InstanceDisplayName,
	bs.name AS DatabaseName,
	bs.recovery_model_desc AS RecoveryModel,
	bs.BackupStatus,
	bs.FullBackupStatus,
	bs.LogBackupStatus,
	bs.LastFull,
	bs.LastLog,
	bs.SnapshotAge
FROM dbo.BackupStatus bs
WHERE bs.BackupStatus IN (1,2)
  AND (@InstanceFilter IS NULL OR bs.InstanceDisplayName LIKE @InstanceFilter + '%')
ORDER BY bs.BackupStatus ASC, bs.SnapshotAge DESC
