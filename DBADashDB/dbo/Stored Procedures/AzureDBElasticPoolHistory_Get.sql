
CREATE PROC dbo.AzureDBElasticPoolHistory_Get(
	@InstanceIDs VARCHAR(MAX)
)
AS
SELECT PH.PoolID,
	   I.Instance,
	   P.elastic_pool_name,
       PH.elastic_pool_dtu_limit_old,     
       PH.elastic_pool_dtu_limit_new,
	   PH.elastic_pool_cpu_limit_old,
       PH.elastic_pool_cpu_limit_new,
       PH.ValidFrom,
       PH.ValidTo 
FROM dbo.AzureDBElasticPoolHistory PH 
JOIN dbo.AzureDBElasticPool P ON P.PoolID = PH.PoolID
JOIN dbo.Instances I ON P.InstanceID = I.InstanceID
WHERE EXISTS(SELECT 1 FROM STRING_SPLIT(@InstanceIDs,',') ss WHERE ss.value = I.InstanceID)