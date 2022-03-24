CREATE PROC dbo.DatabaseMirroringSummary_Get(
		@InstanceIDs VARCHAR(MAX)=NULL
)
AS
SELECT I.InstanceDisplayName AS Instance, 
		SUM(CASE WHEN DM.mirroring_role=1 THEN 1 ELSE 0 END) as PrincipalCount,
		SUM(CASE WHEN DM.mirroring_role=2 THEN 1 ELSE 0 END) as MirrorCount,
		SUM(CASE WHEN DM.mirroring_state IN(4,6) THEN 1 ELSE 0 END) as SynchronizedCount,
		SUM(CASE WHEN DM.mirroring_state=0 THEN 1 ELSE 0 END) as SuspendedCount,
		SUM(CASE WHEN DM.mirroring_state=1 THEN 1 ELSE 0 END) as DisconnectedCount,
		SUM(CASE WHEN DM.mirroring_state=2 THEN 1 ELSE 0 END) as SynchronizingCount,
		SUM(CASE WHEN DM.mirroring_state=3 THEN 1 ELSE 0 END) as PendingFailoverCount,		
		SUM(CASE WHEN DM.mirroring_state=5 THEN 1 ELSE 0 END) as NotSynchronizedCount,
		MAX(CASE WHEN DM.mirroring_witness_state=0 THEN 1 ELSE 0 END) as WitnessUnknownCount,
		MAX(CASE WHEN DM.mirroring_witness_state=1 THEN 1 ELSE 0 END) as WitnessConnectedCount,
		MAX(CASE WHEN DM.mirroring_witness_state=2 THEN 1 ELSE 0 END) as WitnessDisconnectedCount,
		MIN(CASE WHEN DM.mirroring_state IN(4,6) AND DM.mirroring_witness_state IN(0,1) THEN 4 WHEN DM.mirroring_state IN(2,4,6) THEN 2 ELSE 1 END) as MirroringStatus,
		CD.SnapshotAge,
		CD.Status as CollectionDateStatus
FROM dbo.DatabaseMirroring DM 
JOIN dbo.Instances I ON DM.InstanceID = I.InstanceID
LEFT JOIN dbo.CollectionDatesStatus CD ON DM.InstanceID = CD.InstanceID AND CD.Reference = 'DatabaseMirroring'
WHERE EXISTS(SELECT 1 FROM STRING_SPLIT(@InstanceIDs,',') ss WHERE ss.value = I.InstanceID
		UNION ALL
		SELECT 1 WHERE @InstanceIDs IS NULL)
GROUP BY I.InstanceDisplayName,
		CD.SnapshotAge,
		CD.Status