CREATE PROC [dbo].[AzureDBPerformanceSummary_Get](
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
DECLARE @tInstanceIDs TABLE(
	InstanceID INT NOT NULL PRIMARY KEY
)
IF @InstanceIDs IS NULL
BEGIN
	INSERT INTO @tInstanceIDs
	(
	    InstanceID
	)
	SELECT DISTINCT InstanceID
	FROM dbo.Instances 
	WHERE IsActive=1
	AND EditionID=1674378470
END 
ELSE 
BEGIN
	INSERT INTO @tInstanceIDs
	(
		InstanceID
	)
	SELECT value
	FROM STRING_SPLIT(@InstanceIDs,',')
END;

WITH T AS (
SELECT RS.InstanceID,
  	MAX(DTU.AvgDTUPercent) Max_DTUPercent,
	MAX(DTU.AvgDTUsUsed) AS Max_DTUsUsed,
	MAX(RS.dtu_limit)-MAX(DTU.AvgDTUsUsed) AS UnusedDTU,
	AVG(DTU.AvgDTUPercent) AS Avg_DTUPercent,
	AVG(DTU.AvgDTUsUsed) AS Avg_DTUsUsed,
	MIN(RS.dtu_limit) Min_DTULimit,
	MAX(RS.dtu_limit) MAX_DTULimit,
	MAX(RS.avg_cpu_percent) AS MaxCPUPercent,
	MAX(RS.avg_data_io_percent) AS MaxDataPercent,
	MAX(RS.avg_log_write_percent) MaxLogWritePercent,
	AVG(RS.avg_cpu_percent) AS AvgCPUPercent,
	AVG(RS.avg_data_io_percent) AS AvgDataPercent,
	AVG(RS.avg_log_write_percent) AvgLogWritePercent,
	DATEDIFF(mi,MIN(RS.end_time),MAX(RS.end_time)) AS Minutes
FROM dbo.AzureDBResourceStats RS
OUTER APPLY(SELECT 	CASE WHEN dtu_limit>0 THEN (SELECT Max(v) FROM (VALUES (avg_cpu_percent), (avg_data_io_percent), (avg_log_write_percent)) AS value(v)) ELSE NULL END AS [AvgDTUPercent],
					CASE WHEN dtu_limit>0 THEN ((dtu_limit)*((SELECT Max(v) FROM (VALUES (avg_cpu_percent), (avg_data_io_percent), (avg_log_write_percent)) AS value(v))/100.00)) ELSE NULL END AS [AvgDTUsUsed]
			) AS DTU
WHERE RS.end_time>=@FromDate
AND RS.end_time <@ToDate
GROUP BY RS.InstanceID
)
SELECT T.InstanceID,
	I.ConnectionID,
	I.Instance,
	D.name AS DB,
	O.edition,
	O.service_objective,
	O.elastic_pool_name,
	T.Max_DTUPercent,
	T.Max_DTUsUsed,
	T.UnusedDTU,
	T.Avg_DTUPercent,
	T.Avg_DTUsUsed,
	T.Min_DTULimit,
	T.MAX_DTULimit,
	T.MaxCPUPercent,
	T.MaxDataPercent,
	T.MaxLogWritePercent,
	T.AvgCPUPercent,
	T.AvgDataPercent,
	T.AvgLogWritePercent,
	T.Minutes
FROM T
JOIN dbo.Instances I ON I.InstanceID = T.InstanceID
JOIN dbo.Databases D ON I.InstanceID = D.InstanceID
LEFT JOIN dbo.AzureDBServiceObjectives O ON O.InstanceID = I.InstanceID
WHERE I.IsActive=1
AND D.IsActive=1
AND EXISTS(SELECT 1 FROM @tInstanceIDs t WHERE I.InstanceID = t.InstanceID)
AND (D.name=@DatabaseName OR @DatabaseName IS NULL)
ORDER BY T.UnusedDTU DESC;