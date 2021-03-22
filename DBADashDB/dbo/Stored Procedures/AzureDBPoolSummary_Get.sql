CREATE PROC  [dbo].[AzureDBPoolSummary_Get](
	@FromDate DATETIME2(3)=NULL,
	@ToDate DATETIME2(3)=NULL,
	@InstanceIDs VARCHAR(MAX)=NULL,
	@DatabaseName SYSNAME=NULL,
	@DTUHist BIT=0,
	@CPUHist BIT=0,
	@DataHist BIT = 0,
	@LogHist BIT=0,
	@Use60MIN BIT=NULL
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
IF @Use60MIN IS NULL
BEGIN
	SELECT @Use60MIN = CASE WHEN DATEDIFF(hh,@FromDate,@ToDate)>24 THEN 1
						WHEN DATEPART(mi,@FromDate)+DATEPART(s,@FromDate)+DATEPART(ms,@FromDate)=0 
							AND (DATEPART(mi,@ToDate)+DATEPART(s,@ToDate)+DATEPART(ms,@ToDate)=0 
									OR @ToDate>=DATEADD(s,-2,GETUTCDATE())
								)
						THEN 1
						ELSE 0 END
END

DECLARE @SQL NVARCHAR(MAX)

SET @SQL =CAST('' AS NVARCHAR(MAX)) + N'
SELECT I.InstanceID,
	I.ConnectionID,
	I.Instance,
	EP.elastic_pool_name,
	MAX(RS.max_dtu_percent) Max_DTUPercent,
	MAX(RS.max_dtu) AS Max_DTUsUsed,
	MAX(RS.elastic_pool_dtu_limit )-MAX(RS.max_dtu) AS UnusedDTU,
	AVG(RS.avg_dtu_percent) AS Avg_DTUPercent,
	AVG(RS.avg_dtu) AS Avg_DTUsUsed,
	MIN(RS.elastic_pool_dtu_limit) Min_DTULimit,
	MAX(RS.elastic_pool_dtu_limit ) MAX_DTULimit,
	MIN(RS.elastic_pool_cpu_limit)  as MinCPULimit,
	MAX(RS.elastic_pool_cpu_limit) as MaxCPULimit,
	MAX(RS.max_cpu_percent) AS MaxCPUPercent,
	MAX(RS.max_data_io_percent) AS MaxDataPercent,
	MAX(RS.max_log_write_percent) MaxLogWritePercent,
	AVG(RS.avg_cpu_percent) AS AvgCPUPercent,
	AVG(RS.avg_data_io_percent) AS AvgDataPercent,
	AVG(RS.avg_log_write_percent) AvgLogWritePercent,
	' + CASE WHEN @DTUHist =1 THEN '
	SUM(DTU10) AS DTU10,
	SUM(DTU20) AS DTU20,
	SUM(DTU30) AS DTU30,
	SUM(DTU40) AS DTU40,
	SUM(DTU50) AS DTU50,
	SUM(DTU60) AS DTU60,
	SUM(DTU70) AS DTU70,
	SUM(DTU80) AS DTU80,
	SUM(DTU90) AS DTU90,
	SUM(DTU100) AS DTU100,
	' ELSE '' END 
	 + CASE WHEN @CPUHist =1 THEN '
	SUM(CPU10) AS CPU10,
	SUM(CPU20) AS CPU20,
	SUM(CPU30) AS CPU30,
	SUM(CPU40) AS CPU40,
	SUM(CPU50) AS CPU50,
	SUM(CPU60) AS CPU60,
	SUM(CPU70) AS CPU70,
	SUM(CPU80) AS CPU80,
	SUM(CPU90) AS CPU90,
	SUM(CPU100) AS CPU100,
	' ELSE '' END + 
	CASE WHEN @DataHist=1 THEN '
	SUM(Data10) AS Data10,
	SUM(Data20) AS Data20,
	SUM(Data30) AS Data30,
	SUM(Data40) AS Data40,
	SUM(Data50) AS Data50,
	SUM(Data60) AS Data60,
	SUM(Data70) AS Data70,
	SUM(Data80) AS Data80,
	SUM(Data90) AS Data90,
	SUM(Data100) AS Data100,
	' ELSE '' END + 
	CASE WHEN @LogHist =1 THEN '
	SUM(Log10) AS Log10,
	SUM(Log20) AS Log20,
	SUM(Log30) AS Log30,
	SUM(Log40) AS Log40,
	SUM(Log50) AS Log50,
	SUM(Log60) AS Log60,
	SUM(Log70) AS Log70,
	SUM(Log80) AS Log80,
	SUM(Log90) AS Log90,
	SUM(Log100) AS Log100,'
	ELSE '' END + '
	SUM(CPU10+CPU20+CPU30+CPU40+CPU50+CPU70+CPU80+CPU90+CPU100) AS TotalSamples,
	PSS.avg_allocated_storage_percent as current_allocated_storage_percent, 
	PSS.elastic_pool_storage_limit_mb/1024.0 as current_elastic_pool_storage_limit_gb,
	PSS.elastic_pool_storage_used_mb/1024.0 as current_elastic_pool_storage_used_gb,
	PSS.elastic_pool_storage_free_mb/1024.0 as current_elastic_pool_storage_free_gb,
	PSS.ElasticPoolStorageStatus
FROM ' + CASE WHEN @Use60MIN=1 THEN 'dbo.AzureDBElasticPoolResourceStats_60MIN' ELSE 'dbo.AzureDBElasticPoolResourceStats_Raw' END + ' RS
JOIN dbo.AzureDBElasticPool EP ON RS.PoolID = EP.PoolID
JOIN dbo.AzureDBElasticPoolStorageStatus PSS ON EP.PoolID = PSS.PoolID
JOIN dbo.Instances I ON I.InstanceID = EP.InstanceID
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
GROUP BY I.InstanceID,
	I.ConnectionID,
	I.Instance, 
	EP.elastic_pool_name,	
	PSS.avg_allocated_storage_percent,
	PSS.elastic_pool_storage_limit_mb,
	PSS.elastic_pool_storage_used_mb,
	PSS.elastic_pool_storage_free_mb,
	PSS.ElasticPoolStorageStatus
ORDER BY UnusedDTU DESC;'

EXEC sp_executesql @SQL,N'@FromDate DATETIME2(3),@ToDate DATETIME2(3),@Instances IDs READONLY,@DatabaseName SYSNAME',@FromDate,@ToDate,@Instances,@DatabaseName