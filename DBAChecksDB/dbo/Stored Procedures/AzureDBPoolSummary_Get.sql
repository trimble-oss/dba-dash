CREATE PROC  [dbo].[AzureDBPoolSummary_Get](
	@FromDate DATETIME2(3)=NULL,
	@ToDate DATETIME2(3)=NULL,
	@InstanceIDs VARCHAR(MAX)=NULL,
	@DatabaseName SYSNAME=NULL,
	@DTUHist BIT=0,
	@CPUHist BIT=0,
	@DataHist BIT = 0,
	@LogHist BIT=0
)
AS
IF @FromDate IS NULL
	SET @FromDate = DATEADD(d,-1,GETUTCDATE())
IF @ToDate IS NULL 
	SET @ToDate = GETUTCDATE()
DECLARE @Instances IDs
IF @InstanceIDs IS NULL
BEGIN
	INSERT INTO @Instances
	(
	    ID
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
		ID
	)
	SELECT value
	FROM STRING_SPLIT(@InstanceIDs,',')
END;

DECLARE @SQL NVARCHAR(MAX)

SET @SQL =CAST('' AS NVARCHAR(MAX)) + N'
SELECT I.InstanceID,
	I.ConnectionID,
	I.Instance,
	EP.elastic_pool_name,
	MAX(dtu.AvgDTUPercent) Max_DTUPercent,
	MAX(dtu.AvgDTUsUsed) AS Max_DTUsUsed,
	MAX(RS.elastic_pool_dtu_limit )-MAX(dtu.AvgDTUsUsed) AS UnusedDTU,
	AVG(dtu.AvgDTUPercent) AS Avg_DTUPercent,
	AVG(dtu.AvgDTUsUsed) AS Avg_DTUsUsed,
	MIN(RS.elastic_pool_dtu_limit) Min_DTULimit,
	MAX(RS.elastic_pool_dtu_limit ) MAX_DTULimit,
	MIN(RS.elastic_pool_cpu_limit)  as MinCPULimit,
	MAX(RS.elastic_pool_cpu_limit) as MaxCPULimit,
	MAX(RS.avg_cpu_percent) AS MaxCPUPercent,
	MAX(RS.avg_data_io_percent) AS MaxDataPercent,
	MAX(RS.avg_log_write_percent) MaxLogWritePercent,
	AVG(RS.avg_cpu_percent) AS AvgCPUPercent,
	AVG(RS.avg_data_io_percent) AS AvgDataPercent,
	AVG(RS.avg_log_write_percent) AvgLogWritePercent,
	' + CASE WHEN @DTUHist =1 THEN '
	SUM(CASE WHEN DTU.AvgDTUPercent<10 THEN 1 ELSE 0 END) AS DTU10,
	SUM(CASE WHEN DTU.AvgDTUPercent>=10 AND DTU.AvgDTUPercent<20 THEN 1 ELSE 0 END) AS DTU20,
	SUM(CASE WHEN DTU.AvgDTUPercent>=20 AND DTU.AvgDTUPercent<30 THEN 1 ELSE 0 END) AS DTU30,
	SUM(CASE WHEN DTU.AvgDTUPercent>=30 AND DTU.AvgDTUPercent<40 THEN 1 ELSE 0 END) AS DTU40,
	SUM(CASE WHEN DTU.AvgDTUPercent>=40 AND DTU.AvgDTUPercent<50 THEN 1 ELSE 0 END) AS DTU50,
	SUM(CASE WHEN DTU.AvgDTUPercent>=50 AND DTU.AvgDTUPercent<60 THEN 1 ELSE 0 END) AS DTU60,
	SUM(CASE WHEN DTU.AvgDTUPercent>=60 AND DTU.AvgDTUPercent<70 THEN 1 ELSE 0 END) AS DTU70,
	SUM(CASE WHEN DTU.AvgDTUPercent>=70 AND DTU.AvgDTUPercent<80 THEN 1 ELSE 0 END) AS DTU80,
	SUM(CASE WHEN DTU.AvgDTUPercent>=80 AND DTU.AvgDTUPercent<90 THEN 1 ELSE 0 END) AS DTU90,
	SUM(CASE WHEN DTU.AvgDTUPercent>= 90 THEN 1 ELSE 0 END) AS DTU100,
	' ELSE '' END 
	 + CASE WHEN @CPUHist =1 THEN '
	SUM(CASE WHEN RS.avg_cpu_percent <10 THEN 1 ELSE 0 END) AS CPU10,
	SUM(CASE WHEN RS.avg_cpu_percent >=10 AND RS.avg_cpu_percent <20 THEN 1 ELSE 0 END) AS CPU20,
	SUM(CASE WHEN RS.avg_cpu_percent >=20 AND RS.avg_cpu_percent <30 THEN 1 ELSE 0 END) AS CPU30,
	SUM(CASE WHEN RS.avg_cpu_percent >=30 AND RS.avg_cpu_percent <40 THEN 1 ELSE 0 END) AS CPU40,
	SUM(CASE WHEN RS.avg_cpu_percent >=40 AND RS.avg_cpu_percent <50 THEN 1 ELSE 0 END) AS CPU50,
	SUM(CASE WHEN RS.avg_cpu_percent >=50 AND RS.avg_cpu_percent <60 THEN 1 ELSE 0 END) AS CPU60,
	SUM(CASE WHEN RS.avg_cpu_percent >=60 AND RS.avg_cpu_percent <70 THEN 1 ELSE 0 END) AS CPU70,
	SUM(CASE WHEN RS.avg_cpu_percent >=70 AND RS.avg_cpu_percent <80 THEN 1 ELSE 0 END) AS CPU80,
	SUM(CASE WHEN RS.avg_cpu_percent >=80 AND RS.avg_cpu_percent <90 THEN 1 ELSE 0 END) AS CPU90,
	SUM(CASE WHEN RS.avg_cpu_percent >= 90 THEN 1 ELSE 0 END) AS CPU100,
	' ELSE '' END + 
	CASE WHEN @DataHist=1 THEN '
	SUM(CASE WHEN RS.avg_data_io_percent <10 THEN 1 ELSE 0 END) AS Data10,
	SUM(CASE WHEN RS.avg_data_io_percent>=10 AND RS.avg_data_io_percent<20 THEN 1 ELSE 0 END) AS Data20,
	SUM(CASE WHEN RS.avg_data_io_percent>=20 AND RS.avg_data_io_percent<30 THEN 1 ELSE 0 END) AS Data30,
	SUM(CASE WHEN RS.avg_data_io_percent>=30 AND RS.avg_data_io_percent<40 THEN 1 ELSE 0 END) AS Data40,
	SUM(CASE WHEN RS.avg_data_io_percent>=40 AND RS.avg_data_io_percent<50 THEN 1 ELSE 0 END) AS Data50,
	SUM(CASE WHEN RS.avg_data_io_percent>=50 AND RS.avg_data_io_percent<60 THEN 1 ELSE 0 END) AS Data60,
	SUM(CASE WHEN RS.avg_data_io_percent>=60 AND RS.avg_data_io_percent<70 THEN 1 ELSE 0 END) AS Data70,
	SUM(CASE WHEN RS.avg_data_io_percent>=70 AND RS.avg_data_io_percent<80 THEN 1 ELSE 0 END) AS Data80,
	SUM(CASE WHEN RS.avg_data_io_percent>=80 AND RS.avg_data_io_percent<90 THEN 1 ELSE 0 END) AS Data90,
	SUM(CASE WHEN RS.avg_data_io_percent>= 90 THEN 1 ELSE 0 END) AS Data100,
	' ELSE '' END + 
	CASE WHEN @LogHist =1 THEN '
	SUM(CASE WHEN RS.avg_log_write_percent <10 THEN 1 ELSE 0 END) AS Log10,
	SUM(CASE WHEN RS.avg_log_write_percent >=10 AND RS.avg_log_write_percent <20 THEN 1 ELSE 0 END) AS Log20,
	SUM(CASE WHEN RS.avg_log_write_percent >=20 AND RS.avg_log_write_percent <30 THEN 1 ELSE 0 END) AS Log30,
	SUM(CASE WHEN RS.avg_log_write_percent >=30 AND RS.avg_log_write_percent <40 THEN 1 ELSE 0 END) AS Log40,
	SUM(CASE WHEN RS.avg_log_write_percent >=40 AND RS.avg_log_write_percent <50 THEN 1 ELSE 0 END) AS Log50,
	SUM(CASE WHEN RS.avg_log_write_percent >=50 AND RS.avg_log_write_percent <60 THEN 1 ELSE 0 END) AS Log60,
	SUM(CASE WHEN RS.avg_log_write_percent >=60 AND RS.avg_log_write_percent <70 THEN 1 ELSE 0 END) AS Log70,
	SUM(CASE WHEN RS.avg_log_write_percent >=70 AND RS.avg_log_write_percent <80 THEN 1 ELSE 0 END) AS Log80,
	SUM(CASE WHEN RS.avg_log_write_percent >=80 AND RS.avg_log_write_percent <90 THEN 1 ELSE 0 END) AS Log90,
	SUM(CASE WHEN RS.avg_log_write_percent >= 90 THEN 1 ELSE 0 END) AS Log100,'
	ELSE '' END + '
	DATEDIFF(mi,MIN(RS.end_time),MAX(RS.end_time)) AS Minutes

