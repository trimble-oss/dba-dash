CREATE PROC dbo.IdentityColumns_Get(
	@InstanceIDs IDs READONLY,
	@DatabaseID INT=NULL,
    @IncludeCritical BIT=1,
	@IncludeWarning BIT=1,
	@IncludeNA BIT=0,
	@IncludeOK BIT=0,
    @ShowHidden BIT=1
)
AS
SELECT ICI.InstanceID,
       ICI.DatabaseID,
       ICI.InstanceDisplayName,
       ICI.DB,
       ICI.SnapshotDate,
       ICI.SnapshotStatus,
       ICI.object_id,
       ICI.schema_name,
       ICI.object_name,
       ICI.column_name,
       ICI.type,
       ICI.increment_value,
       ICI.seed_value,
       ICI.row_count,
       ICI.last_value,
       ICI.min_ident,
       ICI.max_ident,
       ICI.max_rows,
       ICI.pct_used,
       ICI.IdentityStatus,
       ICI.IdentityPctStatus,
       ICI.IdentityDaysStatus,
       ICI.pct_free,
       ICI.pct_ident_used,
       ICI.pct_rows_used,
       ICI.remaining_ident_count,
       ICI.remaining_row_count,
       ICI.avg_ident_per_day,
       ICI.avg_rows_per_day,
       ICI.avg_calc_days,
       ICI.ident_estimated_days,
       ICI.row_estimated_days,
       ICI.estimated_days,
       ICI.estimated_date,
       ICI.PctUsedWarningThreshold,
	   ICI.PctUsedCriticalThreshold,
       ICI.DaysWarningThreshold,
       ICI.DaysCriticalThreshold,
       ICI.ThresholdConfigurationLevel
FROM dbo.IdentityColumnsInfo ICI
WHERE EXISTS(
		SELECT 1 
		FROM @InstanceIDs T
		WHERE ICI.InstanceID = T.ID
		)
AND (ICI.DatabaseID = @DatabaseID OR @DatabaseID IS NULL)
AND ICI.IdentityStatus IN(  CASE WHEN @IncludeCritical=1 THEN 1 ELSE NULL END,
                            CASE WHEN @IncludeWarning=1 THEN 2 ELSE NULL END,
                            CASE WHEN @IncludeNA=1 THEN 3 ELSE NULL END,
                            CASE WHEN @IncludeOK=1 THEN 4 ELSE NULL END)
AND (ICI.ShowInSummary=1 OR @ShowHidden=1)