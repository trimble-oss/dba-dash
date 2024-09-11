CREATE PROC dbo.ObjectExecutionStats_Upd(
    @ObjectExecutionStats dbo.ObjectExecutionStats READONLY,
    @InstanceID INT,
    @SnapshotDate DATETIME2(3)
)
AS
DECLARE @Ref VARCHAR(30)='ObjectExecutionStats'
SET XACT_ABORT ON

DECLARE @Inserted TABLE(
    ObjectID BIGINT NOT NULL,
    SnapshotDate DATETIME2(3) NOT NULL,
    PeriodTime BIGINT NOT NULL,
    total_worker_time BIGINT NOT NULL,
    total_elapsed_time BIGINT NOT NULL,
    total_logical_reads BIGINT NOT NULL,
    total_logical_writes BIGINT NOT NULL,
    total_physical_reads BIGINT NOT NULL,
    execution_count BIGINT NOT NULL,
    IsCompile BIT NOT NULL
);
DECLARE @60 TABLE(
    ObjectID BIGINT NOT NULL,
    SnapshotDate DATETIME2(3) NOT NULL,
    PeriodTime BIGINT NOT NULL,
    total_worker_time BIGINT NOT NULL,
    total_elapsed_time BIGINT NOT NULL,
    total_logical_reads BIGINT NOT NULL,
    total_logical_writes BIGINT NOT NULL,
    total_physical_reads BIGINT NOT NULL,
    execution_count BIGINT NOT NULL,
    IsCompile BIT NOT NULL,
	MaxExecutionsPerMin DECIMAL(19,6) NOT NULL
);

/* Add objects if they don't exist */
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
JOIN dbo.Databases d ON t.database_name = d.name AND D.InstanceID=@InstanceID
WHERE D.IsActive=1
AND NOT EXISTS(SELECT 1 
			FROM dbo.DBObjects O 
			WHERE O.DatabaseID = d.DatabaseID 
			AND O.ObjectName = t.object_name 
			AND O.SchemaName = t.schema_name
			AND O.ObjectType = t.type
			)
GROUP BY d.DatabaseID,
	t.object_name,
	t.type,
	t.schema_name;

/* Mark objects active that are deleted (e.g. dropped/re-created objects) */
UPDATE O 
	SET O.IsActive=1,
	O.object_id = t.object_id
FROM @ObjectExecutionStats t
JOIN dbo.Databases d ON t.database_name = d.name AND D.InstanceID=@InstanceID
JOIN dbo.DBObjects O ON O.DatabaseID = d.DatabaseID 
					AND O.ObjectName = t.object_name 
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
                                                    AND a.database_name = b.database_name
                                                    AND a.cached_time = b.cached_time
                                                    AND b.InstanceID = @InstanceID
                                                    AND a.current_time_utc > b.current_time_utc		
                                                    -- Ensure we don't have negative values
                                                    AND a.total_elapsed_time>= b.total_elapsed_time
                                                    AND a.total_worker_time>= b.total_worker_time
                                                    AND a.total_logical_reads>= b.total_logical_reads
                                                    AND a.total_logical_writes>= b.total_logical_writes
                                                    AND a.total_physical_reads>= b.total_physical_reads
                                                    AND a.execution_count>= b.execution_count
	JOIN dbo.Databases d ON a.database_name = d.name AND D.InstanceID=@InstanceID
	JOIN dbo.DBObjects O ON a.object_name = O.ObjectName AND a.schema_name = O.SchemaName AND O.DatabaseID = d.DatabaseID AND O.ObjectType = a.type
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
/* Get the data inserted so we can use this later to update 60MIN aggregation table */
OUTPUT Inserted.ObjectID,
       Inserted.SnapshotDate,
       Inserted.PeriodTime,
       Inserted.total_worker_time,
       Inserted.total_elapsed_time,
       Inserted.total_logical_reads,
       Inserted.total_logical_writes,
       Inserted.total_physical_reads,
       Inserted.execution_count,
       Inserted.IsCompile INTO @Inserted
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
AND NOT EXISTS(SELECT 1 FROM dbo.ObjectExecutionStats S WHERE S.ObjectID = T.ObjectID AND S.InstanceID=@InstanceID AND S.SnapshotDate = T.current_time_utc);

/* Get 60MIN aggregate for data inserted */
INSERT INTO @60
(
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
SELECT I.ObjectID,
    DG.DateGroup,
    SUM(I.PeriodTime),
    SUM(I.total_worker_time),
    SUM(I.total_elapsed_time),
    SUM(I.total_logical_reads),
    SUM(I.total_logical_writes),
    SUM(I.total_physical_reads),
    SUM(I.execution_count),
    CAST(MAX(CAST(I.IsCompile AS INT)) AS BIT) ,
	MAX(I.execution_count/(nullif(I.PeriodTime,0)/60000000.0))
FROM @Inserted I
CROSS APPLY dbo.DateGroupingMins(I.SnapshotDate,60) DG
GROUP BY DG.DateGroup,I.ObjectID

/* Update aggregate table */
UPDATE OES60 
	SET OES60.PeriodTime += T.PeriodTime,
    OES60.total_worker_time += T.total_worker_time,
    OES60.total_elapsed_time += T.total_elapsed_time,
    OES60.total_logical_reads += T.total_logical_reads,
    OES60.total_logical_writes += T.total_logical_writes,
    OES60.total_physical_reads += T.total_physical_reads,
    OES60.execution_count += T.execution_count,
    OES60.IsCompile= OES60.IsCompile | T.IsCompile,
	OES60.MaxExecutionsPerMin = (SELECT MAX(V) FROM (VALUES(OES60.MaxExecutionsPerMin),(t.MaxExecutionsPerMin)) T(V))
FROM dbo.ObjectExecutionStats_60MIN OES60
JOIN @60 T ON T.ObjectID = OES60.ObjectID AND T.SnapshotDate = OES60.SnapshotDate
WHERE OES60.InstanceID=@InstanceID

/* Insert into 60MIN aggregate table if doesn't exist */
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
SELECT @InstanceID InstanceID,
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
FROM @60 T 
WHERE NOT EXISTS(SELECT 1 
				FROM dbo.ObjectExecutionStats_60MIN OES60 
				WHERE T.ObjectID = OES60.ObjectID 
				AND OES60.InstanceID = @InstanceID 
				AND OES60.SnapshotDate = T.SnapshotDate
				)

-- Update staging table.
DELETE Staging.ObjectExecutionStats WHERE InstanceID=@InstanceID;

WITH T AS (
   -- handle infrequent dupes on object_id,cached_time,database_name
   SELECT @InstanceID AS InstanceID,
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
          ROW_NUMBER() OVER (PARTITION BY object_id,
                                          cached_time,
                                          database_name
                             ORDER BY total_elapsed_time DESC
                            ) rnum
   FROM @ObjectExecutionStats
)
INSERT INTO Staging.ObjectExecutionStats(
    InstanceID,
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
    current_time_utc
)
SELECT T.InstanceID,
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
       current_time_utc
FROM T
WHERE T.rnum = 1;

EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										@Reference = @Ref,
										@SnapshotDate = @SnapshotDate;

COMMIT;