FROM dbo.AzureDBElasticPoolResourceStats RS
JOIN dbo.AzureDBElasticPool EP ON RS.PoolID = EP.PoolID
JOIN dbo.Instances I ON I.InstanceID = EP.InstanceID
OUTER APPLY (SELECT CASE WHEN RS.elastic_pool_dtu_limit>0 THEN (SELECT Max(v) FROM (VALUES (RS.avg_cpu_percent), (RS.avg_data_io_percent), (RS.avg_log_write_percent)) AS value(v)) ELSE NULL END AS [AvgDTUPercent],
	CASE WHEN RS.elastic_pool_dtu_limit>0 THEN ((RS.elastic_pool_dtu_limit)*((SELECT Max(v) FROM (VALUES (RS.avg_cpu_percent), (RS.avg_data_io_percent), (RS.avg_log_write_percent)) AS value(v))/100.00)) ELSE NULL END AS [AvgDTUsUsed]) AS dtu
WHERE RS.end_time>=@FromDate
AND RS.end_time <@ToDate
AND I.IsActive=1
AND EXISTS(SELECT 1
			FROM @Instances ids
			JOIN dbo.AzureDBServiceObjectives SO ON SO.InstanceID = ids.ID 
			JOIN dbo.Instances SOI ON SOI.InstanceID = SO.InstanceID
			JOIN dbo.Databases SOD ON SOD.InstanceID = SOI.InstanceID		
			WHERE SOI.Instance = I.Instance
			AND SO.elastic_pool_name = EP.elastic_pool_name
			' + CASE WHEN @DatabaseName IS NULL THEN '' ELSE 'AND SOD.Name = @DatabaseName' END + '
			) 
GROUP BY I.InstanceID,I.ConnectionID,I.Instance, EP.elastic_pool_name
ORDER BY UnusedDTU DESC;'

EXEC sp_executesql @SQL,N'@FromDate DATETIME2(3),@ToDate DATETIME2(3),@Instances IDs READONLY,@DatabaseName SYSNAME',@FromDate,@ToDate,@Instances,@DatabaseName