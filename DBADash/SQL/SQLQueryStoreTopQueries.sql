/*
DECLARE @Database SYSNAME='DBADashDB'
DECLARE @SortCol NVARCHAR(128) = 'total_cpu_time_ms';
DECLARE @Top INT=25
DECLARE @FromDate DATETIMEOFFSET(7)=DATEADD(mi,-60,SYSUTCDATETIME())
DECLARE @ToDate DATETIMEOFFSET(7)=SYSUTCDATETIME()
DECLARE @ObjectName NVARCHAR(128) = NULL
DECLARE @ObjectID INT = NULL
DECLARE @NearestInterval BIT=1
DECLARE @GroupBy NVARCHAR(128) = 'query_hash'
DECLARE @QueryID BIGINT =NULL
DECLARE @PlanID BIGINT =NULL
DECLARE @QueryHash BINARY(8)
DECLARE @QueryPlanHash BINARY(8)
DECLARE @ParallelPlans BIT
DECLARE @IncludeWaits BIT=1
DECLARE @MinimumPlanCount INT=1
*/
SET NUMERIC_ROUNDABORT OFF

IF NOT EXISTS(
	SELECT 1 
	FROM sys.databases 
	WHERE name = @Database
)
BEGIN
	RAISERROR('Invalid database',11,1)
	RETURN
END
IF @GroupBy NOT IN('query_id','plan_id','query_plan_hash','query_hash','object_id','date_bucket')
BEGIN
	RAISERROR('Invalid group by',11,1)
	RETURN
END
IF @GroupBy = 'date_bucket' AND @QueryID IS NULL
BEGIN
	RAISERROR('Query ID must be used with date_bucket group by',11,1)
	RETURN
END
IF OBJECT_ID('sys.query_store_wait_stats') IS NULL
BEGIN
	SET @IncludeWaits=0
	PRINT 'Wait stats are not available on this version'
END
DECLARE @BucketStart NVARCHAR(MAX)
DECLARE @BucketEnd NVARCHAR(MAX)
SELECT @BucketStart = CASE WHEN DATEDIFF(mi,@FromDate,@ToDate)<65 THEN 'DATEADD(mi, DATEDIFF(mi, 0, rs.last_execution_time),0 )'
			WHEN DATEDIFF(hh,@FromDate,@ToDate)<49 THEN 'DATEADD(hh, DATEDIFF(hh, 0, rs.last_execution_time),0 )'
			WHEN DATEDIFF(d,@FromDate,@ToDate)<31 THEN 'DATEADD(d, DATEDIFF(d, 0, rs.last_execution_time),0 )'
			WHEN DATEDIFF(w,@FromDate,@ToDate)<31 THEN 'DATEADD(w, DATEDIFF(w, 0, rs.last_execution_time),0 )'
			ELSE 'DATEADD(m, DATEDIFF(m, 0, rs.last_execution_time),0 )' END
SELECT @BucketEnd = REPLACE(@BucketStart,'DATEDIFF(','1+DATEDIFF(')

DECLARE @SortSQL NVARCHAR(MAX)
SELECT @SortSQL = CASE WHEN @SortCol IN('total_cpu_time_ms',
											'avg_cpu_time_ms',
											'total_duration_ms',
											'avg_duration_ms',
											'count_executions',
											'max_memory_grant_kb',
											'total_physical_io_reads_kb',
											'avg_physical_io_reads_kb',
											'bucket_start') THEN QUOTENAME(@SortCol) ELSE NULL END
IF @SortSQL IS NULL
BEGIN
	RAISERROR('Invalid sort',11,1)
	RETURN
END
DECLARE @SupportsTuningRecommendations BIT
/* 
	Requires 130 compatibility level or later for OPENJSON
	EngineEdition should be Enterprise, AzureDB or managed instance
	2017 or later (14) compatibility level if EngineEdition is Enterprise (regular SQL instance)
*/
SELECT @SupportsTuningRecommendations = CASE	WHEN EXISTS(SELECT 1 
															FROM sys.databases 
															WHERE compatibility_level >= 130 
															AND name = @Database
														) 	
												AND SERVERPROPERTY('EngineEdition') IN(3,5,8) 
												AND (
													CAST(SERVERPROPERTY('ProductMajorVersion') AS INT) >= 14 
													OR SERVERPROPERTY('EngineEdition') <> 3
													)		
												THEN CAST(1 AS BIT)
												ELSE CAST(0 AS BIT) END

