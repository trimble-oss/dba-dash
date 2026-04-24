CREATE PROC dbo.Counters_Get
AS
SELECT CounterID,
       object_name,
       counter_name,
       instance_name 
FROM dbo.Counters C
WHERE EXISTS(SELECT 1 
			FROM dbo.InstanceCounters IC 
			WHERE IC.CounterID = C.CounterID
			AND IC.UpdatedDate>=DATEADD(d,-2,GETUTCDATE())
			UNION ALL
			SELECT 1
			FROM dbo.AzureDBElasticPool P 
			WHERE C.instance_name = P.elastic_pool_name
			AND C.CounterType = 3 /* Elastic Pool */
			AND (P.ValidTo IS NULL OR P.ValidTo >= DATEADD(d,-2,GETUTCDATE()))
			UNION ALL
			SELECT 1 
			FROM dbo.Instances I
			WHERE I.EngineEdition = 5 /* AzureDB */
			AND C.CounterType = 4 /* AzureDB */
			AND I.IsActive = 1
			)
ORDER BY object_name,counter_name,instance_name