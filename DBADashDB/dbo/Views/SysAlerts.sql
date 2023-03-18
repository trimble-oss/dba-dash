CREATE VIEW dbo.SysAlerts
AS
SELECT I.InstanceID,
		I.Instance,
		I.ConnectionID,
        I.InstanceDisplayName,
        A.id,
        A.name,
        A.message_id,
        A.severity,
        A.enabled,
        A.delay_between_responses,
		A.last_occurrence,
		calc.last_occurrence_utc,
        A.last_response,
		DATEADD(mi,I.UTCOffset,A.last_response) last_response_utc,
        A.notification_message,
        A.include_event_description,
        A.database_name,
        A.event_description_keyword,
        A.occurrence_count,
        A.count_reset,
		DATEADD(mi,I.UTCOffset,A.count_reset) count_reset_utc,
        A.job_id,
		J.name AS JobName,
        A.has_notification,
		CASE WHEN A.include_event_description & 1 = 1 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS Email,
		CASE WHEN A.include_event_description & 2 = 2 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS Pager,
		CASE WHEN A.include_event_description & 4 = 4 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS NetSend,
        A.category_id,
		A.performance_condition,
		calc.IsCriticalAlert,
		I.UTCOffset,
        I.ShowInSummary,
        A.AlertLevel,
        A.DefaultAlertLevel,
        calc.ActualAlertLevel,
        A.NotificationPeriodHrs,
        A.DefaultNotificationPeriodHrs,
        calc.ActualNotificationPeriodHrs,
        CASE    WHEN calc.ActualAlertLevel = 3 THEN 3
                WHEN calc.last_occurrence_utc >= DATEADD(HOUR,-calc.ActualNotificationPeriodHrs,GETUTCDATE()) AND calc.last_occurrence_utc < A.AcknowledgeDate THEN 5
                WHEN calc.last_occurrence_utc >= DATEADD(HOUR,-calc.ActualNotificationPeriodHrs,GETUTCDATE()) THEN calc.ActualAlertLevel
                ELSE 4 END AS AlertStatus,
        A.AcknowledgeDate
FROM dbo.Alerts A 
JOIN dbo.Instances I ON A.InstanceID = I.InstanceID
LEFT JOIN dbo.Jobs J ON J.InstanceID = I.InstanceID AND J.job_id = A.job_id
OUTER APPLY(SELECT  CASE WHEN ISNULL(A.AlertLevel,A.DefaultAlertLevel)=1 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsCriticalAlert,
                    DATEADD(mi,I.UTCOffset,A.last_occurrence) AS last_occurrence_utc,
                     ISNULL(A.AlertLevel,A.DefaultAlertLevel) AS ActualAlertLevel,
                     ISNULL(A.NotificationPeriodHrs,A.DefaultNotificationPeriodHrs) AS ActualNotificationPeriodHrs
            ) calc