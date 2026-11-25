CREATE PROC Alert.CustomSqlAlert_Upd
AS
/*
	Get alerts from user-defined stored procedures in the UserAlert schema & update the active alerts.

	Each 'CustomSql' rule references a proc (by name) in the UserAlert schema via Details JSON ($.ProcName).
	The proc is executed and must return EXACTLY three columns, by ordinal (names are not significant at runtime):
	InstanceID INT, AlertKey NVARCHAR(256), AlertMessage NVARCHAR(MAX).  Extra columns cause INSERT ... EXEC
	to fail (caught & logged below).

	Security: procs can only be created in UserAlert by db_ddladmin/db_owner, so an App-role user
	registering a rule cannot inject SQL - they can only reference an admin-authored proc.  The proc name
	is validated against sys.procedures (UserAlert schema) before execution.

	The proc executes in the context of the alert processing (this proc's caller) via ownership chaining
	(both Alert.* and UserAlert.* are owned by dbo).

	Robustness: if a proc errors, its currently active alerts are carried forward so a transient failure
	does not incorrectly resolve/close them.  Alerts that were waiting out the rule's resolve delay are left
	as they are: carrying them forward would make them active again, and resolving them would assume the
	issue is still clear.
*/
SET NOCOUNT ON
DECLARE @Type VARCHAR(50) = 'CustomSql';

/* Check if we have any rules to process */
IF NOT EXISTS(
	SELECT 1
	FROM Alert.Rules
	WHERE Type = @Type
	AND IsActive = 1
)
BEGIN
	PRINT CONCAT('No rules of type ', @Type, ' to process')
	RETURN;
END
PRINT CONCAT('Processing alerts of type ', @Type)
EXEC Alert.ApplicableInstances_Ensure;

DECLARE @AlertDetails Alert.AlertDetails;
DECLARE @FailedRules TABLE(RuleID INT NOT NULL PRIMARY KEY);

CREATE TABLE #ProcOutput(
	InstanceID INT NOT NULL,
	AlertKey NVARCHAR(256) COLLATE DATABASE_DEFAULT NOT NULL,
	AlertMessage NVARCHAR(MAX) COLLATE DATABASE_DEFAULT NOT NULL
);

DECLARE @RuleID INT,
		@ProcName SYSNAME,
		@Priority INT,
		@GroupID INT,
		@RuleAlertKey NVARCHAR(256),
		@ApplyToTagID INT,
		@ApplyToInstanceID INT,
		@ApplyToHidden BIT,
		@Proc NVARCHAR(300),
		@ErrMsg NVARCHAR(MAX);

DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
	SELECT	R.RuleID,
			TRY_CAST(JSON_VALUE(R.Details, '$.ProcName') AS SYSNAME),
			R.Priority,
			R.GroupID,
			R.AlertKey,
			R.ApplyToTagID,
			R.ApplyToInstanceID,
			R.ApplyToHidden
	FROM Alert.Rules R
	WHERE R.Type = @Type
	AND R.IsActive = 1;

OPEN cur
FETCH NEXT FROM cur INTO @RuleID, @ProcName, @Priority, @GroupID, @RuleAlertKey, @ApplyToTagID, @ApplyToInstanceID, @ApplyToHidden

