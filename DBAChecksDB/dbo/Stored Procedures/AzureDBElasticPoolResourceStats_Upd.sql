
CREATE PROC [dbo].[AzureDBElasticPoolResourceStats_Upd](@AzureDBElasticPoolResourceStats dbo.AzureDBElasticPoolResourceStats READONLY,@InstanceID INT,@SnapshotDate DATETIME2(3))
AS
DECLARE @Ref VARCHAR(30)='AzureDBElasticPoolResourceStats'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN
	DECLARE @MaxDate DATETIME2(7)
	SELECT @MaxDate=ISNULL(MAX(end_time),'19000101')
	FROM dbo.AzureDBElasticPoolResourceStats
	WHERE InstanceID=@InstanceID

	INSERT INTO AzureDBElasticPoolResourceStats
	(
		InstanceID,
	    start_time,
	    end_time,
	    elastic_pool_name,
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
	SELECT  @InstanceID,
		start_time,
	    end_time,
	    elastic_pool_name,
	    avg_cpu_percent,
	    avg_data_io_percent,
	    avg_log_write_percent,
	    avg_storage_percent,
	    max_worker_percent,
	    max_session_percent,
	    elastic_pool_dtu_limit,
	    elastic_pool_storage_limit_mb,
	    avg_allocated_storage_percent 
	FROM @AzureDBElasticPoolResourceStats
	WHERE end_time>@MaxDate

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										 @Reference = @Ref,
										 @SnapshotDate = @SnapshotDate
END