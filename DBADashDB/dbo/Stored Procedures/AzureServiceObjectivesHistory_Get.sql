CREATE PROC dbo.AzureServiceObjectivesHistory_Get(
    @InstanceIDs VARCHAR(MAX),
    @ShowHidden BIT=1
)
AS
SELECT H.InstanceID,
	  I.Instance,
       D.name AS db,     
       H.edition,
       H.service_objective,
       H.elastic_pool_name,
       H.new_edition,
       H.new_service_objective,
       H.new_elastic_pool_name,
       H.ValidFrom,
       H.ValidTo
FROM dbo.AzureDBServiceObjectivesHistory H
JOIN dbo.Instances I  ON I.InstanceID = H.InstanceID
JOIN dbo.Databases D  ON I.InstanceID = D.InstanceID
WHERE EXISTS(
            SELECT 1 
            FROM STRING_SPLIT(@InstanceIDs,',') ss 
            WHERE ss.value =  I.InstanceID
            )
AND (I.ShowInSummary=1 OR @ShowHidden=1)
ORDER BY H.ValidTo DESC;