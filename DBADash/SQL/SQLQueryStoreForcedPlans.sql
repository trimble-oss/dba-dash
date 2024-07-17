WITH agg AS (
	SELECT P.query_id,
			COUNT(DISTINCT P.plan_id) AS num_plans,
			MAX(P.last_execution_time) AS last_execution_time_query
	FROM sys.query_store_plan P
	GROUP BY P.query_id 
	HAVING MAX(CAST(P.is_forced_plan AS TINYINT)) = 1
)
SELECT	DB_NAME() AS DB,
		P.query_id,
		P.plan_id,
		Q.object_id,
		ISNULL(OBJECT_NAME(Q.object_id),'') object_name,
		QT.query_sql_text,	
		P.plan_forcing_type_desc,
		P.force_failure_count,
		P.last_force_failure_reason_desc,
		agg.num_plans,
		P.last_execution_time as last_execution_time_plan,
		agg.last_execution_time_query,
		P.last_compile_start_time,
		Q.query_hash,
		P.query_plan_hash,
		P.is_parallel_plan,
		Q.query_parameterization_type_desc
FROM sys.query_store_plan P
JOIN sys.query_store_query Q ON Q.query_id = P.query_id
JOIN sys.query_store_query_text QT ON Q.query_text_id = QT.query_text_id
JOIN agg ON P.query_id = agg.query_id
WHERE P.is_forced_plan = 1