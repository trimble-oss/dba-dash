CREATE PROC dbo.PlanForcingLog_Get(
	@InstanceID INT,
	@Top INT=1000,
	@DB NVARCHAR(128) = NULL
)
AS
SELECT TOP(@Top) 
		database_name AS DB,
		log_date,
		log_type,
		user_name,
		query_id,plan_id,
		object_name,
		query_sql_text,
		query_hash,
		query_plan_hash,
		notes,
		status
FROM dbo.PlanForcingLog
WHERE InstanceID = @InstanceID
AND (database_name = @DB OR @DB IS NULL)
ORDER BY log_date DESC