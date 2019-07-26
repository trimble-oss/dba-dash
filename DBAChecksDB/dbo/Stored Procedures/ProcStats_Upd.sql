
CREATE PROC [dbo].[ProcStats_Upd](@ProcStats dbo.ProcStats READONLY,@InstanceID INT,@SnapshotDate DATETIME)
AS
INSERT INTO dbo.Procs
(
    DatabaseID,
    object_name,
	object_id
)
SELECT DISTINCT d.DatabaseID,t.object_name,t.object_id
FROM @ProcStats t
JOIN dbo.Databases d ON t.database_id = d.database_id AND D.InstanceID=@InstanceID
WHERE D.IsActive=1
AND NOT EXISTS(SELECT 1 FROM dbo.Procs p WHERE p.DatabaseID = d.DatabaseID AND p.object_id = t.object_id AND p.object_name = t.object_name);

WITH t AS (
	SELECT P.ProcID,
		   a.current_time_utc,
		   DATEDIFF(mcs, MAX(MAX(b.current_time_utc)) OVER (), a.current_time_utc) diff,
		   SUM(a.total_worker_time - ISNULL(b.total_worker_time, 0)) total_worker_time,
		   SUM(a.total_elapsed_time - ISNULL(b.total_elapsed_time, 0)) total_elapsed_time,
		   SUM(a.total_logical_reads - ISNULL(b.total_logical_reads, 0)) total_logical_reads,
		   SUM(a.total_logical_writes - ISNULL(b.total_logical_writes, 0)) total_logical_writes,
		   SUM(a.total_physical_reads - ISNULL(b.total_physical_reads, 0)) total_physical_reads,
		   SUM(a.execution_count - ISNULL(b.execution_count, 0)) execution_count,
		   MAX(CASE
			   WHEN b.object_id IS NULL THEN
				   1
			   ELSE
				   0
		   END) AS IsCompile
	FROM @ProcStats a
		LEFT JOIN Staging.ProcStats b ON  a.object_id = b.object_id
							 AND a.database_id = b.database_id
							 AND a.cached_time = b.cached_time
							 AND b.InstanceID = @InstanceID
							 AND a.current_time_utc > b.current_time_utc
	JOIN dbo.Databases d ON a.database_id = d.database_id AND D.InstanceID=@InstanceID
	JOIN dbo.Procs p ON a.OBJECT_NAME = p.OBJECT_NAME AND p.object_id = a.object_id AND p.DatabaseID = d.DatabaseID
	WHERE D.IsActive=1
	GROUP BY p.ProcID,a.current_time_utc
)
INSERT INTO dbo.ProcStats
(
    ProcID,
    SnapshotDate,
    PeriodTime,
    total_worker_time,
    total_elapsed_time,
    total_logical_reads,
    total_logical_writes,
    total_physical_reads,
    execution_count,
    IsCompile
)
SELECT t.ProcID,
	   t.current_time_utc,
	   t.diff,
       t.total_worker_time,
       t.total_elapsed_time,
       t.total_logical_reads,
       t.total_logical_writes,
       t.total_physical_reads,
       t.execution_count,
       t.IsCompile
FROM T
WHERE t.diff IS NOT NULL

DELETE Staging.ProcStats WHERE InstanceID=@InstanceID
INSERT INTO Staging.ProcStats(
			InstanceID
			,[object_id]
           ,[database_id]
           ,[object_name]
           ,[total_worker_time]
           ,[total_elapsed_time]
           ,[total_logical_reads]
           ,[total_logical_writes]
           ,[total_physical_reads]
           ,[cached_time]
           ,[execution_count]
           ,[current_time_utc])
SELECT @InstanceID
			,[object_id]
           ,[database_id]
           ,[object_name]
           ,[total_worker_time]
           ,[total_elapsed_time]
           ,[total_logical_reads]
           ,[total_logical_writes]
           ,[total_physical_reads]
           ,[cached_time]
           ,[execution_count]
           ,[current_time_utc]
FROM @ProcStats