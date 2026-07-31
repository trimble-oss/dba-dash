CREATE PROC Alert.CustomSqlProcs_Get
AS
/*
	Enumerates the stored procedures in the UserAlert schema for use as custom SQL alert rules.

	Custom alert procs must be created in the UserAlert schema (requires db_ddladmin/db_owner) and
	return a single result set of EXACTLY three columns matching the expected contract:

		InstanceID		INT				NOT NULL	-- the instance the alert relates to
		AlertKey		NVARCHAR(256)	NOT NULL	-- unique key per item within the instance (used for de-dupe/resolution)
		AlertMessage	NVARCHAR(MAX)	NOT NULL	-- the alert message

	Returning zero rows for an instance/key resolves any matching active alert.

	Execution is by ordinal (INSERT ... EXEC), so column ORDER matters but names do not affect runtime.
	Names & types are validated here purely as an authoring guardrail.  Extra columns are NOT allowed - the
	result set must be exactly these three columns or the runtime INSERT ... EXEC will fail.

	Example:

		CREATE PROC UserAlert.LongRunningAgentJobs
		AS
		SET NOCOUNT ON
		SELECT	InstanceID,
				CONCAT('Job:', JobName) AS AlertKey,
				CONCAT('Job ', JobName, ' ran for ', DurationSeconds, 's') AS AlertMessage
		FROM ... -- your query against the repository DB

	IsValidSchema is a best-effort check using sys.dm_exec_describe_first_result_set_for_object.  Procs that
	use dynamic SQL / temp tables may not be describable, in which case IsValidSchema is 0 and the proc can
	still be registered (it may work at runtime - the picker warns before selecting).
*/
SET NOCOUNT ON

DECLARE @CanEdit BIT = CASE WHEN IS_ROLEMEMBER('db_owner') = 1 OR IS_ROLEMEMBER('db_ddladmin') = 1 THEN 1 ELSE 0 END;

SELECT	p.name AS ProcName,
		QUOTENAME(s.name) + '.' + QUOTENAME(p.name) AS QualifiedName,
		CAST(CASE WHEN d.ColCount = 3 AND d.HasInstanceID = 1 AND d.HasAlertKey = 1 AND d.HasMessage = 1 THEN 1 ELSE 0 END AS BIT) AS IsValidSchema,
		/* A CustomSql rule stores the proc name in AlertKey, so check that directly (indexable, no JSON parse) */
		CAST(CASE WHEN EXISTS(
				SELECT 1
				FROM Alert.Rules R
				WHERE R.Type = 'CustomSql'
				AND R.AlertKey = p.name COLLATE DATABASE_DEFAULT
			) THEN 1 ELSE 0 END AS BIT) AS InUse,
		@CanEdit AS CanEdit
FROM sys.procedures p
JOIN sys.schemas s ON p.schema_id = s.schema_id
OUTER APPLY (
	SELECT	COUNT(*) AS ColCount,
			MAX(IIF(r.column_ordinal = 1 AND r.name = 'InstanceID' AND r.system_type_name = 'int', 1, 0)) AS HasInstanceID,
			MAX(IIF(r.column_ordinal = 2 AND r.name = 'AlertKey' AND (r.system_type_name LIKE 'nvarchar%' OR r.system_type_name LIKE 'varchar%'), 1, 0)) AS HasAlertKey,
			MAX(IIF(r.column_ordinal = 3 AND r.name = 'AlertMessage' AND (r.system_type_name LIKE 'nvarchar%' OR r.system_type_name LIKE 'varchar%'), 1, 0)) AS HasMessage
	FROM sys.dm_exec_describe_first_result_set_for_object(p.object_id, 0) r
	WHERE r.error_number IS NULL
) d
WHERE s.name = 'UserAlert'
ORDER BY p.name
