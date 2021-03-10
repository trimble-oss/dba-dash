IF HAS_PERMS_BY_NAME('msdb..sysalerts','OBJECT','SELECT')=1
BEGIN
	SELECT id,
		name,
		message_id,
		severity,
		enabled,
		delay_between_responses,
		DATEADD(s,last_occurrence_time%100,DATEADD(mi,last_occurrence_time/100%100,DATEADD(hh,last_occurrence_time/10000,DATEADD(d,last_occurrence_date%100-1,DATEADD(mm,last_occurrence_date/100%100-1,DATEADD(yy,NULLIF(last_occurrence_date,0)/10000-1900,0)))))) last_occurrence,
		DATEADD(s,last_response_time%100,DATEADD(mi,last_response_time/100%100,DATEADD(hh,last_response_time/10000,DATEADD(d,last_response_date%100-1,DATEADD(mm,last_response_date/100%100-1,DATEADD(yy,NULLIF(last_response_date,0)/10000-1900,0)))))) as last_response,
		notification_message,
		include_event_description,
		database_name,
		event_description_keyword,
		occurrence_count,
		DATEADD(s,count_reset_time%100,DATEADD(mi,count_reset_time/100%100,DATEADD(hh,count_reset_time/10000,DATEADD(d,count_reset_date%100-1,DATEADD(mm,count_reset_date/100%100-1,DATEADD(yy,NULLIF(count_reset_date,0)/10000-1900,0)))))) as count_reset,
		job_id,
		has_notification,
		category_id,
		performance_condition
	FROM msdb..sysalerts
END