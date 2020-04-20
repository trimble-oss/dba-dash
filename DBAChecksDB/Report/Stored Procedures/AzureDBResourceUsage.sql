CREATE PROC [Report].[AzureDBResourceUsage](
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
	   I.ConnectionID,
       end_time,
       avg_cpu_percent,
       avg_data_io_percent,
       avg_log_write_percent,
       avg_memory_usage_percent,
       xtp_storage_percent,
       max_worker_percent,
       max_session_percent,
       dtu_limit,
       avg_instance_cpu_percent,
       avg_instance_memory_percent,
       cpu_limit,
       replica_role,
       DTU.AvgDTUPercent,
       DTU.AvgDTUsUsed
FROM dbo.AzureDBResourceStats ARS
JOIN dbo.Instances I ON I.InstanceID = ARS.InstanceID
OUTER APPLY(SELECT 	CASE WHEN dtu_limit>0 THEN (SELECT Max(v) FROM (VALUES (avg_cpu_percent), (avg_data_io_percent), (avg_log_write_percent)) AS value(v)) ELSE NULL END AS [AvgDTUPercent],
					CASE WHEN dtu_limit>0 THEN ((dtu_limit)*((SELECT Max(v) FROM (VALUES (avg_cpu_percent), (avg_data_io_percent), (avg_log_write_percent)) AS value(v))/100.00)) ELSE NULL END AS [AvgDTUsUsed]
			) AS DTU
WHERE EXISTS(SELECT 1 FROM @Instances t WHERE I.InstanceID = t.InstanceID)
AND ARS.end_time>=@FromDate
AND ARS.end_time<@ToDate
AND I.IsActive=1