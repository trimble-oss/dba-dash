
CREATE PROC [dbo].[AzureDBElasticPoolResourceStats_Upd](@AzureDBElasticPoolResourceStats dbo.AzureDBElasticPoolResourceStats READONLY,@InstanceID INT,@SnapshotDate DATETIME2(3))
AS
DECLARE @Ref VARCHAR(30)='AzureDBElasticPoolResourceStats'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN
	DECLARE @MaxDate DATETIME2(7);

	WITH poolUpd AS (
		SELECT elastic_pool_name,elastic_pool_dtu_limit,elastic_pool_cpu_limit,end_time,ROW_NUMBER() OVER(PARTITION BY elastic_pool_name ORDER BY end_time DESC) rnum
		FROM @AzureDBElasticPoolResourceStats
	)
	MERGE dbo.AzureDBElasticPool AS T
	USING(SELECT * FROM poolUpd WHERE rnum=1) AS S ON s.elastic_pool_name = T.elastic_pool_name AND T.InstanceID = @InstanceID
	WHEN MATCHED AND EXISTS(SELECT S.elastic_pool_dtu_limit EXCEPT SELECT T.elastic_pool_dtu_limit)
				OR EXISTS(SELECT S.elastic_pool_cpu_limit EXCEPT SELECT T.elastic_pool_cpu_limit) 
	THEN UPDATE SET T.elastic_pool_dtu_limit = S.elastic_pool_dtu_limit,
			T.elastic_pool_cpu_limit = S.elastic_pool_cpu_limit,
			T.ValidFrom=S.end_time
	WHEN NOT MATCHED THEN INSERT(InstanceID,elastic_pool_name,elastic_pool_dtu_limit ,elastic_pool_cpu_limit,ValidFrom )
	VALUES(@InstanceID,elastic_pool_name,elastic_pool_dtu_limit ,elastic_pool_cpu_limit,GETUTCDATE())
	OUTPUT Inserted.PoolID, DELETED.elastic_pool_dtu_limit,DELETED.elastic_pool_cpu_limit,INSERTED.elastic_pool_dtu_limit,Inserted.elastic_pool_cpu_limit,ISNULL(Deleted.ValidFrom,'19000101'),Inserted.ValidFrom
	INTO dbo.AzureDBElasticPoolHistory(PoolID,elastic_pool_dtu_limit_old,elastic_pool_cpu_limit_old,elastic_pool_dtu_limit_new,elastic_pool_cpu_limit_new,ValidFrom,ValidTo);

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
	    avg_allocated_storage_percent,
	    elastic_pool_cpu_limit
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
	    RS.avg_allocated_storage_percent,
		RS.elastic_pool_cpu_limit
	FROM @AzureDBElasticPoolResourceStats RS
	JOIN dbo.AzureDBElasticPool EP ON EP.elastic_pool_name = RS.elastic_pool_name AND EP.InstanceID = @InstanceID
	WHERE RS.end_time>@MaxDate

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										 @Reference = @Ref,
										 @SnapshotDate = @SnapshotDate
END