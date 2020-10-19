CREATE PROC [dbo].[AzureDBPerformanceSummary_Get](
	@FromDate DATETIME2(3)=NULL,
	@ToDate DATETIME2(3)=NULL,
	@InstanceIDs SYSNAME=NULL,
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
DECLARE @tInstanceIDs IDs
IF @InstanceIDs IS NULL
BEGIN
	INSERT INTO @tInstanceIDs
	(
	    ID
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
		ID
	)
	SELECT value
	FROM STRING_SPLIT(@InstanceIDs,',')
END;

DECLARE @SQL NVARCHAR(MAX)
SET @SQL = CAST(N'' AS NVARCHAR(MAX)) + N'
SELECT RS.InstanceID,
	I.ConnectionID,
	I.Instance,
	D.name AS DB,
	O.edition,
	O.service_objective,
	O.elastic_pool_name,
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
FROM dbo.AzureDBResourceStats RS
JOIN dbo.Instances I ON I.InstanceID = RS.InstanceID
JOIN dbo.Databases D ON I.InstanceID = D.InstanceID
LEFT JOIN dbo.AzureDBServiceObjectives O ON O.InstanceID = I.InstanceID
OUTER APPLY(SELECT 	CASE WHEN dtu_limit>0 THEN (SELECT Max(v) FROM (VALUES (avg_cpu_percent), (avg_data_io_percent), (avg_log_write_percent)) AS value(v)) ELSE NULL END AS [AvgDTUPercent],
					CASE WHEN dtu_limit>0 THEN ((dtu_limit)*((SELECT Max(v) FROM (VALUES (avg_cpu_percent), (avg_data_io_percent), (avg_log_write_percent)) AS value(v))/100.00)) ELSE NULL END AS [AvgDTUsUsed]
			) AS DTU
WHERE RS.end_time>=@FromDate
AND RS.end_time <@ToDate
AND D.IsActive=1
AND I.IsActive=1
AND EXISTS(SELECT 1 FROM @tInstanceIDs t WHERE I.InstanceID = t.ID)
' + CASE WHEN @DatabaseName IS NOT NULL THEN 'AND D.name=@DatabaseName' ELSE '' END + '
GROUP BY RS.InstanceID,	
	I.ConnectionID,
	I.Instance,
	D.name,
	O.edition,
	O.service_objective,
	O.elastic_pool_name
ORDER BY UnusedDTU DESC;'

EXEC sp_executesql @SQL,N'@FromDate DATETIME2(3),@ToDate DATETIME2(3),@tInstanceIDs IDs READONLY,@DatabaseName SYSNAME',@FromDate,@ToDate,@tInstanceIDs,@DatabaseName