IF @SupportsTuningRecommendations=1
BEGIN
	CREATE TABLE #DTR(
		query_id BIGINT,
		recommended_plan_id BIGINT,
		regressed_plan_id BIGINT,
		PRIMARY KEY(query_id,recommended_plan_id,regressed_plan_id)
	)
END

DECLARE @SQL NVARCHAR(MAX)
SET @SQL = CONCAT(N'
USE ', QUOTENAME(@Database),'

' + CASE WHEN @SupportsTuningRecommendations = 0 THEN '' ELSE '
INSERT INTO #DTR(
		query_id,
		recommended_plan_id,
		regressed_plan_id
)
SELECT	DISTINCT /* To guarantee we don''t have duplicates */
		d.query_id,
		d.recommended_plan_id,
		d.regressed_plan_id
FROM sys.dm_db_tuning_recommendations dtr
CROSS APPLY OPENJSON(Details, ''$.planForceDetails'') WITH (
		query_id INT ''$.queryId'',
		regressed_plan_id INT ''$.regressedPlanId'',
		recommended_plan_id INT ''$.recommendedPlanId'',
		regressed_plan_error_count INT ''$.regressedPlanErrorCount'',
		recommended_plan_error_count INT ''$.recommendedPlanErrorCount'',
		regressed_plan_execution_count INT ''$.regressedPlanExecutionCount'',
		regressed_plan_cpu_time_average FLOAT ''$.regressedPlanCpuTimeAverage'',
		recommended_plan_execution_count INT ''$.recommendedPlanExecutionCount'',
		recommended_plan_cpu_time_average FLOAT ''$.recommendedPlanCpuTimeAverage''
		)
	d
WHERE dtr.type =''FORCE_LAST_GOOD_PLAN''
' END + '

DECLARE @interval_from BIGINT
DECLARE @interval_to BIGINT

SELECT  TOP(1) @interval_from= runtime_stats_interval_id
FROM sys.query_store_runtime_stats_interval
WHERE start_time<=@FromDate
ORDER BY start_time DESC

SELECT  TOP(1) @interval_to= runtime_stats_interval_id
FROM sys.query_store_runtime_stats_interval
WHERE end_time>=@ToDate
ORDER BY end_time ASC

IF @interval_from IS NULL
BEGIN
	SET @interval_from = 0
END
IF @interval_to IS NULL
BEGIN
	SET @interval_to = 9223372036854775807
END

