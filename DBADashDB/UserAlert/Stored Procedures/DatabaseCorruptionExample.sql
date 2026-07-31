CREATE PROC UserAlert.DatabaseCorruptionExample
AS
/*
	Example custom SQL alert proc.

	Register this proc as a 'CustomSql' alert rule (Alerts > add rule > Type = CustomSql > Procedure) to use it.
	It raises an alert for each database where corruption has been detected and not yet acknowledged.

	Custom SQL alert procs must live in the UserAlert schema and return EXACTLY three columns, by ordinal:
		InstanceID   INT            -- the instance the alert relates to
		AlertKey     NVARCHAR(256)  -- unique key per item within the instance (used for de-dupe / resolution)
		AlertMessage NVARCHAR(MAX)  -- the alert message
	Returning no row for a previously alerted item resolves that alert automatically.

	This is a starting point - copy it and adapt the query to alert on any criteria you can express against
	the repository database.  See Alert.CustomSqlAlert_Upd and Alert.CustomSqlProcs_Get.
*/
SET NOCOUNT ON

SELECT	D.InstanceID,
		CONCAT('Corruption:', D.name, ':', C.SourceTable) AS AlertKey,   /* unique per database + source */
		CONCAT('Corruption detected in database [', D.name, '] - ',
				ISNULL(C.CountOfRows, 0), ' row(s) reported by ',
				CASE C.SourceTable WHEN 1 THEN 'msdb.dbo.suspect_pages'
								   WHEN 2 THEN 'sys.dm_db_mirroring_auto_page_repair'
								   WHEN 3 THEN 'sys.dm_hadr_auto_page_repair'
								   ELSE 'unknown source' END,
				'. Last updated ', CONVERT(VARCHAR(30), C.UpdateDate, 120), ' UTC.') AS AlertMessage
FROM dbo.Corruption C
JOIN dbo.Databases D ON D.DatabaseID = C.DatabaseID
WHERE C.AckDate IS NULL      /* unacknowledged corruption only */
AND D.IsActive = 1
