CREATE PROC Alert.CollectionDatesAlert_Upd
AS
/* 
	Get instances that fail the CollectionDates alert rule & update the active alerts
*/
DECLARE @Type VARCHAR(50)='CollectionDates';

/* Check if we have any rules to process */
IF NOT EXISTS(
	SELECT 1 
	FROM Alert.Rules
	WHERE Type = @Type
)
BEGIN
	PRINT CONCAT('No rules of type ',@Type,' to process');
	RETURN;
END
PRINT CONCAT('Processing alerts of type ',@Type)

CREATE TABLE #ExceededThreshold(
	InstanceID INT NOT NULL,
	Priority INT NOT NULL,
	AlertKey NVARCHAR(128) NOT NULL,
	Reference VARCHAR(100) NOT NULL,
	Threshold DECIMAL(28,9) NOT NULL,
	RuleID INT NOT NULL,
	SnapshotAge INT NOT NULL
)
INSERT INTO #ExceededThreshold
(
    InstanceID,
    Priority,
	AlertKey,
    Reference,
    Threshold,
    RuleID,
	SnapshotAge
)
SELECT 	I.InstanceID,
		R.Priority,	
		R.AlertKey,
		CDS.Reference,
		CASE WHEN Calc.UseCriticalStatus=1 AND CDS.CriticalThreshold<R.Threshold THEN CDS.CriticalThreshold ELSE R.Threshold END AS Threshold,
		R.RuleID,
		CDS.SnapshotAge
FROM Alert.Rules R 
OUTER APPLY(SELECT 	NULLIF(TRY_CAST(JSON_VALUE(R.Details,'$.Reference') AS VARCHAR(100)),'') AS Reference,
					ISNULL(TRY_CAST(JSON_VALUE(R.Details,'$.UseCriticalStatus') AS BIT),0) AS UseCriticalStatus
		) AS Calc
CROSS APPLY Alert.ApplicableInstances_Get(R.ApplyToTagID,R.ApplyToInstanceID,R.AlertKey) I
JOIN dbo.CollectionDatesStatus CDS ON CDS.InstanceID = I.InstanceID 
		AND (CDS.Status=1 OR Calc.UseCriticalStatus=0)
		AND (CDS.SnapshotAge >= R.Threshold OR R.Threshold IS NULL)
		AND (CDS.Reference = Calc.Reference OR Calc.Reference IS NULL)
WHERE R.Type = @Type
AND R.IsActive=1

DECLARE @MaxRows INT=10
DECLARE @AlertDetails Alert.AlertDetails;

WITH dedupe AS (
	/* 1 row per instance */
	SELECT InstanceID,
			Priority,
			RuleID,
			AlertKey,
			SnapshotAge,
			COUNT(*) OVER(PARTITION BY InstanceID,AlertKey) cnt,
			ROW_NUMBER() OVER(PARTITION BY InstanceID,AlertKey ORDER BY Priority) rnum
	FROM #ExceededThreshold
)
INSERT INTO @AlertDetails(
		InstanceID,
		Priority,
		Message,
		AlertKey,
		RuleID
)
SELECT dedupe.InstanceID,
	dedupe.Priority,
	STUFF((SELECT TOP(@MaxRows) CONCAT('
',ET2.Reference,' collection age is ',SnapshotAge,'mins, exceeding the specified threshold (', FORMAT(ET2.Threshold,'N0'),'mins).')
	FROM #ExceededThreshold ET2
	WHERE dedupe.InstanceID = ET2.InstanceID
	AND dedupe.AlertKey = ET2.AlertKey
	ORDER BY ET2.Priority
	FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,2,'')
	+ IIF(dedupe.cnt>@MaxRows,'
...',''),
	dedupe.AlertKey,
	dedupe.RuleID
FROM dedupe
WHERE rnum=1

EXEC Alert.ActiveAlerts_Upd @AlertDetails=@AlertDetails,@AlertType=@Type