WHILE @@FETCH_STATUS = 0
BEGIN
	/* Validate the referenced proc exists in the UserAlert schema before executing it */
	IF @ProcName IS NOT NULL
		AND EXISTS(
			SELECT 1
			FROM sys.procedures p
			JOIN sys.schemas s ON p.schema_id = s.schema_id
			WHERE s.name = 'UserAlert'
			AND p.name = @ProcName
		)
	BEGIN
		BEGIN TRY
			TRUNCATE TABLE #ProcOutput;
			SET @Proc = QUOTENAME('UserAlert') + N'.' + QUOTENAME(@ProcName);

			INSERT INTO #ProcOutput(InstanceID, AlertKey, AlertMessage)
			EXEC @Proc;

			/* Emit alerts, filtered to the instances the rule applies to (tag/instance/hidden scoping) */
			INSERT INTO @AlertDetails(InstanceID, Priority, AlertKey, Message, RuleID, GroupID)
			SELECT	PO.InstanceID,
					@Priority,
					CONCAT(@RuleAlertKey, ':', PO.AlertKey),
					PO.AlertMessage,
					@RuleID,
					@GroupID
			FROM #ProcOutput PO
			JOIN Alert.ApplicableInstances_Get(@RuleID, @ApplyToTagID, @ApplyToInstanceID, '%', @ApplyToHidden) AI
				ON AI.InstanceID = PO.InstanceID
		END TRY
		BEGIN CATCH
			SET @ErrMsg = CONCAT('Error executing custom alert proc UserAlert.', @ProcName, ' (RuleID ', @RuleID, '): ', ERROR_MESSAGE());
			PRINT @ErrMsg;
			EXEC Alert.CustomSqlAlertError_Log @ErrorMessage = @ErrMsg;
			/* Carry forward existing active alerts for this rule so a transient failure doesn't resolve them */
			INSERT INTO @AlertDetails(InstanceID, Priority, AlertKey, Message, RuleID, GroupID)
			SELECT	AA.InstanceID,
					AA.Priority,
					AA.AlertKey,
					AA.LastMessage,
					AA.RuleID,
					AA.GroupID
			FROM Alert.ActiveAlerts AA
			WHERE AA.AlertType = @Type
			AND AA.RuleID = @RuleID
			AND AA.IsResolved = 0
			AND AA.ResolvingSince IS NULL

			INSERT INTO @FailedRules(RuleID) VALUES(@RuleID)
		END CATCH
	END
	ELSE
	BEGIN
		SET @ErrMsg = CONCAT('Custom alert proc not found in UserAlert schema for RuleID ', @RuleID, ': ', ISNULL(@ProcName, '(null)'));
		PRINT @ErrMsg;
		EXEC Alert.CustomSqlAlertError_Log @ErrorMessage = @ErrMsg;
		/* Proc is missing - carry forward existing active alerts rather than resolving them silently */
		INSERT INTO @AlertDetails(InstanceID, Priority, AlertKey, Message, RuleID, GroupID)
		SELECT	AA.InstanceID,
				AA.Priority,
				AA.AlertKey,
				AA.LastMessage,
				AA.RuleID,
				AA.GroupID
		FROM Alert.ActiveAlerts AA
		WHERE AA.AlertType = @Type
		AND AA.RuleID = @RuleID
		AND AA.IsResolved = 0
		AND AA.ResolvingSince IS NULL

		INSERT INTO @FailedRules(RuleID) VALUES(@RuleID)
	END

	FETCH NEXT FROM cur INTO @RuleID, @ProcName, @Priority, @GroupID, @RuleAlertKey, @ApplyToTagID, @ApplyToInstanceID, @ApplyToHidden
END
CLOSE cur
DEALLOCATE cur

EXEC Alert.ActiveAlerts_Upd @AlertDetails = @AlertDetails, @AlertType = @Type, @ResolveAlertsOfType = 0

/* Resolve alerts that are no longer reported - except for rules whose proc failed on this run */
DECLARE @Cleared dbo.BigIDs
INSERT INTO @Cleared(ID)
SELECT AA.AlertID
FROM Alert.ActiveAlerts AA
WHERE AA.AlertType = @Type
AND NOT EXISTS(SELECT 1
			FROM @AlertDetails AD
			WHERE AD.InstanceID = AA.InstanceID
			AND AD.AlertKey = AA.AlertKey
			AND AD.GroupID = AA.GroupID
			)
AND NOT EXISTS(SELECT 1
			FROM @FailedRules FR
			WHERE FR.RuleID = AA.RuleID
			)
AND AA.IsResolved = 0
AND AA.IsBlackout = 0

EXEC Alert.ActiveAlerts_Resolve @AlertIDs = @Cleared, @AlertType = @Type

/* Resolving alerts of rules whose proc failed keep waiting.  Refresh them so Alert.Alerts_Upd doesn't close them as stale, as it doesn't the alerts carried forward */
UPDATE AA
	SET UpdatedDate = SYSUTCDATETIME()
FROM Alert.ActiveAlerts AA
WHERE AA.AlertType = @Type
AND AA.IsResolved = 0
AND AA.ResolvingSince IS NOT NULL
AND EXISTS(SELECT 1
			FROM @FailedRules FR
			WHERE FR.RuleID = AA.RuleID
			)
