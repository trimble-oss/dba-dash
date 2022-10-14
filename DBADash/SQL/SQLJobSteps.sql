SELECT	JS.job_id,
		JS.step_name,
		JS.database_name,
		JS.step_id,
		JS.subsystem,
		JS.command,
		JS.cmdexec_success_code,
		JS.on_success_action,
		JS.on_success_step_id,
		JS.on_fail_action,
		JS.on_fail_step_id,
		ISNULL(JS.database_user_name,'') AS database_user_name,
		JS.retry_attempts,
		JS.retry_interval,
		ISNULL(JS.output_file_name,'') AS output_file_name,
		ISNULL(P.name,'') AS proxy_name
FROM msdb.dbo.sysjobsteps JS
LEFT JOIN msdb.dbo.sysproxies P ON JS.proxy_id = P.proxy_id