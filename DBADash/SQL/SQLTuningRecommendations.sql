/* -- Uncomment for debugging
DECLARE @TOP INT = 10
DECLARE @Sort NVARCHAR(100) = 'estimated_gain'
DECLARE @SortDesc BIT = 1
DECLARE @DB SYSNAME = NULL
DECLARE @MinScore INT = 0
DECLARE @CurrentState VARCHAR(100) = NULL /* Active, Verifying, Success, Reverted, Expired */
DECLARE @CurrentStateReason VARCHAR(100) = NULL /* SchemaChanged,StatisticsChanged,ForcingFailed,AutomaticTuningOptionDisabled,UnsupportedStatementType,LastGoodPlanForced,AutomaticTuningOptionNotEnabled,VerificationAborted,VerificationForcedQueryRecompile,PlanForcedByUser,PlanUnforcedByUser */
*/
IF CAST(SERVERPROPERTY('ProductMajorVersion') AS INT) < 14 AND SERVERPROPERTY('EngineEdition') IN(1,2,3,4)
BEGIN
	RAISERROR('sys.dm_db_tuning_recommendations isn''t available on this version of SQL Server',11,1)
	RETURN
END
IF SERVERPROPERTY('EngineEdition') IN(1,2,4) 
BEGIN
	RAISERROR('sys.dm_db_tuning_recommendations isn''t populated for Standard & Express editions',11,1)
	RETURN
END

DECLARE @SortSQL NVARCHAR(MAX)
SELECT @SortSQL = 'ORDER BY ' + CASE WHEN @Sort IN('estimated_gain','score','regressed_plan_cpu_time_average','recommended_plan_cpu_time_average','regressed_plan_execution_count','recommended_plan_execution_count','last_execution_time','regressed_plan_last_execution_time','recommended_plan_last_execution_time') THEN @Sort ELSE NULL END 
					+ CASE WHEN @SortDesc=1 THEN ' DESC' ELSE '' END

IF @SortSQL IS NULL
BEGIN
	RAISERROR('Invalid sort',11,1)
	RETURN
END

CREATE TABLE #tuning (
	DB SYSNAME NOT NULL,
	name NVARCHAR(4000) NULL,
	type NVARCHAR(4000) NULL,
	valid_since DATETIME2 NULL,
	last_refresh DATETIME2 NULL,
	query_sql_text NVARCHAR(MAX) NULL,
	object_name NVARCHAR(128) NULL,
	reason NVARCHAR(4000) NULL,
	score INT NULL,
	query_id BIGINT NULL,
	regressed_plan_id BIGINT NULL,
	recommended_plan_id BIGINT NULL,
	last_execution_time DATETIME2(7),
	regressed_plan_last_execution_time DATETIME2(7),
	recommended_plan_last_execution_time DATETIME2(7),
	current_state NVARCHAR(4000) NULL,
	current_state_reason NVARCHAR(4000) NULL,
	script NVARCHAR(4000) NULL,
	estimated_gain FLOAT NULL,
	error_prone VARCHAR(3) NOT NULL,
    regressed_plan_execution_count INT NULL,
	recommended_plan_execution_count INT NULL,
	regressed_plan_cpu_time_average_ms FLOAT NULL,
	recommended_plan_cpu_time_average_ms FLOAT NULL,
	is_executable_action BIT NULL,
	is_revertable_action BIT NULL,
	execute_action_start_time DATETIME2(7) NULL,
	execute_action_duration TIME(7) NULL,
	execute_action_initiated_by NVARCHAR(4000) NULL,
	execute_action_initiated_time DATETIME2(7) NULL,
	revert_action_start_time DATETIME2(7) NULL,
	revert_action_duration TIME(7) NULL,
	revert_action_initiated_by NVARCHAR(4000) NULL,
	revert_action_initiated_time DATETIME2(7) NULL,
	query_hash BINARY(8) NULL,
	recommended_plan_hash BINARY(8) NULL,
	regressed_plan_hash BINARY(8) NULL
) 
DECLARE DBs CURSOR FAST_FORWARD READ_ONLY LOCAL FOR
	SELECT name
	FROM sys.databases
	WHERE state  = 0
	AND HAS_DBACCESS(name)=1
	AND is_query_store_on=1
	AND (name = @DB OR @DB IS NULL)
	AND compatibility_level >= 130 /* 2016 or later compatibility level is required */

