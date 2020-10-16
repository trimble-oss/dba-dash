CREATE PROC  [dbo].[AzureDBPoolSummary_Get](
	@FromDate DATETIME2(3)=NULL,
	@ToDate DATETIME2(3)=NULL,
	@InstanceIDs SYSNAME=NULL,
	@DatabaseName SYSNAME=NULL
)
AS
IF @FromDate IS NULL
	SET @FromDate = DATEADD(d,-1,GETUTCDATE())
IF @ToDate IS NULL 
	SET @ToDate = GETUTCDATE()
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
	AND EditionID=1674378470
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
WITH T AS (
SELECT InstanceID,
	elastic_pool_name,
	end_time AS [EndTime],
	CASE WHEN elastic_pool_dtu_limit>0 THEN (SELECT Max(v) FROM (VALUES (avg_cpu_percent), (avg_data_io_percent), (avg_log_write_percent)) AS value(v)) ELSE NULL END AS [AvgDTUPercent],
	CASE WHEN elastic_pool_dtu_limit>0 THEN ((elastic_pool_dtu_limit)*((SELECT Max(v) FROM (VALUES (avg_cpu_percent), (avg_data_io_percent), (avg_log_write_percent)) AS value(v))/100.00)) ELSE NULL END AS [AvgDTUsUsed],
	avg_cpu_percent,
	avg_data_io_percent,
	avg_log_write_percent,
	elastic_pool_dtu_limit AS [DTULimit]
FROM dbo.AzureDBElasticPoolResourceStats RS
WHERE RS.end_time>=@FromDate
AND RS.end_time <@ToDate
)
SELECT I.InstanceID,
	I.ConnectionID,
	I.Instance,
	T.elastic_pool_name,
	MAX(T.AvgDTUPercent) Max_DTUPercent,
	MAX(T.AvgDTUsUsed) AS Max_DTUsUsed,
	MAX(T.DTULimit)-MAX(T.AvgDTUsUsed) AS UnusedDTU,
	AVG(T.AvgDTUPercent) AS Avg_DTUPercent,
	AVG(T.AvgDTUsUsed) AS Avg_DTUsUsed,
	MIN(T.DTULimit) Min_DTULimit,
	MAX(T.DTULimit) MAX_DTULimit,
	MAX(T.avg_cpu_percent) AS MaxCPUPercent,
	MAX(T.avg_data_io_percent) AS MaxDataPercent,
	MAX(T.avg_log_write_percent) MaxLogWritePercent,
	AVG(T.avg_cpu_percent) AS AvgCPUPercent,
	AVG(T.avg_data_io_percent) AS AvgDataPercent,
	AVG(T.avg_log_write_percent) AvgLogWritePercent,
	DATEDIFF(mi,MIN(T.EndTime),MAX(T.EndTime)) AS Minutes

FROM T
JOIN dbo.Instances I ON I.InstanceID = T.InstanceID
WHERE I.IsActive=1
AND EXISTS(SELECT 1
			FROM @Instances ids
			JOIN dbo.AzureDBServiceObjectives SO ON SO.InstanceID = ids.InstanceID
			JOIN dbo.Instances SOI ON SOI.InstanceID = SO.InstanceID
			JOIN dbo.Databases SOD ON SOD.InstanceID = SOI.InstanceID		
			WHERE (SOD.Name = @DatabaseName OR @DatabaseName IS NULL)
			AND SOI.Instance = I.Instance
			AND SO.elastic_pool_name = T.elastic_pool_name
			) 
GROUP BY I.InstanceID,I.ConnectionID,I.Instance, T.elastic_pool_name
ORDER BY UnusedDTU DESC;