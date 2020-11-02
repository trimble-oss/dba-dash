CREATE PROC [dbo].[ObjectExecutionStats_Upd](@ObjectExecutionStats dbo.ProcStats READONLY,@InstanceID INT,@SnapshotDate DATETIME2(3))
AS
DECLARE @Ref VARCHAR(30)='ObjectExecutionStats'
SET XACT_ABORT ON
INSERT INTO dbo.DBObjects
(
    DatabaseID,
	ObjectName,
    object_id,
	ObjectType,
    SchemaName,
    IsActive
)
SELECT d.DatabaseID,
	t.object_name,
	MAX(t.object_id),
	t.type,
	t.schema_name,
	CAST(1 AS BIT)
FROM @ObjectExecutionStats t
JOIN dbo.Databases d ON t.database_id = d.database_id AND D.InstanceID=@InstanceID
WHERE D.IsActive=1
AND NOT EXISTS(SELECT 1 
			FROM dbo.DBObjects O 
			WHERE O.DatabaseID = d.DatabaseID 
			AND O.objectname = t.object_name 
			AND O.SchemaName = t.schema_name
			AND O.ObjectType = t.type
			)
GROUP BY d.DatabaseID,
	t.object_name,
	t.type,
	t.schema_name;

UPDATE O 
	SET O.IsActive=1,
	O.object_id = t.object_id
FROM @ObjectExecutionStats t
JOIN dbo.Databases d ON t.database_id = d.database_id AND D.InstanceID=@InstanceID
JOIN dbo.DBObjects O ON O.DatabaseID = d.DatabaseID 
					AND O.objectname = t.object_name 
					AND O.ObjectType = t.type
					AND O.SchemaName = t.schema_name
WHERE D.IsActive=1
AND (O.IsActive=0 OR o.object_id <> t.object_id);

BEGIN TRAN;

WITH t AS (
	SELECT O.ObjectID,
		   a.current_time_utc,
		   DATEDIFF_BIG(mcs, MAX(MAX(b.current_time_utc)) OVER (), a.current_time_utc) diff,
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
	FROM @ObjectExecutionStats a
		LEFT JOIN Staging.ObjectExecutionStats b ON  a.object_id = b.object_id
							 AND a.database_id = b.database_id
							 AND a.cached_time = b.cached_time
							 AND b.InstanceID = @InstanceID
							 AND a.current_time_utc > b.current_time_utc
							 AND a.total_elapsed_time>= b.total_elapsed_time
	JOIN dbo.Databases d ON a.database_id = d.database_id AND D.InstanceID=@InstanceID
	JOIN dbo.DBObjects O ON a.OBJECT_NAME = O.OBJECTNAME AND O.object_id = a.object_id AND O.DatabaseID = d.DatabaseID AND O.ObjectType = a.type
	WHERE D.IsActive=1
	AND (a.cached_time> DATEADD(s,-70,a.current_time_utc) OR b.object_id IS NOT NULL) -- recently cached or we can calculate diff from staging table
	GROUP BY O.ObjectID,a.current_time_utc
)
INSERT INTO dbo.ObjectExecutionStats
(
	InstanceID,
    ObjectID,
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
SELECT @InstanceID,
		t.ObjectID,
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
AND t.execution_count>0
AND NOT EXISTS(SELECT 1 FROM dbo.ObjectExecutionStats S WHERE S.ObjectID = T.ObjectID AND S.InstanceID=@InstanceID AND S.SnapshotDate = T.current_time_utc)

DELETE Staging.ObjectExecutionStats WHERE InstanceID=@InstanceID;

WITH T AS (
	-- handle infrequent dupes on object_id,cached_time,database_id
	SELECT @InstanceID as InstanceID
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
			   ,[current_time_utc],
			   ROW_NUMBER() OVER(PARTITION BY object_id,cached_time,database_id ORDER BY total_elapsed_time DESC) rnum
	FROM @ObjectExecutionStats
)
INSERT INTO Staging.ObjectExecutionStats(
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
SELECT  InstanceID
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
FROM T
WHERE rnum=1

DECLARE @From60 DATETIME2(3) 
DECLARE @To60 DATETIME2(3)
SELECT @From60 = MIN(CONVERT(DATETIME2(3),SUBSTRING(CONVERT(VARCHAR,t.current_time_utc,120),0,14) + ':00',120)),
@To60 = DATEADD(hh,1,MAX(t.current_time_utc))
FROM @ObjectExecutionStats t

BEGIN TRAN
DELETE dbo.ObjectExecutionStats_60MIN WHERE InstanceID=@InstanceID AND SnapshotDate>=@From60
AND SnapshotDate< @To60

INSERT INTO dbo.ObjectExecutionStats_60MIN
(
	InstanceID,
    ObjectID,
    SnapshotDate,
    PeriodTime,
    total_worker_time,
    total_elapsed_time,
    total_logical_reads,
    total_logical_writes,
    total_physical_reads,
    execution_count,
    IsCompile,
	MaxExecutionsPerMin
)
SELECT S.InstanceID,
	S.ObjectID,
	CONVERT(DATETIME2(3),SUBSTRING(CONVERT(VARCHAR,S.SnapshotDate,120),0,14) + ':00',120) AS SnapshotDate,
	MAX(SUM(S.PeriodTime)) OVER(PARTITION BY S.InstanceID,CONVERT(DATETIME2(3),SUBSTRING(CONVERT(VARCHAR,S.SnapshotDate,120),0,14) + ':00',120)) PeriodTime,
	SUM(S.total_worker_time) total_worker_time,
	SUM(S.total_elapsed_time) total_elapsed_time,
	SUM(S.total_logical_reads) total_logical_reads,
	SUM(S.total_logical_writes) total_logical_writes,
	SUM(S.total_physical_reads) total_physical_reads,
	SUM(S.execution_count) execution_count,
	CAST(MAX(CAST(S.IsCompile AS INT)) AS BIT) IsCompile,
	MAX(MaxExecutionsPerMin) AS MaxExecutionsPerMin
FROM dbo.ObjectExecutionStats S
WHERE S.InstanceID = @InstanceID 
AND S.SnapshotDate >=@From60
AND S.SnapshotDate< @To60
GROUP BY S.ObjectID,S.InstanceID,
	CONVERT(DATETIME2(3),SUBSTRING(CONVERT(VARCHAR,S.SnapshotDate,120),0,14) + ':00',120) 
 OPTION(OPTIMIZE FOR(@From60='9999-12-31'))
COMMIT

EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										@Reference = @Ref,
										@SnapshotDate = @SnapshotDate

COMMIT