SELECT TOP (@Top)
		DB_NAME() AS DB,
		',CASE WHEN @GroupBy = 'plan_id' THEN 'RS.plan_id,P.query_plan_hash,' ELSE '' END,'
		',CASE WHEN @GroupBy IN('query_id', 'plan_id') THEN '
		P.query_id,
		Q.object_id,
		Q.query_hash,
		ISNULL(OBJECT_NAME(Q.object_id),'''') object_name,
		QT.query_sql_text query_sql_text,'
		WHEN @GroupBy = 'query_hash' THEN 'Q.query_hash,'
		WHEN @GroupBy = 'query_plan_hash' THEN 'P.query_plan_hash,'
		WHEN @GroupBy = 'object_id' THEN 'Q.object_id,
		ISNULL(OBJECT_NAME(Q.object_id),'''') object_name,'
		WHEN @GroupBy = 'date_bucket' THEN 'RS.plan_id,P.plan_forcing_type_desc,'
		ELSE NULL END, 
		CASE WHEN @IncludeWaits = 1 THEN 'STUFF(
				(SELECT TOP(3) '', '' + CONCAT(W.wait_category_desc, '' = '',SUM(W.total_query_wait_time_ms),''ms'')
						FROM sys.query_store_wait_stats W
						JOIN sys.query_store_plan P2 ON W.plan_id = P2.plan_id
						JOIN sys.query_store_query Q2 ON Q2.query_id = P2.query_id
						WHERE W.runtime_stats_interval_id>=@interval_from
						AND W.runtime_stats_interval_id<=@interval_to
						' + CASE WHEN @GroupBy = 'query_id' THEN 'AND P2.query_id = P.query_id'
						WHEN @GroupBy = 'plan_id' THEN 'AND W.plan_id = RS.plan_id'
						WHEN @GroupBy = 'query_plan_hash' THEN 'AND P2.query_plan_hash = P.query_plan_hash'
						WHEN @GroupBy = 'query_hash' THEN 'AND Q2.query_hash = Q.query_hash'
						ELSE NULL END + '
						GROUP BY W.wait_category_desc
						ORDER BY SUM(W.total_query_wait_time_ms) DESC
						FOR XML PATH(''''),TYPE
		).value(''.'',''NVARCHAR(MAX)''),1,2,'''') AS top_waits,' ELSE '' END,'
		SUM(RS.avg_cpu_time*RS.count_executions)*0.001 total_cpu_time_ms,
		SUM(RS.avg_cpu_time*RS.count_executions)/NULLIF(SUM(RS.count_executions), 0)*0.001 avg_cpu_time_ms,
		MAX(RS.max_cpu_time)*0.001 AS max_cpu_time_ms,
		SUM(RS.avg_duration*RS.count_executions)*0.001 total_duration_ms,
		SUM(RS.avg_duration*RS.count_executions)/NULLIF(SUM(RS.count_executions), 0)*0.001 avg_duration_ms,
		MAX(RS.max_duration)*0.001 AS max_duration_ms,
		SUM(RS.count_executions) count_executions,
		SUM(CASE WHEN RS.execution_type = 3 THEN 1 ELSE 0 END) AS abort_count,
		SUM(CASE WHEN RS.execution_type = 4 THEN 1 ELSE 0 END) AS exception_count,
		SUM(RS.count_executions)*60.0/NULLIF(DATEDIFF(s, MIN(MIN(RS.first_execution_time)) OVER(),MAX(MAX(RS.last_execution_time)) OVER()),0) executions_per_min,
		SUM(RS.avg_query_max_used_memory*RS.count_executions)/NULLIF(SUM(RS.count_executions), 0)*8 as avg_memory_grant_kb,
		MAX(RS.max_query_max_used_memory)*8 max_memory_grant_kb,
		SUM(RS.avg_physical_io_reads*RS.count_executions)*8 total_physical_io_reads_kb,
		SUM(RS.avg_physical_io_reads*RS.count_executions)/NULLIF(SUM(RS.count_executions), 0)*8 avg_physical_io_reads_kb,
		SUM(RS.avg_rowcount*RS.count_executions)/NULLIF(SUM(RS.count_executions),0) AS avg_rowcount,
		MAX(RS.max_rowcount) AS max_rowcount,
		', CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.query_store_runtime_stats'),'avg_tempdb_space_used','ColumnId') IS NULL THEN '' ELSE 'SUM(RS.avg_tempdb_space_used*RS.count_executions)/NULLIF(SUM(RS.count_executions),0)*8 AS avg_tempdb_space_used_kb,' END,'
		', CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.query_store_runtime_stats'),'max_tempdb_space_used','ColumnId') IS NULL THEN '' ELSE 'MAX(RS.max_tempdb_space_used)*8 AS max_tempdb_space_used_kb,' END + '
		MAX(RS.max_dop) AS max_dop,
		', CASE WHEN @GroupBy IN('query_id','plan_id') THEN 'Q.query_parameterization_type_desc,' ELSE 'COUNT(DISTINCT Q.query_id) AS num_queries,' END, '
		', CASE WHEN @GroupBy = 'plan_id' THEN 'P.plan_forcing_type_desc,P.force_failure_count,P.last_force_failure_reason_desc,P.is_parallel_plan,' ELSE 'COUNT(DISTINCT P.plan_id) num_plans,' END, '
		' + CASE WHEN @GroupBy = 'date_bucket' THEN
		CONCAT(@BucketStart,' AS bucket_start,',@BucketEnd,' AS bucket_end')
		ELSE 'MIN(MIN(RS.first_execution_time)) OVER() interval_start,
		MAX(MAX(RS.last_execution_time)) OVER() interval_end' 
		END + '
		' + CASE WHEN @SupportsTuningRecommendations=1 AND @GroupBy <> 'plan_id' THEN ',CAST(MAX(CASE WHEN Reg.regressed_plan_id IS NOT NULL THEN 1 ELSE 0 END) AS BIT) AS has_regressed_plan' ELSE '' END + '
		' + CASE WHEN @SupportsTuningRecommendations=1 AND @GroupBy <> 'plan_id' THEN ',CAST(MAX(CASE WHEN Rec.recommended_plan_id IS NOT NULL THEN 1 ELSE 0 END) AS BIT) AS has_recommended_plan' ELSE '' END + '
		' + CASE WHEN @GroupBy = 'plan_id' AND @SupportsTuningRecommendations=1 THEN ',CASE WHEN Reg.regressed_plan_id IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END as is_regressed_plan' ELSE '' END + '
		' + CASE WHEN @GroupBy = 'plan_id' AND @SupportsTuningRecommendations=1 THEN ',CASE WHEN Rec.recommended_plan_id IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END as is_recommended_plan' ELSE '' END + '
