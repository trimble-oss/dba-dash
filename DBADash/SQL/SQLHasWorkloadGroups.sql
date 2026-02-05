SELECT CASE WHEN EXISTS(SELECT 1 
						FROM sys.resource_governor_workload_groups 
						WHERE group_id > 2 /* Ignore internal/default groups */
						) 
						THEN CAST(1 AS BIT) 
						ELSE CAST(0 AS BIT) 
						END AS HasWorkloadGroups