CREATE PROC Alert.CounterAlert_Upd
AS
/* 
	Get instances that fail the Counter alert rule & update the active alerts
*/
DECLARE @Type VARCHAR(50)='Counter';

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

CREATE TABLE #EffectiveThresholds (
	InstanceID INT NOT NULL,
	RuleID INT NOT NULL,
	AlertKey NVARCHAR(128) COLLATE DATABASE_DEFAULT NOT NULL,
	EvaluationPeriodMins TINYINT NOT NULL,
	Priority TINYINT NOT NULL,
	CounterID INT NOT NULL,
	Aggregation VARCHAR(10) COLLATE DATABASE_DEFAULT NOT NULL,
	Threshold DECIMAL(28,9) NULL,
	ComparisonSymbol VARCHAR(10) COLLATE DATABASE_DEFAULT NOT NULL,
)
/* Get rule details & the instances they apply to */
INSERT INTO #EffectiveThresholds(
	InstanceID,
	RuleID,
	AlertKey,
	EvaluationPeriodMins,
	Priority,
	CounterID,
	Aggregation,
	Threshold,
	ComparisonSymbol
)
SELECT 	I.InstanceID,
		R.RuleID,
		R.AlertKey + CASE WHEN Calc.ApplyToAllInstances=CAST(1 AS BIT) THEN '{' + c.instance_name + '}' ELSE '' END,
		ISNULL(R.EvaluationPeriodMins,5),
		R.Priority,
		C.CounterID,
		Calc.Aggregation,
		R.Threshold,
		Calc.ComparisonSymbol
FROM Alert.Rules R 
OUTER APPLY(SELECT TRY_CAST(JSON_VALUE(R.Details,'$.Counter.ObjectName') AS NVARCHAR(128)) AS ObjectName,
		TRY_CAST(JSON_VALUE(R.Details,'$.Counter.CounterName') AS NVARCHAR(128)) AS CounterName,
		TRY_CAST(JSON_VALUE(R.Details,'$.Counter.InstanceName') AS NVARCHAR(128)) AS InstanceName,
		TRY_CAST(JSON_VALUE(R.Details,'$.Counter.ApplyToAllInstances') AS BIT) AS ApplyToAllInstances,
		TRY_CAST(JSON_VALUE(R.Details,'$.Aggregation') AS VARCHAR(10)) AS Aggregation,
		ISNULL(TRY_CAST(JSON_VALUE(R.Details,'$.ComparisonSymbol') AS VARCHAR(10)),'>=') AS ComparisonSymbol
		) AS Calc
JOIN dbo.Counters C ON C.object_name = Calc.ObjectName 
					AND C.counter_name = Calc.CounterName 
					AND (C.instance_name = Calc.InstanceName OR Calc.ApplyToAllInstances=CAST(1 AS BIT))
CROSS APPLY Alert.ApplicableInstances_Get(R.ApplyToTagID,R.ApplyToInstanceID,R.AlertKey) I
WHERE R.Type = @Type
AND R.IsActive=1
AND Calc.Aggregation IN('MAX','AVG','SUM','MIN')

CREATE TABLE #ExceededThreshold(
	InstanceID INT NOT NULL,
	CounterID INT NOT NULL,
	AlertKey NVARCHAR(128) COLLATE DATABASE_DEFAULT NOT NULL,
	CounterValue DECIMAL(28,9) NOT NULL,
	Threshold DECIMAL(28,9) NOT NULL,
	Priority INT NOT NULL,
	RuleID INT NOT NULL,
	Aggregation VARCHAR(10) COLLATE DATABASE_DEFAULT NOT NULL,
	ComparisonSymbol VARCHAR(10) COLLATE DATABASE_DEFAULT NOT NULL,
	EvaluationPeriodMins TINYINT NOT NULL,
)

INSERT INTO #ExceededThreshold(
		InstanceID,
		CounterID,
		AlertKey,
		CounterValue,
		Threshold,
		Priority,
		RuleID,
		Aggregation,
		ComparisonSymbol,
		EvaluationPeriodMins
)
SELECT	T.InstanceID,
		T.CounterID,
		T.AlertKey,
		AGG.CounterValue,
		T.Threshold,
		T.Priority,
		T.RuleID,
		T.Aggregation,
		T.ComparisonSymbol,
		T.EvaluationPeriodMins
FROM #EffectiveThresholds T
CROSS APPLY(
			SELECT CASE WHEN T.Aggregation='MAX' THEN MAX(PC.Value)
					WHEN T.Aggregation = 'AVG' THEN AVG(PC.Value)
					WHEN T.Aggregation ='SUM' THEN SUM(PC.Value)
					WHEN T.Aggregation='MIN' THEN MIN(PC.Value)
					ELSE NULL END AS CounterValue
			FROM dbo.PerformanceCounters PC
			WHERE PC.InstanceID = T.InstanceID
			AND PC.CounterID = T.CounterID
			AND PC.SnapshotDate >= CAST(DATEADD(mi,-60,SYSUTCDATETIME()) AS DATETIME2(2))
			AND PC.SnapshotDate >= DATEADD(mi,-T.EvaluationPeriodMins,SYSUTCDATETIME())
			AND PC.SnapshotDate <= CAST(SYSUTCDATETIME() AS DATETIME2(2))
			) Agg
WHERE	(
			(Agg.CounterValue >= T.Threshold AND T.ComparisonSymbol='>=')
			OR (Agg.CounterValue > T.Threshold AND T.ComparisonSymbol='>')
			OR (Agg.CounterValue <= T.Threshold AND T.ComparisonSymbol='<=')
			OR (Agg.CounterValue < T.Threshold AND T.ComparisonSymbol='<')
			OR (Agg.CounterValue = T.Threshold AND T.ComparisonSymbol='=')
		)

DECLARE @AlertDetails Alert.AlertDetails;


INSERT INTO @AlertDetails
(
    InstanceID,
    Priority,
    Message,
	AlertKey,
	RuleID
)
SELECT	ET.InstanceID,
		ET.Priority, 
		CONCAT(ET.AlertKey,' = ',FORMAT(ET.CounterValue,'#,##0.#######'),' exceeded the specified threshold (',
					ET.Aggregation,ET.ComparisonSymbol,FORMAT(ET.Threshold,'#,##0.#######'),' over ',ET.EvaluationPeriodMins,'mins)'),
		ET.AlertKey,
		RuleID
FROM #ExceededThreshold ET

/* Duplicate AlertKey will be handled here */
EXEC Alert.ActiveAlerts_Upd @AlertDetails=@AlertDetails,@AlertType=@Type