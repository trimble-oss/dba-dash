SELECT instance_id,
		job_id,
		step_id,
		step_name,
		sql_message_id,
		sql_severity,
		message,
		run_status,
		run_date,
		run_time,
		run_duration,
		operator_id_emailed,
		operator_id_netsent,
		operator_id_paged,
		retries_attempted,
		server
FROM msdb.dbo.sysjobhistory
WHERE instance_id> @instance_id -- Get history since last run
AND run_date > @run_date
AND run_status <> 4 -- Exclude 4 In Progress