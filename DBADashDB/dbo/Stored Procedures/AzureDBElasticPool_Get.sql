CREATE PROC dbo.AzureDBElasticPool_Get
AS
SELECT	EP.PoolID,
		EP.elastic_pool_name,
		I.InstanceGroupName
FROM dbo.AzureDBElasticPool EP
JOIN dbo.Instances I ON EP.InstanceID = I.InstanceID