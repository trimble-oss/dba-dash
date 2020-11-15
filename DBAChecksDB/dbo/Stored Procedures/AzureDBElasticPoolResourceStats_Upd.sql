
CREATE PROC [dbo].[AzureDBElasticPoolResourceStats_Upd](@AzureDBElasticPoolResourceStats dbo.AzureDBElasticPoolResourceStats READONLY,@InstanceID INT,@SnapshotDate DATETIME2(3))
AS
DECLARE @Ref VARCHAR(30)='AzureDBElasticPoolResourceStats'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN
	DECLARE @MaxDate DATETIME2(7)
	INSERT INTO dbo.AzureDBElasticPool
	(
	    InstanceID,
	    elastic_pool_name
	)
	SELECT @InstanceID,elastic_pool_name 
	FROM @AzureDBElasticPoolResourceStats
	EXCEPT 
	SELECT @InstanceID,elastic_pool_name
	FROM dbo.AzureDBElasticPool
	WHERE InstanceID = @InstanceID

	SELECT @MaxDate=ISNULL(MAX(end_time),'19000101')
	FROM dbo.AzureDBElasticPoolResourceStats RS 
	JOIN dbo.AzureDBElasticPool EP ON EP.PoolID = RS.PoolID
	WHERE EP.InstanceID=@InstanceID


	INSERT INTO dbo.AzureDBElasticPoolResourceStats
	(
		PoolID,
	    start_time,
	    end_time,
	    avg_cpu_percent,
	    avg_data_io_percent,
	    avg_log_write_percent,
	    avg_storage_percent,
	    max_worker_percent,
	    max_session_percent,
	    elastic_pool_dtu_limit,
	    elastic_pool_storage_limit_mb,
	    avg_allocated_storage_percent
	)
	SELECT  EP.PoolID,
		RS.start_time,
	    RS.end_time,
	    RS.avg_cpu_percent,
	    RS.avg_data_io_percent,
	    RS.avg_log_write_percent,
	    RS.avg_storage_percent,
	    RS.max_worker_percent,
	    RS.max_session_percent,
	    RS.elastic_pool_dtu_limit,
	    RS.elastic_pool_storage_limit_mb,
	    RS.avg_allocated_storage_percent 
	FROM @AzureDBElasticPoolResourceStats RS
	JOIN dbo.AzureDBElasticPool EP ON EP.elastic_pool_name = RS.elastic_pool_name AND EP.InstanceID = @InstanceID
	WHERE RS.end_time>@MaxDate

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										 @Reference = @Ref,
										 @SnapshotDate = @SnapshotDate
END