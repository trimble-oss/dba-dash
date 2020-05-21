CREATE PROC [dbo].[AzureDBResourceStats_Upd](@AzureDBResourceStats dbo.AzureDBResourceStats READONLY,@InstanceID INT,@SnapshotDate DATETIME2(3))
AS
DECLARE @MaxDate DATETIME2
SELECT @MaxDate=ISNULL(MAX(end_time),'19000101') 
FROM dbo.AzureDBResourceStats
WHERE InstanceID=@InstanceID

INSERT INTO dbo.AzureDBResourceStats
(
    InstanceID,
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
    replica_role
)
SELECT @InstanceID,
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
       replica_role 
FROM @AzureDBResourceStats
WHERE end_time > @MaxDate;

WITH t AS (
	SELECT InstanceID,
		end_time, 
		dtu_limit, 
		LAG(dtu_limit) OVER(PARTITION BY InstanceID ORDER BY end_time) dtu_limit_previous
	FROM dbo.AzureDBResourceStats
	WHERE InstanceID=@InstanceID
	AND end_time>=@MaxDate
)
INSERT INTO dbo.AzureDBDTULimitChange(InstanceID,ChangeDate,dtu_limit_new,dtu_limit_old)
SELECT t.InstanceID,
       t.end_time,
       t.dtu_limit,
       t.dtu_limit_previous
FROM T 
WHERE t.dtu_limit<> t.dtu_limit_previous

EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID, 
                             @Reference = 'AzureDBResourceStats', 
                             @SnapshotDate = @SnapshotDate