OPEN DBs
DECLARE @SQL NVARCHAR(MAX)
WHILE 1=1
BEGIN
	FETCH NEXT FROM DBs INTO @DB
	IF @@FETCH_STATUS<>0
		BREAK

	SET @SQL = CONCAT('USE ',QUOTENAME(@DB),'
	SELECT  TOP(@TOP)
			DB_NAME() AS DB,
			dtr.name,
			dtr.type,
			dtr.valid_since,
			dtr.last_refresh,
			qsqt.query_sql_text,
			OBJECT_NAME(qsq.object_id) as object_name,
			dtr.reason,
			dtr.score,
			d.query_id,
			d.regressed_plan_id,
			d.recommended_plan_id,
			qsq.last_execution_time,
			reg.last_execution_time AS regressed_plan_last_execution_time,
			rec.last_execution_time AS recommended_plan_last_execution_time,			
			JSON_VALUE(STATE, ''$.currentValue'') AS current_state,
			JSON_VALUE(STATE, ''$.reason'') AS current_state_reason,
			JSON_VALUE(details, ''$.implementationDetails.script'') AS script,
			(regressed_plan_execution_count + recommended_plan_execution_count) *
								(regressed_plan_cpu_time_average - recommended_plan_cpu_time_average) / 1000000 AS estimated_gain,
			IIF(regressed_plan_error_count > recommended_plan_error_count, ''YES'', ''NO'') error_prone,
			regressed_plan_execution_count,
			recommended_plan_execution_count,
			regressed_plan_cpu_time_average / 1000.0 AS regressed_plan_cpu_time_average_ms,
			recommended_plan_cpu_time_average / 1000.0 AS recommended_plan_cpu_time_average_ms,
			dtr.is_executable_action,
			dtr.is_revertable_action,
			dtr.execute_action_start_time,
			dtr.execute_action_duration,
			dtr.execute_action_initiated_by,
			dtr.execute_action_initiated_time,
			dtr.revert_action_start_time,
			dtr.revert_action_duration,
			dtr.revert_action_initiated_by,
			dtr.revert_action_initiated_time,
			qsq.query_hash,
			rec.query_plan_hash AS recommended_plan_hash,
			reg.query_plan_hash AS regressed_plan_hash
	FROM sys.dm_db_tuning_recommendations dtr
	CROSS APPLY OPENJSON(Details, ''$.planForceDetails'') WITH (
			query_id BIGINT ''$.queryId'',
			regressed_plan_id BIGINT ''$.regressedPlanId'',
			recommended_plan_id BIGINT ''$.recommendedPlanId'',
			regressed_plan_error_count INT ''$.regressedPlanErrorCount'',
			recommended_plan_error_count INT ''$.recommendedPlanErrorCount'',
			regressed_plan_execution_count INT ''$.regressedPlanExecutionCount'',
			regressed_plan_cpu_time_average FLOAT ''$.regressedPlanCpuTimeAverage'',
			recommended_plan_execution_count INT ''$.recommendedPlanExecutionCount'',
			recommended_plan_cpu_time_average FLOAT ''$.recommendedPlanCpuTimeAverage''
			)
	 d
	CROSS APPLY(SELECT	JSON_VALUE(STATE, ''$.currentValue'') AS current_state,
						JSON_VALUE(STATE, ''$.reason'') AS current_state_reason,
						JSON_VALUE(details, ''$.implementationDetails.script'') AS script
						) AS calc
	INNER JOIN sys.query_store_query AS qsq ON qsq.query_id = d.query_id
	INNER JOIN sys.query_store_query_text AS qsqt ON qsqt.query_text_id = qsq.query_text_id
	INNER JOIN sys.query_store_plan AS rec ON rec.plan_id = d.recommended_plan_id
	INNER JOIN sys.query_store_plan AS reg ON reg.plan_id = d.regressed_plan_id
	WHERE dtr.score >= @MinScore
	', CASE WHEN @CurrentState IS NULL THEN '' ELSE 'AND calc.current_state = @CurrentState' END, '
	', CASE WHEN @CurrentStateReason IS NULL THEN '' ELSE 'AND calc.current_state_reason = @CurrentStateReason' END, '
	',@SortSQL)

	INSERT INTO #tuning(DB,
						name,
						type,
						valid_since,
						last_refresh,
						query_sql_text,
						object_name,
						reason,
						score,
						query_id,
						regressed_plan_id,
						recommended_plan_id,
						last_execution_time,
						regressed_plan_last_execution_time,
						recommended_plan_last_execution_time,
						current_state,
						current_state_reason,
						script,
						estimated_gain,
						error_prone,
						regressed_plan_execution_count,
						recommended_plan_execution_count,
						regressed_plan_cpu_time_average_ms,
						recommended_plan_cpu_time_average_ms,
						is_executable_action,
						is_revertable_action,
						execute_action_start_time,
						execute_action_duration,
						execute_action_initiated_by,
						execute_action_initiated_time,
						revert_action_start_time,
						revert_action_duration,
						revert_action_initiated_by,
						revert_action_initiated_time,
						query_hash,
						recommended_plan_hash,
						regressed_plan_hash
	)
	EXEC sp_executesql @SQL,N'@TOP INT,@CurrentState VARCHAR(100),@CurrentStateReason VARCHAR(100), @MinScore INT',@TOP,@CurrentState,@CurrentStateReason, @MinScore

END

SET @SQL = CONCAT(N'
SELECT	TOP(@TOP)
		DB,
		name,
		type,
		query_sql_text,
		object_name,
		reason,
		score,
		query_id,
		regressed_plan_id,
		recommended_plan_id,
		last_execution_time,
		regressed_plan_last_execution_time,
		recommended_plan_last_execution_time,
		current_state,
		current_state_reason,
		script,
		estimated_gain,
		error_prone,
		regressed_plan_execution_count,
		recommended_plan_execution_count,
		regressed_plan_cpu_time_average_ms,
		recommended_plan_cpu_time_average_ms,
		is_executable_action,
		is_revertable_action,
		valid_since,
		last_refresh,
		execute_action_start_time,
		execute_action_duration,
		execute_action_initiated_by,
		execute_action_initiated_time,
		revert_action_start_time,
		revert_action_duration,
		revert_action_initiated_by,
		revert_action_initiated_time,
		query_hash,
		recommended_plan_hash,
		regressed_plan_hash
FROM #tuning
',@SortSQL)

EXEC sp_executesql @SQL,N'@TOP INT',@TOP
