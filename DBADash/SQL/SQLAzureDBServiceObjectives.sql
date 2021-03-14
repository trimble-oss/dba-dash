SELECT edition,
       service_objective,
       elastic_pool_name 
FROM sys.database_service_objectives
WHERE database_id = DB_ID()