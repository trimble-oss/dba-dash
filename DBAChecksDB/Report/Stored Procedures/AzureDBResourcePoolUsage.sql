CREATE PROC [Report].[AzureDBResourcePoolUsage](
	@InstanceIDs VARCHAR(MAX),
	@FromDate DATETIME2(3)=NULL,
	@ToDate DATETIME2(3)=NULL
)
AS
IF @ToDate IS NULL
	SET @ToDate=GETUTCDATE()
IF @FromDate IS NULL
	SET @FromDate=DATEADD(mi,-120,GETUTCDATE())
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
	SELECT Item
	FROM dbo.SplitStrings(@InstanceIDs,',')
END;
SELECT I.InstanceID,
	   I.Instance,
	   EP.elastic_pool_name,
       RPS.end_time,
       RPS.avg_cpu_percent,
       RPS.avg_data_io_percent,
       RPS.avg_log_write_percent,
       RPS.max_worker_percent,
       RPS.max_session_percent,
       RPS.elastic_pool_dtu_limit,
       DTU.AvgDTUPercent,
       DTU.AvgDTUsUsed
FROM dbo.AzureDBElasticPoolResourceStats RPS
JOIN dbo.AzureDBElasticPool EP ON RPS.PoolID = EP.PoolID
JOIN dbo.Instances I ON I.InstanceID = EP.InstanceID
OUTER APPLY(SELECT 	CASE WHEN RPS.elastic_pool_dtu_limit>0 THEN (SELECT Max(value.v) FROM (VALUES (avg_cpu_percent), (avg_data_io_percent), (avg_log_write_percent)) AS value(v)) ELSE NULL END AS [AvgDTUPercent],
					CASE WHEN RPS.elastic_pool_dtu_limit>0 THEN ((RPS.elastic_pool_dtu_limit)*((SELECT Max(value.v) FROM (VALUES (avg_cpu_percent), (avg_data_io_percent), (avg_log_write_percent)) AS value(v))/100.00)) ELSE NULL END AS [AvgDTUsUsed]
			) AS DTU
WHERE EXISTS(SELECT 1 
			FROM @Instances t 
			JOIN dbo.AzureDBServiceObjectives SO ON SO.InstanceID = t.InstanceID
			JOIN dbo.Instances I2 ON I2.InstanceID = t.InstanceID
			WHERE I2.Instance = I.Instance
			AND EP.elastic_pool_name = SO.elastic_pool_name)
AND RPS.end_time>=@FromDate
AND RPS.end_time<@ToDate
AND I.IsActive=1