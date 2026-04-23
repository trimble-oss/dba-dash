CREATE PROC DBADash.AI_OSPerformance_Get(
    @MaxRows INT = 300,
    @InstanceFilter NVARCHAR(256) = NULL,
    @HoursBack INT = 24
)
AS
/* Result set 1: CPU utilization summary (hourly averages) */
;WITH CPUHourly AS (
    SELECT c.InstanceID,
        DATEADD(HOUR, DATEDIFF(HOUR, 0, c.EventTime), 0) AS HourUtc,
        AVG(CAST(c.SQLProcessCPU AS INT)) AS AvgSqlCpu,
        AVG(100 - CAST(c.SystemIdleCPU AS INT)) AS AvgTotalCpu,
        MAX(CAST(c.SQLProcessCPU AS INT)) AS MaxSqlCpu,
        MAX(100 - CAST(c.SystemIdleCPU AS INT)) AS MaxTotalCpu
    FROM dbo.CPU c
    WHERE c.EventTime >= DATEADD(HOUR, -@HoursBack, SYSUTCDATETIME())
    GROUP BY c.InstanceID, DATEADD(HOUR, DATEDIFF(HOUR, 0, c.EventTime), 0)
)
SELECT TOP (@MaxRows)
    i.InstanceDisplayName,
    ch.HourUtc,
    ch.AvgSqlCpu,
    ch.AvgTotalCpu,
    ch.MaxSqlCpu,
    ch.MaxTotalCpu,
    ch.AvgTotalCpu - ch.AvgSqlCpu AS AvgOtherCpu
FROM CPUHourly ch
INNER JOIN dbo.Instances i ON i.InstanceID = ch.InstanceID
WHERE i.IsActive = 1
  AND (@InstanceFilter IS NULL OR i.InstanceDisplayName LIKE @InstanceFilter + '%')
ORDER BY ch.HourUtc DESC, ch.AvgTotalCpu DESC

/* Result set 2: Key performance counters (latest values) */
;WITH LatestCounter AS (
    SELECT pc.InstanceID, pc.CounterID, pc.Value, pc.SnapshotDate,
        ROW_NUMBER() OVER (PARTITION BY pc.InstanceID, pc.CounterID ORDER BY pc.SnapshotDate DESC) AS rn
    FROM dbo.PerformanceCounters pc
    WHERE pc.SnapshotDate >= DATEADD(HOUR, -@HoursBack, SYSUTCDATETIME())
)
SELECT TOP (@MaxRows)
    i.InstanceDisplayName,
    c.object_name AS ObjectName,
    c.counter_name AS CounterName,
    c.instance_name AS InstanceName,
    lc.Value,
    lc.SnapshotDate,
    c.CriticalFrom, c.CriticalTo,
    c.WarningFrom, c.WarningTo
FROM LatestCounter lc
INNER JOIN dbo.Counters c ON c.CounterID = lc.CounterID
INNER JOIN dbo.Instances i ON i.InstanceID = lc.InstanceID
WHERE lc.rn = 1
  AND i.IsActive = 1
  AND (@InstanceFilter IS NULL OR i.InstanceDisplayName LIKE @InstanceFilter + '%')
  AND c.counter_name IN (
    'Page life expectancy',
    'Batch Requests/sec',
    'SQL Compilations/sec',
    'SQL Re-Compilations/sec',
    'Memory Grants Pending',
    'Memory Grants Outstanding',
    'Target Server Memory (KB)',
    'Total Server Memory (KB)',
    'Lazy Writes/sec',
    'Page Reads/sec',
    'Page Writes/sec',
    'Checkpoint Pages/sec',
    'Free list stalls/sec',
    'Log Flushes/sec',
    'Transactions/sec',
    'Processes blocked',
    'User Connections'
  )
ORDER BY i.InstanceDisplayName, c.object_name, c.counter_name

/* Result set 3: Top procedures by CPU and elapsed time (from ObjectExecutionStats, recent period) */
;WITH LatestOES AS (
    SELECT oes.InstanceID, oes.ObjectID,
        SUM(oes.execution_count) AS TotalExecutions,
        SUM(oes.total_worker_time) / 1000000.0 AS TotalCpuSec,
        SUM(oes.total_elapsed_time) / 1000000.0 AS TotalElapsedSec,
        SUM(oes.total_logical_reads) AS TotalLogicalReads,
        SUM(oes.total_physical_reads) AS TotalPhysicalReads,
        SUM(oes.total_logical_writes) AS TotalLogicalWrites
    FROM dbo.ObjectExecutionStats oes
    WHERE oes.SnapshotDate >= DATEADD(HOUR, -@HoursBack, SYSUTCDATETIME())
    GROUP BY oes.InstanceID, oes.ObjectID
)
SELECT TOP (@MaxRows)
    i.InstanceDisplayName,
    d.name AS DatabaseName,
    o.SchemaName + '.' + o.ObjectName AS ProcedureName,
    ot.TypeDescription AS ObjectType,
    l.TotalExecutions,
    CAST(l.TotalCpuSec AS DECIMAL(18,2)) AS TotalCpuSec,
    CAST(l.TotalElapsedSec AS DECIMAL(18,2)) AS TotalElapsedSec,
    l.TotalLogicalReads,
    l.TotalPhysicalReads,
    l.TotalLogicalWrites,
    CASE WHEN l.TotalExecutions > 0 THEN CAST(l.TotalCpuSec / l.TotalExecutions AS DECIMAL(18,4)) ELSE NULL END AS AvgCpuSecPerExec,
    CASE WHEN l.TotalExecutions > 0 THEN CAST(l.TotalElapsedSec / l.TotalExecutions AS DECIMAL(18,4)) ELSE NULL END AS AvgElapsedSecPerExec,
    CASE WHEN l.TotalExecutions > 0 THEN l.TotalLogicalReads / l.TotalExecutions ELSE NULL END AS AvgReadsPerExec
FROM LatestOES l
INNER JOIN dbo.Instances i ON i.InstanceID = l.InstanceID
INNER JOIN dbo.DBObjects o ON o.ObjectID = l.ObjectID
INNER JOIN dbo.Databases d ON d.DatabaseID = o.DatabaseID
LEFT JOIN dbo.ObjectType ot ON ot.ObjectType = o.ObjectType
WHERE i.IsActive = 1
  AND o.IsActive = 1
  AND (@InstanceFilter IS NULL OR i.InstanceDisplayName LIKE @InstanceFilter + '%')
ORDER BY l.TotalCpuSec DESC
