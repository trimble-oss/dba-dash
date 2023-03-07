CREATE VIEW dbo.IdentityColumnsInfo
AS
SELECT	IC.InstanceID,
		IC.DatabaseID,
		I.InstanceDisplayName,
		D.name AS DB, 
		IC.SnapshotDate,
		CASE WHEN cdt.WarningThreshold IS NULL AND cdt.CriticalThreshold IS NULL THEN 3 WHEN DATEDIFF(mi,IC.SnapshotDate,GETUTCDATE()) > cdt.CriticalThreshold THEN 1 WHEN DATEDIFF(mi,IC.SnapshotDate,GETUTCDATE()) > cdt.WarningThreshold THEN 2 ELSE 4 END AS SnapshotStatus,
		IC.object_id,
		IC.schema_name,
		IC.object_name,
		IC.column_name,
		TYPE_NAME(IC.system_type_id) AS type,
		IC.increment_value,
		IC.seed_value, 
		IC.row_count,
		IC.last_value,
		IC.min_ident,
		IC.max_ident,
		IC.max_rows,
		calc2.pct_used,
		calc2.pct_free,
		CASE WHEN calc2.pct_used>THRES.PctUsedCriticalThreshold THEN 1 WHEN calc2.pct_used > THRES.PctUsedWarningThreshold THEN 2 WHEN THRES.PctUsedWarningThreshold IS NOT NULL OR THRES.PctUsedCriticalThreshold IS NOT NULL THEN 4 ELSE 3 END AS IdentityStatus,
		calc.pct_ident_used,
        calc.pct_rows_used,
		calc.remaining_ident_count,
		calc.remaining_row_count,
		PerDay.avg_ident_per_day,
        PerDay.avg_rows_per_day,
		PerDay.avg_calc_days,
		calc2.ident_estimated_days,
		calc2.row_estimated_days,
		calc3.estimated_days,
		CAST(DATEADD(d,CASE WHEN calc3.estimated_days > 36500 THEN NULL ELSE calc3.estimated_days END,IC.SnapshotDate) AS DATE) AS estimated_date,
		THRES.PctUsedWarningThreshold,
		THRES.PctUsedCriticalThreshold,
		I.ShowInSummary
FROM dbo.IdentityColumns IC
JOIN dbo.Instances I ON I.InstanceID = IC.InstanceID
JOIN dbo.Databases D ON D.DatabaseID = IC.DatabaseID
OUTER APPLY (SELECT CASE WHEN IC.last_value <0 THEN (ABS(IC.min_ident)-ABS(IC.last_value)) / IC.max_rows ELSE IC.last_value/IC.max_ident END AS pct_ident_used,
					IC.row_count/IC.max_rows AS pct_rows_used,
					CASE WHEN IC.last_value < 0 THEN ABS(IC.last_value)+IC.max_ident ELSE IC.max_ident - IC.last_value END AS remaining_ident_count,
					IC.max_rows-IC.row_count AS remaining_row_count
	
		) AS calc
OUTER APPLY(SELECT TOP(1) (IC.last_value-ICH.last_value) / (DATEDIFF(s,ICH.SnapshotDate,IC.SnapshotDate)/86400.) avg_ident_per_day,
				(IC.row_count-ICH.row_count) / (DATEDIFF(s,ICH.SnapshotDate,IC.SnapshotDate)/86400.) AS avg_rows_per_day,
				(DATEDIFF(s,ICH.SnapshotDate,IC.SnapshotDate)/86400.) AS avg_calc_days
			FROM dbo.IdentityColumnsHistory ICH
			WHERE ICH.InstanceID = IC.InstanceID 
			AND ICH.DatabaseID = IC.DatabaseID
			AND ICH.object_id = IC.object_id
			AND ICH.SnapshotDate < IC.SnapshotDate
			AND ICH.SnapshotDate > CAST(DATEADD(d,-31,GETUTCDATE()) AS DATETIME2(2))
			ORDER BY ICH.SnapshotDate
			) AS PerDay
OUTER APPLY(SELECT	calc.remaining_ident_count / CASE WHEN PerDay.avg_ident_per_day <=0 THEN NULL ELSE PerDay.avg_ident_per_day END AS ident_estimated_days,
					calc.remaining_row_count / CASE WHEN PerDay.avg_rows_per_day <=0 THEN NULL ELSE PerDay.avg_rows_per_day END AS row_estimated_days,
					(SELECT MAX(PCT.pct_used) FROM (VALUES (calc.pct_ident_used),(calc.pct_rows_used)) PCT(pct_used)) AS pct_used,
					(SELECT 1.-MAX(PCT.pct_used) FROM (VALUES (calc.pct_ident_used),(calc.pct_rows_used)) PCT(pct_used)) AS pct_free
			) AS calc2
OUTER APPLY(SELECT MIN(Est.estimated_days) AS estimated_days
			FROM (VALUES(calc2.ident_estimated_days),(calc2.row_estimated_days)) AS Est(estimated_days)
			) AS calc3
OUTER APPLY(SELECT TOP(1)	T.WarningThreshold, 
							T.CriticalThreshold
			FROM dbo.CollectionDatesThresholds T
			WHERE T.InstanceID = D.InstanceID OR T.InstanceID=-1
			AND T.Reference = 'IdentityColumns'
			ORDER BY T.InstanceID DESC) cdt
OUTER APPLY(SELECT TOP(1)	ICT.PctUsedWarningThreshold,
							ICT.PctUsedCriticalThreshold
		FROM dbo.IdentityColumnThresholds ICT
		WHERE (ICT.InstanceID = IC.InstanceID OR ICT.InstanceID=-1)
		AND (ICT.DatabaseID = IC.DatabaseID OR ICT.DatabaseID =-1)
		AND (ICT.object_name = IC.object_name OR ICT.object_name = '')
		ORDER BY ICT.InstanceID DESC,ICT.DatabaseID DESC, ICT.object_name DESC
		)  AS THRES
WHERE I.IsActive=1
AND D.IsActive=1