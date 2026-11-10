CREATE PROC dbo.InternalPerformanceCounters_Upd(
	@InstanceID INT,
	@SnapshotDate DATETIME2(7),
	@InternalPerformanceCounters dbo.PerformanceCounters READONLY
)
AS
/*
	DBADash's own diagnostic counters (plans to collect, text from cache...), logged with LogInternalPerformanceCounters on.

	Collection Duration (ms) is an exception: newer services report collection runs instead (dbo.CollectionRuns_Upd) and that
	counter is read from them.  An older service still logs the duration here - it's imported as a run without a status, so
	the counter carries on.  The instance name is the collection, followed by the step for the steps that follow some
	built-in collections ("QueryStats Plans").
*/
SET XACT_ABORT ON
DECLARE @Ref NVARCHAR(128)='InternalPerformanceCounters'
DECLARE @Counters dbo.PerformanceCounters
DECLARE @Runs dbo.CollectionRuns

INSERT INTO @Counters(SnapshotDate,object_name,counter_name,instance_name,cntr_value,cntr_type)
SELECT SnapshotDate,object_name,counter_name,instance_name,cntr_value,cntr_type
FROM @InternalPerformanceCounters
WHERE NOT (RTRIM(object_name) = 'DBADash' AND RTRIM(counter_name) = 'Collection Duration (ms)')

INSERT INTO @Runs(Reference,Step,SnapshotDate,Status,Attempts,DurationMs,CpuMs,LogicalReads)
SELECT	LEFT(Name,CASE WHEN SpacePos = 0 THEN 100 ELSE SpacePos - 1 END),
		CASE WHEN SpacePos = 0 THEN '' ELSE LEFT(SUBSTRING(Name,SpacePos + 1,128),27) END,
		SnapshotDate,
		NULL,
		NULL,
		CAST(ROUND(cntr_value,0) AS INT),
		NULL,
		NULL
FROM (	SELECT	SnapshotDate,
				cntr_value,
				RTRIM(instance_name) AS Name,
				/* Only built-in collections have steps, and their names have no spaces.  A custom collection's name can - "UserData.Disk Check" is the collection, not a step */
				CASE WHEN RTRIM(instance_name) LIKE 'UserData.%' THEN 0 ELSE CHARINDEX(' ',RTRIM(instance_name)) END AS SpacePos
		FROM @InternalPerformanceCounters
		WHERE RTRIM(object_name) = 'DBADash'
		AND RTRIM(counter_name) = 'Collection Duration (ms)'
		AND RTRIM(instance_name) <> ''
		AND cntr_value BETWEEN 0 AND 2147483647
		) T

IF EXISTS(SELECT 1 FROM @Counters)
BEGIN
	EXEC dbo.PerformanceCounters_Upd @PerformanceCounters=@Counters,
									@InstanceID = @InstanceID,
									@SnapshotDate=@SnapshotDate,
									@Internal=1
END

IF EXISTS(SELECT 1 FROM @Runs)
BEGIN
	/* Older services only sent these with LogInternalPerformanceCounters on - the setting that keeps each run */
	EXEC dbo.CollectionRuns_Upd @InstanceID = @InstanceID,
								@SnapshotDate = @SnapshotDate,
								@CollectionRuns = @Runs,
								@StoreDetail = 1
END

EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,
								@Reference = @Ref,
								@SnapshotDate = @SnapshotDate
