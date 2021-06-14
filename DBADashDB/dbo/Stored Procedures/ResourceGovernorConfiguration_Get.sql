CREATE PROC dbo.ResourceGovernorConfiguration_Get(
	@InstanceIDs VARCHAR(MAX)=NULL
)
AS
DECLARE @Instances TABLE(
	InstanceID INT PRIMARY KEY
)
IF @InstanceIDs IS NULL
BEGIN
	INSERT INTO @Instances
	(
	    InstanceID
	)
	SELECT InstanceID 
	FROM dbo.Instances 
	WHERE IsActive=1
END 
ELSE 
BEGIN
	INSERT INTO @Instances
	(
		InstanceID
	)
	SELECT value
	FROM STRING_SPLIT(@InstanceIDs,',')
END;
SELECT RG.InstanceID,
	I.Instance, 
	RG.is_enabled,
	RG.classifier_function,
	RG.reconfiguration_error,
	RG.reconfiguration_pending,
	RG.max_outstanding_io_per_volume,
	RG.script,
	ValidFrom,
	CD.SnapshotAge,
	CD.SnapshotDate,
	CD.Status AS SnapshotStatus
FROM dbo.ResourceGovernorConfiguration RG 
JOIN dbo.Instances I ON RG.InstanceID = I.InstanceID
LEFT JOIN dbo.CollectionDatesStatus CD ON CD.InstanceID = I.InstanceID AND CD.Reference='ResourceGovernorConfiguration'
WHERE EXISTS(SELECT 1 
			FROM @Instances t 
			WHERE I.InstanceID = t.InstanceID
			)