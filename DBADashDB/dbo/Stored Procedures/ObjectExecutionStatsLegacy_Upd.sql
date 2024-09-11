CREATE PROC dbo.ObjectExecutionStatsLegacy_Upd(
    @ObjectExecutionStatsLegacy dbo.ProcStats READONLY,
    @InstanceID INT,
    @SnapshotDate DATETIME2(3)
)
AS
DECLARE @ObjectExecutionStats dbo.ObjectExecutionStats
INSERT INTO @ObjectExecutionStats(
       object_id,
       database_id,
       database_name,
       object_name,
       total_worker_time,
       total_elapsed_time,
       total_logical_reads,
       total_logical_writes,
       total_physical_reads,
       cached_time,
       execution_count,
       current_time_utc,
       type,
       schema_name)
SELECT  T.object_id,
       T.database_id,
       D.name,
       T.object_name,
       T.total_worker_time,
       T.total_elapsed_time,
       T.total_logical_reads,
       T.total_logical_writes,
       T.total_physical_reads,
       T.cached_time,
       T.execution_count,
       T.current_time_utc,
       T.type,
       T.schema_name	
FROM @ObjectExecutionStatsLegacy T
JOIN dbo.Databases D ON T.database_id = D.database_id AND D.InstanceID=@InstanceID
WHERE D.IsActive=1

EXEC dbo.ObjectExecutionStats_Upd @ObjectExecutionStats=@ObjectExecutionStats,
        @InstanceID = @InstanceID, 
        @SnapshotDate = @SnapshotDate