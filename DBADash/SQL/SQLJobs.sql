SELECT	J.job_id,
		S.name AS originating_server,
		J.name,
		J.enabled,
		J.description,
		J.start_step_id,
		J.category_id,
		C.name AS category,
		SUSER_SNAME(J.owner_sid) AS owner,
		J.notify_level_eventlog,
		J.notify_level_email,
		J.notify_level_netsend,
		J.notify_level_page,
		ISNULL(EmailOp.name,'') AS notify_email_operator,
		ISNULL(NetOp.name,'') AS notify_netsend_operator,
		ISNULL(PageOp.name,'') AS notify_page_operator,
		J.delete_level,
		J.date_created,
		J.date_modified,
		J.version_number,
		CASE WHEN EXISTS(SELECT * FROM msdb.dbo.sysjobschedules JS WHERE JS.job_id = J.job_id) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS has_schedule,
		CASE WHEN EXISTS(SELECT * FROM msdb.dbo.sysjobservers JS WHERE JS.job_id = J.job_id) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS has_server,
		CASE WHEN EXISTS(SELECT 1 FROM msdb.dbo.sysjobsteps JS WHERE JS.job_id = J.job_id) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END has_step,
		CAST(NULL AS BINARY(32)) AS DDLHash, /* Populated using SMO */
		CAST(NULL AS VARBINARY(MAX)) AS DDL /* Populated using SMO */
FROM msdb.dbo.sysjobs J 
LEFT JOIN [msdb].[sys].[servers] AS S ON J.originating_server_id = S.server_id AND J.originating_server_id<>0 /* Return NULL for this server like SMO */
LEFT JOIN msdb.dbo.syscategories C ON C.category_id = J.category_id
LEFT JOIN msdb.dbo.sysoperators EmailOp ON J.notify_email_operator_id = EmailOp.id
LEFT JOIN msdb.dbo.sysoperators NetOp ON J.notify_netsend_operator_id = NetOp.id
LEFT JOIN msdb.dbo.sysoperators PageOp ON J.notify_page_operator_id = PageOp.id