CREATE PROC DBADash.AI_IdentityColumns_Get(
    @MaxRows INT = 200,
    @InstanceFilter NVARCHAR(256) = NULL,
    @HoursBack INT = NULL
)
AS
SELECT TOP (@MaxRows)
    ic.InstanceDisplayName,
    ic.DB AS DatabaseName,
    ic.schema_name + '.' + ic.object_name AS TableName,
    ic.column_name AS ColumnName,
    ic.type AS DataType,
    ic.last_value AS LastValue,
    ic.max_ident AS MaxValue,
    ic.row_count AS [RowCount],
    CAST(ic.pct_used * 100.0 AS DECIMAL(8,2)) AS PctUsed,
    ic.remaining_ident_count AS RemainingValues,
    CAST(ic.avg_ident_per_day AS BIGINT) AS AvgIdentPerDay,
    CAST(ic.estimated_days AS BIGINT) AS EstimatedDaysRemaining,
    ic.estimated_date AS EstimatedExhaustionDate,
    ic.IdentityStatus
FROM dbo.IdentityColumnsInfo ic
WHERE (ic.IdentityStatus IN (1, 2) OR ic.pct_used > 0.5)
  AND ic.ShowInSummary = 1
  AND (@InstanceFilter IS NULL OR ic.InstanceDisplayName LIKE @InstanceFilter + '%')
ORDER BY ic.IdentityStatus ASC, ic.pct_used DESC
