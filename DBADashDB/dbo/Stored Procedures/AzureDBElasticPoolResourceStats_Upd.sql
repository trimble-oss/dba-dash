CREATE PROC dbo.AzureDBElasticPoolResourceStats_Upd(
	@AzureDBElasticPoolResourceStats dbo.AzureDBElasticPoolResourceStats READONLY,
	@InstanceID INT,
	@SnapshotDate DATETIME2(3)
)
AS
SET XACT_ABORT ON
DECLARE @Ref VARCHAR(100)='AzureDBElasticPoolResourceStats'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN
	CREATE TABLE #Pools(
		elastic_pool_name NVARCHAR(128) PRIMARY KEY,
		elastic_pool_dtu_limit INT,
		elastic_pool_cpu_limit DECIMAL (5, 2),
		first_end_time DATETIME2(7)
	)


	DECLARE @MaxDate DATETIME2(7);
	DECLARE @MinDate DATETIME2(7);
	DECLARE @AggFrom DATETIME2(7);
	DECLARE @PoolUpdates INT=0;

	SELECT @MinDate = MIN(end_time)
	FROM @AzureDBElasticPoolResourceStats S

	SELECT @AggFrom = DG.DateGroup
	FROM dbo.DateGroupingMins(@MinDate,60) DG;

	WITH pools AS (
		SELECT	elastic_pool_name,
				elastic_pool_dtu_limit,
				elastic_pool_cpu_limit,
				FIRST_VALUE(end_time) OVER(PARTITION BY elastic_pool_name,elastic_pool_dtu_limit,elastic_pool_cpu_limit ORDER BY end_time) first_end_time, /* First occurrence of this config */
				ROW_NUMBER() OVER(PARTITION BY elastic_pool_name ORDER BY end_time DESC) rnum
		FROM @AzureDBElasticPoolResourceStats
	)
	INSERT INTO #Pools(elastic_pool_name,elastic_pool_dtu_limit,elastic_pool_cpu_limit,first_end_time)
	SELECT	p.elastic_pool_name,
			p.elastic_pool_dtu_limit,
			p.elastic_pool_cpu_limit,
			p.first_end_time
	FROM pools p
	WHERE p.rnum=1

	/* Update existing pools */
	UPDATE EP
		 SET EP.elastic_pool_dtu_limit = T.elastic_pool_dtu_limit,
			EP.elastic_pool_cpu_limit = T.elastic_pool_cpu_limit,
			EP.ValidFrom = T.first_end_time,
			EP.ValidTo = NULL /* The pool is active as it's contained in #Pools, so set ValidTo to NULL just in case it was previously 'deleted' */
	/* Track updates */
	OUTPUT Inserted.PoolID, DELETED.elastic_pool_dtu_limit,DELETED.elastic_pool_cpu_limit,INSERTED.elastic_pool_dtu_limit,Inserted.elastic_pool_cpu_limit,ISNULL(Deleted.ValidFrom,'19000101'),Inserted.ValidFrom
		INTO dbo.AzureDBElasticPoolHistory(PoolID,elastic_pool_dtu_limit_old,elastic_pool_cpu_limit_old,elastic_pool_dtu_limit_new,elastic_pool_cpu_limit_new,ValidFrom,ValidTo)
	FROM dbo.AzureDBElasticPool EP
	JOIN #Pools T ON EP.elastic_pool_name = T.elastic_pool_name
	WHERE EP.InstanceID = @InstanceID
	AND (	
			/* Limits have changed */
			EXISTS(
				SELECT	T.elastic_pool_dtu_limit,
						T.elastic_pool_cpu_limit
				EXCEPT 
				SELECT	EP.elastic_pool_dtu_limit,
						EP.elastic_pool_cpu_limit
				)
			/* or pool was previously deactivated */
			OR EP.ValidTo IS NOT NULL 
		)
	SET @PoolUpdates += @@ROWCOUNT

	/* Insert new pools */
	INSERT INTO dbo.AzureDBElasticPool(InstanceID,elastic_pool_name,elastic_pool_dtu_limit ,elastic_pool_cpu_limit,ValidFrom )
	SELECT	@InstanceID,
			P.elastic_pool_name,
			P.elastic_pool_dtu_limit ,
			P.elastic_pool_cpu_limit,
			P.first_end_time
	FROM #Pools P
	WHERE NOT EXISTS(
					SELECT 1 
					FROM dbo.AzureDBElasticPool EP
					WHERE EP.elastic_pool_name = P.elastic_pool_name
					AND EP.InstanceID = @InstanceID
					)
	SET @PoolUpdates += @@ROWCOUNT

	/* Deactivate pools that no longer exist */
	UPDATE EP
		SET EP.ValidTo = SYSUTCDATETIME()
	FROM dbo.AzureDBElasticPool EP
	WHERE EP.InstanceID = @InstanceID
	AND NOT EXISTS(
					SELECT 1 
					FROM #Pools P
					WHERE EP.elastic_pool_name = P.elastic_pool_name
					)
	AND EP.ValidTo IS NULL

	SET @PoolUpdates += @@ROWCOUNT

	IF @PoolUpdates>0
	BEGIN
		EXEC dbo.AzureDBCounters_Upd @InstanceID = @InstanceID
	END

	SELECT @MaxDate=ISNULL(MAX(end_time),'19000101')
	FROM dbo.AzureDBElasticPoolResourceStats RS 
	JOIN dbo.AzureDBElasticPool EP ON EP.PoolID = RS.PoolID
	WHERE EP.InstanceID=@InstanceID
	AND RS.end_time >= @AggFrom

	BEGIN TRAN
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

	IF @@ROWCOUNT>0
	BEGIN

		DELETE agg
		FROM dbo.AzureDBElasticPoolResourceStats_60MIN agg
		WHERE EXISTS(SELECT 1 
					FROM dbo.AzureDBElasticPool P 
					WHERE P.PoolID = agg.PoolID
					AND P.InstanceID= @InstanceID
					)
		AND agg.end_time>=@AggFrom

		INSERT INTO dbo.AzureDBElasticPoolResourceStats_60MIN
		(
			PoolID,
			end_time,
			avg_cpu_percent,
			max_cpu_percent,
			avg_data_io_percent,
			max_data_io_percent,
			avg_log_write_percent,
			max_log_write_percent,
			avg_storage_percent,
			max_storage_percent,
			max_worker_percent,
			max_session_percent,
			elastic_pool_dtu_limit,
			elastic_pool_cpu_limit,
			elastic_pool_storage_limit_mb,
			avg_allocated_storage_percent,
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
			Log100
		)
		SELECT     PoolID,
			DG.DateGroup,
			AVG(avg_cpu_percent),
			MAX(max_cpu_percent),
			AVG(avg_data_io_percent),
			MAX(max_data_io_percent),
			AVG(avg_log_write_percent),
			MAX(max_log_write_percent),
			AVG(avg_storage_percent),
			MAX(max_storage_percent),
			MAX(max_worker_percent),
			MAX(max_session_percent),
			MAX(elastic_pool_dtu_limit),
			MAX(elastic_pool_cpu_limit),
			MAX(elastic_pool_storage_limit_mb),
			AVG(avg_allocated_storage_percent),
			AVG(avg_dtu_percent),
			MAX(max_dtu_percent),
			AVG(avg_dtu),
			MAX(max_dtu),
			SUM(DTU10),
			SUM(DTU20),
			SUM(DTU30),
			SUM(DTU40),
			SUM(DTU50),
			SUM(DTU60),
			SUM(DTU70),
			SUM(DTU80),
			SUM(DTU90),
			SUM(DTU100),
			SUM(CPU10),
			SUM(CPU20),
			SUM(CPU30),
			SUM(CPU40),
			SUM(CPU50),
			SUM(CPU60),
			SUM(CPU70),
			SUM(CPU80),
			SUM(CPU90),
			SUM(CPU100),
			SUM(Data10),
			SUM(Data20),
			SUM(Data30),
			SUM(Data40),
			SUM(Data50),
			SUM(Data60),
			SUM(Data70),
			SUM(Data80),
			SUM(Data90),
			SUM(Data100),
			SUM(Log10),
			SUM(Log20),
			SUM(Log30),
			SUM(Log40),
			SUM(Log50),
			SUM(Log60),
			SUM(Log70),
			SUM(Log80),
			SUM(Log90),
			SUM(Log100) 
		FROM dbo.AzureDBElasticPoolResourceStats_Raw R
		CROSS APPLY dbo.DateGroupingMins(R.end_time,60) DG
		WHERE R.InstanceID= @InstanceID
		AND R.end_time>=@AggFrom
		GROUP BY DG.DateGroup,R.PoolID

	END
	COMMIT
	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										 @Reference = @Ref,
										 @SnapshotDate = @SnapshotDate
END