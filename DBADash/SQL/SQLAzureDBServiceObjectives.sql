SELECT SO.edition,
       SO.service_objective,
       SO.elastic_pool_name 
FROM sys.database_service_objectives SO
JOIN sys.databases D ON SO.database_id = D.database_id
WHERE D.name = DB_NAME()