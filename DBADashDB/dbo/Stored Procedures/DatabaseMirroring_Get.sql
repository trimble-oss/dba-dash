CREATE PROC dbo.DatabaseMirroring_Get(
		@InstanceIDs VARCHAR(MAX)=NULL,
		@ShowHidden BIT=1
)
AS
SELECT I.InstanceDisplayName as Instance,
	D.name as DB,
	DM.mirroring_state,
	DM.mirroring_state_desc,
	DM.mirroring_role_desc, 
	DM.mirroring_safety_level_desc,
	DM.mirroring_safety_sequence,
	DM.mirroring_partner_name,
	DM.mirroring_partner_instance,
	DM.mirroring_witness_name,
	DM.mirroring_witness_state,
	DM.mirroring_witness_state_desc,
	DM.mirroring_connection_timeout,
	DM.mirroring_redo_queue_type, 
	DMP.mirroring_state AS partner_mirroring_state,
	DMP.mirroring_state_desc AS partner_mirroring_state_desc,
	CD.SnapshotAge,
	CD.Status as CollectionDateStatus
FROM dbo.DatabaseMirroring DM
JOIN dbo.Databases D ON D.DatabaseID = DM.DatabaseID AND DM.InstanceID = D.InstanceID
JOIN dbo.Instances I ON I.InstanceID = D.InstanceID
JOIN dbo.DatabaseMirroring DMP ON DM.mirroring_guid = DMP.mirroring_guid AND DMP.InstanceID <> DM.InstanceID
LEFT JOIN dbo.CollectionDatesStatus CD ON DM.InstanceID = CD.InstanceID AND CD.Reference = 'DatabaseMirroring'
WHERE EXISTS(SELECT 1 FROM STRING_SPLIT(@InstanceIDs,',') ss WHERE ss.value = I.InstanceID
		UNION ALL
		SELECT 1 WHERE @InstanceIDs IS NULL)
AND I.IsActive=1
AND D.IsActive=1
AND (I.ShowInSummary=1 OR @ShowHidden=1)