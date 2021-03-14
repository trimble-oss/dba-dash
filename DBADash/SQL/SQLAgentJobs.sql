SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
WITH T
AS (SELECT j.job_id,
           j.name,
           MAX(   CASE
                      WHEN jh.step_id = 0
                      AND  jh.run_status <> 1 THEN
                          dt.RunDateTime
                      ELSE
                          NULL
                  END
              ) LastFail,
           MAX(   CASE
                      WHEN jh.step_id = 0
                      AND  jh.run_status = 1 THEN
                          dt.RunDateTime
                      ELSE
                          NULL
                  END
              ) LastSucceed,
           SUM(   CASE
                      WHEN jh.step_id = 0
                      AND  jh.run_status <> 1
                      AND  dt.RunDateTime >= DATEADD(hh, -24, GETDATE()) THEN
                          1
                      ELSE
                          0
                  END
              ) FailCount24Hrs,
           SUM(   CASE
                      WHEN jh.step_id = 0
                      AND  jh.run_status = 1
                      AND  dt.RunDateTime >= DATEADD(hh, -24, GETDATE()) THEN
                          1
                      ELSE
                          0
                  END
              ) SucceedCount24Hrs,
           SUM(   CASE
                      WHEN jh.step_id = 0
                      AND  jh.run_status <> 1
                      AND  dt.RunDateTime >= DATEADD(d, -7, GETDATE()) THEN
                          1
                      ELSE
                          0
                  END
              ) FailCount7Days,
           SUM(   CASE
                      WHEN jh.step_id = 0
                      AND  jh.run_status = 1
                      AND  dt.RunDateTime >= DATEADD(d, -7, GETDATE()) THEN
                          1
                      ELSE
                          0
                  END
              ) SucceedCount7Days,
           SUM(   CASE
                      WHEN jh.step_id <> 0
                      AND  jh.run_status <> 1
                      AND  dt.RunDateTime >= DATEADD(d, -7, GETDATE()) THEN
                          1
                      ELSE
                          0
                  END
              ) AS JobStepFails7Days,
           SUM(   CASE
                      WHEN jh.step_id <> 0
                      AND  jh.run_status <> 1
                      AND  dt.RunDateTime >= DATEADD(hh, -24, GETDATE()) THEN
                          1
                      ELSE
                          0
                  END
              ) AS JobStepFails24Hrs,
           j.enabled,
           MAX(   CASE
                      WHEN jh.step_id = 0 THEN
                          dt.RunDurationSec
                      ELSE
                          NULL
                  END
              ) AS MaxDurationSec,
           AVG(   CASE
                      WHEN jh.step_id = 0 THEN
                          dt.RunDurationSec
                      ELSE
                          NULL
                  END
              ) AS AvgDurationSec,
           j.start_step_id,
           j.category_id,
           CONVERT(VARCHAR(MAX), j.owner_sid, 2) owner_sid_string,
           j.notify_email_operator_id,
           j.notify_netsend_operator_id,
           j.notify_page_operator_id,
           j.notify_level_eventlog,
           j.notify_level_email,
           j.notify_level_netsend,
           j.notify_level_page,
           j.date_created,
           j.date_modified,
           j.description,
		   j.delete_level,
		   j.version_number
    FROM msdb.dbo.sysjobs j
        LEFT JOIN msdb.dbo.sysjobhistory jh ON j.job_id = jh.job_id
        OUTER APPLY
    (
        SELECT	DATEADD(s,run_time%100,DATEADD(mi,run_time/100%100,DATEADD(hh,run_time/10000,DATEADD(d,run_date%100-1,DATEADD(mm,run_date/100%100-1,DATEADD(yy,NULLIF(run_date,0)/10000-1900,0)))))) as RunDateTime,
               ((jh.run_duration / 1000000) * 86400)
               + (((jh.run_duration - ((jh.run_duration / 1000000) * 1000000)) / 10000) * 3600)
               + (((jh.run_duration - ((jh.run_duration / 10000) * 10000)) / 100) * 60)
               + (jh.run_duration - (jh.run_duration / 100) * 100) AS RunDurationSec
    ) dt
    GROUP BY j.job_id,
             j.name,
             j.enabled,
             j.start_step_id,
             j.category_id,
             j.owner_sid,
             j.owner_sid,
             j.notify_email_operator_id,
             j.notify_netsend_operator_id,
             j.notify_page_operator_id,
             j.description,
             j.date_modified,
             j.date_created,
             j.notify_level_eventlog,
             j.notify_level_email,
             j.notify_level_netsend,
             j.notify_level_page,
			 j.delete_level,
			 j.version_number)
SELECT T.job_id,
       T.name,
       T.enabled,
       T.LastFail,
       T.LastSucceed,
       T.FailCount24Hrs,
       T.SucceedCount24Hrs,
       T.FailCount7Days,
       T.SucceedCount7Days,
       T.JobStepFails7Days,
       T.JobStepFails24Hrs,
       T.MaxDurationSec,
       T.AvgDurationSec,
       CASE
           WHEN LastFail > ISNULL(LastSucceed, 0) THEN
               1
           ELSE
               0
       END AS IsLastFail,
       T.start_step_id,
       T.category_id,
       T.owner_sid_string,
       T.notify_email_operator_id,
       T.notify_netsend_operator_id,
       T.notify_page_operator_id,
	   T.notify_level_eventlog,
	   T.notify_level_email,
	   T.notify_level_netsend,
	   T.notify_level_page,
       T.date_created,
       T.date_modified,
       T.description,
	   T.delete_level,
	   T.version_number
FROM T
ORDER BY T.date_modified DESC;