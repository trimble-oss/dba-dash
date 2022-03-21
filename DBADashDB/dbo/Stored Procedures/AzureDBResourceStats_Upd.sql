CREATE PROC [dbo].[AzureDBResourceStats_Upd](@AzureDBResourceStats dbo.AzureDBResourceStats READONLY,@InstanceID INT,@SnapshotDate DATETIME2(3))
AS
SET XACT_ABORT ON
DECLARE @MaxDate DATETIME2(3) = DATEADD(d,-1,GETUTCDATE())

SELECT @MaxDate=ISNULL(MAX(end_time),@MaxDate) 
FROM dbo.AzureDBResourceStats
WHERE InstanceID=@InstanceID
AND end_time>=@MaxDate

BEGIN TRAN
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

IF @@ROWCOUNT>0
BEGIN
	DECLARE @60MINFrom DATETIME2(3)

	SELECT TOP(1) @60MINFrom =DG.DateGroup
	FROM @AzureDBResourceStats t 
	CROSS APPLY dbo.DateGroupingMins(end_time,60) DG
	ORDER BY t.end_time

	DELETE dbo.AzureDBResourceStats_60MIN
	WHERE InstanceID = @InstanceID
	AND end_time>=@60MINFrom
 
	INSERT INTO dbo.AzureDBResourceStats_60MIN(InstanceID,
		   end_time,
		   avg_cpu_percent,
		   max_cpu_percent,
		   avg_data_io_percent,
		   max_data_io_percent,
		   avg_log_write_percent,
		   max_log_write_percent,
		   avg_memory_usage_percent,
		   max_memory_usage_percent,
		   xtp_storage_percent,
		   max_xtp_storage_percent,
		   max_worker_percent,
		   max_session_percent,
		   dtu_limit,
		   avg_instance_cpu_percent,
		   max_instance_cpu_percent,
		   avg_instance_memory_percent,
		   max_instance_memory_percent,
		   cpu_limit,
		   replica_role,
		   avg_dtu_percent,
		   max_dtu_percent,
		   avg_dtu,
		   max_dtu,
		   DTU10,
		   DTU20,
		   DTU30,
		   DTU40,
		   DTU50,
		   DTU60,
		   DTU70,
		   DTU80,
		   DTU90,
		   DTU100,
		   CPU10,
		   CPU20,
		   CPU30,
		   CPU40,
		   CPU50,
		   CPU60,
		   CPU70,
		   CPU80,
		   CPU90,
		   CPU100,
		   Data10,
		   Data20,
		   Data30,
		   Data40,
		   Data50,
		   Data60,
		   Data70,
		   Data80,
		   Data90,
		   Data100,
		   Log10,
		   Log20,
		   Log30,
		   Log40,
		   Log50,
		   Log60,
		   Log70,
		   Log80,
		   Log90,
		   Log100)
	SELECT	RS.InstanceID,
			DG.DateGroup end_time,
			AVG(RS.avg_cpu_percent) AS avg_cpu_percent,
			MAX(RS.max_cpu_percent) AS max_cpu_percent,
			AVG(RS.avg_data_io_percent) AS avg_data_io_percent,
			MAX(RS.max_data_io_percent) AS max_data_io_percent,
			AVG(RS.avg_log_write_percent) AS avg_log_write_percent,
			MAX(RS.max_log_write_percent) AS max_log_write_percent,
			AVG(RS.avg_memory_usage_percent) as avg_memory_usage_percent,
			MAX(RS.max_memory_usage_percent) AS max_memory_usage_percent,
			AVG(RS.xtp_storage_percent) AS xtp_storage_percent,
			MAX(RS.max_xtp_storage_percent) AS max_xtp_storage_percent,
			MAX(RS.max_worker_percent) AS max_worker_percent,
			MAX(RS.max_session_percent) AS max_session_percent,
			MAX(RS.dtu_limit) as dtu_limit,
			AVG(RS.avg_instance_cpu_percent) AS avg_instance_cpu_percent,
			MAX(RS.max_instance_cpu_percent) AS max_instance_cpu_percent,
			AVG(RS.avg_instance_memory_percent) AS avg_instance_memory_percent,
			MAX(RS.max_instance_memory_percent) AS max_instance_memory_percent,
			MAX(RS.cpu_limit) AS cpu_limit,
			MAX(RS.replica_role) AS replica_role,
			AVG(RS.avg_dtu_percent) AS avg_dtu_percent,
			MAX(RS.max_dtu_percent) ASmax_dtu_percent,
			AVG(RS.avg_dtu) AS avg_dtu,
			MAX(RS.max_dtu) AS max_dtu,
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
			SUM(Log10) AS Log10,
			SUM(Log20) AS Log20,
			SUM(Log30) AS Log30,
			SUM(Log40) AS Log40,
			SUM(Log50) AS Log50,
			SUM(Log60) AS Log60,
			SUM(Log70) AS Log70,
			SUM(Log80) AS Log80,
			SUM(Log90) AS Log90,
			SUM(Log100) AS Log100
	FROM dbo.AzureDBResourceStats_Raw RS
	CROSS APPLY dbo.DateGroupingMins(RS.end_time,60) DG
	WHERE RS.InstanceID=@InstanceID
	AND RS.end_time>=@60MINFrom
	GROUP BY RS.InstanceID,DG.DateGroup
	OPTION(OPTIMIZE FOR(@60MINFrom='99991231'))
END;

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
OPTION(OPTIMIZE FOR(@MaxDate='99991231'))

COMMIT

EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID, 
                             @Reference = 'AzureDBResourceStats', 
                             @SnapshotDate = @SnapshotDate