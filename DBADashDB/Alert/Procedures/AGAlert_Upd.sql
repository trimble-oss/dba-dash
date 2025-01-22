CREATE PROC Alert.AGAlert_Upd
AS
/* 
	Get instances that fail the AGHealth alert rule & update the active alerts
*/
SET NOCOUNT ON
DECLARE @Type VARCHAR(50)='AGHealth';

/* Check if we have any rules to process */
IF NOT EXISTS(
	SELECT 1 
	FROM Alert.Rules
	WHERE Type = @Type
)
BEGIN
	PRINT CONCAT('No rules of type ',@Type,' to process')
	RETURN;
END
PRINT CONCAT('Processing alerts of type ',@Type)

DECLARE @AlertDetails Alert.AlertDetails;
DECLARE @MaxDBs INT=10;
CREATE TABLE #Unhealthy(
	InstanceID INT NOT NULL,
	DB NVARCHAR(128) COLLATE DATABASE_DEFAULT NOT NULL,
	synchronization_state_desc NVARCHAR(60) COLLATE DATABASE_DEFAULT NULL,
	synchronization_health_desc NVARCHAR(60) COLLATE DATABASE_DEFAULT NULL,
	Priority TINYINT NOT NULL,
	AlertKey NVARCHAR(128) COLLATE DATABASE_DEFAULT NOT NULL,
	RuleID INT NOT NULL
)
CREATE TABLE #Instances(
	InstanceID INT NOT NULL PRIMARY KEY,
	AlertKey NVARCHAR(128) COLLATE DATABASE_DEFAULT NOT NULL,
	Priority INT NOT NULL,
	RuleID INT NOT NULL
);
/* Get the AG rules that apply to each instance, ensuring thewe have a single rule per instance. */
WITH DeDupe AS (
	SELECT I.InstanceID,
			R.AlertKey,
			R.Priority,
			R.RuleID,
			ROW_NUMBER() OVER(PARTITION BY I.InstanceID ORDER BY R.Priority, R.RuleID) rnum
	FROM Alert.Rules R
	CROSS APPLY Alert.ApplicableInstances_Get(R.ApplyToTagID,R.ApplyToInstanceID,R.AlertKey,R.ApplyToHidden) I
	WHERE R.Type = @Type
	AND R.IsActive=1
)
INSERT INTO #Instances(
    InstanceID,
	AlertKey,
	Priority,
	RuleID
)
SELECT	InstanceID,
		AlertKey,
		Priority,
		RuleID 
FROM DeDupe 
WHERE rnum=1

INSERT INTO #Unhealthy(
    InstanceID,
    DB,
    synchronization_state_desc,
    synchronization_health_desc,
	Priority,
	AlertKey,
	RuleID
)
SELECT	I.InstanceID, 
		D.name,
		HADR.synchronization_state_desc,
		HADR.synchronization_health_desc,
		CASE WHEN HADR.synchronization_health_desc='NOT_HEALTHY' THEN T.Priority ELSE T.Priority+1 END AS Priority,
		T.AlertKey,
		T.RuleID
FROM dbo.DatabasesHADR HADR
JOIN dbo.Databases D ON D.DatabaseID = HADR.DatabaseID
JOIN dbo.Instances I ON I.InstanceID = D.InstanceID
JOIN #Instances T ON I.InstanceID = T.InstanceID
WHERE (
		HADR.synchronization_state NOT IN(1,2)
		OR HADR.synchronization_health <> 2
		);

/* Convert the per database AG health data into a single row per instance */
INSERT INTO @AlertDetails
(
    InstanceID,
    Priority,
	AlertKey,
    Message
)
SELECT I.InstanceID,
		MIN(I.Priority),
		MIN(I.AlertKey),
		CONCAT(COUNT(*),' Databases are not in a healthy state:
',
		STUFF((SELECT TOP(@MaxDBs) CONCAT('
Database ',U.DB,' status is ',U.synchronization_health_desc,' with synchronization state ',U.synchronization_state_desc)
				FROM #Unhealthy U 
				WHERE U.InstanceID =  I.InstanceID
				ORDER BY U.Priority,U.DB
				FOR XML PATH,TYPE).value('.','NVARCHAR(MAX)'),1,2,''),
		IIF(COUNT(*)>@MaxDBs,'
...','')) AS Message
FROM #Unhealthy I
GROUP BY I.InstanceID

EXEC Alert.ActiveAlerts_Upd @AlertDetails=@AlertDetails,@AlertType=@Type