FROM sys.query_store_runtime_stats AS RS
JOIN sys.query_store_plan AS P ON P.plan_id = RS.plan_id
JOIN sys.query_store_query AS Q  ON Q.query_id = P.query_id
JOIN sys.query_store_query_text AS QT ON Q.query_text_id = QT.query_text_id
' + CASE WHEN @SupportsTuningRecommendations=1 THEN 'LEFT JOIN #DTR Rec ON P.query_id = Rec.query_id AND P.plan_id = Rec.recommended_plan_id
LEFT JOIN #DTR Reg ON P.query_id = Reg.query_id AND P.plan_id = Reg.regressed_plan_id' ELSE '' END + '
WHERE RS.runtime_stats_interval_id >= @interval_from
AND RS.runtime_stats_interval_id <= @interval_to
', CASE WHEN @NearestInterval = 1 THEN '' ELSE 'AND NOT (RS.first_execution_time > @ToDate OR RS.last_execution_time < @FromDate)' END,'
', CASE WHEN @ObjectName IS NOT NULL THEN 'AND OBJECT_NAME(Q.object_id) = @ObjectName COLLATE DATABASE_DEFAULT' ELSE '' END,'
', CASE WHEN @ObjectID IS NOT NULL THEN 'AND Q.object_id = @ObjectID' ELSE '' END,'
', CASE WHEN @QueryID IS NOT NULL THEN 'AND P.query_id = @QueryID' ELSE '' END,'
', CASE WHEN @PlanID IS NOT NULL THEN 'AND RS.plan_id = @PlanID' ELSE '' END,'
', CASE WHEN @QueryHash IS NOT NULL THEN 'AND Q.query_hash = @QueryHash' ELSE '' END,'
', CASE WHEN @QueryPlanHash IS NOT NULL THEN 'AND P.query_plan_hash = @QueryPlanHash' ELSE '' END,'
', CASE WHEN @ParallelPlans = 1 THEN 'AND P.is_parallel_plan = 1' ELSE '' END,'
', CASE WHEN @GroupBy = 'object_id' THEN 'AND Q.object_id <> 0' ELSE '' END, '
GROUP BY ',CASE WHEN @GroupBy = 'query_id' THEN 'P.query_id, QT.query_sql_text, Q.object_id,Q.query_hash,Q.query_parameterization_type_desc'
			WHEN @GroupBy = 'query_plan_hash' THEN 'P.query_plan_hash'
			WHEN @GroupBy = 'query_hash' THEN 'Q.query_hash'
			WHEN @GroupBy = 'object_id' THEN 'Q.object_id'
			WHEN @GroupBy = 'plan_id' THEN 'P.query_id, QT.query_sql_text, Q.object_id,Q.query_hash,Q.query_parameterization_type_desc,RS.plan_id,P.query_plan_hash,P.plan_forcing_type_desc,P.force_failure_count,P.last_force_failure_reason_desc,P.is_parallel_plan,Rec.recommended_plan_id,Reg.regressed_plan_id' 
			WHEN @GroupBy = 'date_bucket' THEN CONCAT(@BucketStart,',',@BucketEnd,',RS.plan_id,P.plan_forcing_type_desc')
			ELSE NULL END, '
', CASE WHEN @MinimumPlanCount >1 THEN 'HAVING COUNT(DISTINCT RS.plan_id)>=@MinimumPlanCount' ELSE '' END,'
ORDER BY ',@SortSQL,' DESC
OPTION(HASH JOIN, LOOP JOIN)')

EXEC sp_executesql @SQL,N'@Top INT,@FromDate DATETIMEOFFSET(7),@ToDate DATETIMEOFFSET(7), @ObjectName NVARCHAR(128),@ObjectID INT,@QueryID BIGINT,@PlanID BIGINT,@QueryHash BINARY(8),@QueryPlanHash BINARY(8),@MinimumPlanCount INT',
					@Top,@FromDate,@ToDate,@ObjectName,@ObjectID,@QueryID,@PlanID,@QueryHash,@QueryPlanHash,@MinimumPlanCount

