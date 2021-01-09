CREATE   PROC [dbo].[PerformanceCounters_Upd](@InstanceID INT,@SnapshotDate DATETIME2(7),@PerformanceCounters dbo.PerformanceCounters READONLY)
AS
DECLARE @Ref NVARCHAR(128)='PerformanceCounters'

INSERT INTO dbo.Counters
(
    object_name,
    counter_name,
    instance_name
)
SELECT RTRIM(object_name),RTRIM(counter_name),RTRIM(instance_name) 
FROM @PerformanceCounters
EXCEPT 
SELECT object_name,counter_name,instance_name 
FROM dbo.Counters WITH(UPDLOCK);

INSERT INTO dbo.InstanceCounters(InstanceID,CounterID)
SELECT @InstanceID,C.CounterID
FROM @PerformanceCounters pc 
JOIN dbo.Counters C ON C.counter_name = pc.counter_name AND C.instance_name = pc.instance_name AND C.object_name = pc.object_name
EXCEPT 
SELECT InstanceID,CounterID 
FROM dbo.InstanceCounters
WHERE InstanceID =@InstanceID;

WITH PC AS (
	SELECT @InstanceID AS InstanceID,
		C.CounterID,
		B.SnapshotDate,
		CASE WHEN B.cntr_type = 65792 AND B.object_name='Batch Resp Statistics' AND B.cntr_value >= A.cntr_value THEN B.cntr_value - A.cntr_value
			WHEN B.cntr_type = 65792 AND B.object_name<>'Batch Resp Statistics' THEN B.cntr_value 
			WHEN B.cntr_type = 272696576 AND B.cntr_value>=A.cntr_value THEN (B.cntr_value-A.cntr_value) / (DATEDIFF(ms,A.SnapshotDate,B.SnapshotDate)/1000.0)
			WHEN B.cntr_type=537003264 THEN B.cntr_value*1.0 / NULLIF(Bbase.cntr_value,0)
			WHEN B.cntr_type=1073874176 AND Bbase.cntr_value>=Abase.cntr_value THEN (B.cntr_value-A.cntr_value*1.0) / NULLIF(Bbase.cntr_value-Abase.cntr_value,0)
		ELSE NULL END AS Value
	FROM @PerformanceCounters B
	JOIN dbo.Counters C ON C.counter_name = B.counter_name AND C.object_name = B.object_name AND C.instance_name = B.instance_name
	LEFT JOIN dbo.CounterMapping M ON B.object_name = M.object_name 
									AND B.counter_name = M.counter_name 
									AND B.cntr_type IN(1073874176,272696576)
	LEFT JOIN @PerformanceCounters Bbase ON B.object_name = M.object_name 
											AND B.counter_name = M.base_counter_name 
											AND Bbase.instance_name = B.instance_name 
											AND Bbase.cntr_type=1073939712	
											AND B.cntr_type IN(1073874176,537003264)
	LEFT JOIN Staging.PerformanceCounters A ON A.object_name = b.object_name 
												AND A.counter_name = B.counter_name 
												AND A.instance_name = B.instance_name 
												AND B.cntr_type = A.cntr_type 
												AND A.InstanceID = @InstanceID
												AND A.SnapshotDate< B.SnapshotDate
	LEFT JOIN Staging.PerformanceCounters Abase ON Abase.object_name = M.object_name 
												AND Abase.counter_name = M.base_counter_name 
												AND Abase.instance_name = B.instance_name 
												AND A.cntr_type = 1073874176 
												AND Abase.InstanceID = @InstanceID 
												AND Abase.cntr_type=1073939712	
												AND ABase.SnapshotDate <B.SnapshotDate
	WHERE B.cntr_type IN(65792,272696576,537003264,1073874176)
	AND NOT EXISTS(SELECT 1 FROM dbo.PerformanceCounters PC WHERE PC.SnapshotDate = B.SnapshotDate AND PC.InstanceID = @InstanceID AND PC.CounterID=C.CounterID)
)
INSERT INTO dbo.PerformanceCounters
(
    InstanceID,
    CounterID,
    SnapshotDate,
    Value
)
SELECT PC.InstanceID,
       PC.CounterID,
	   PC.SnapshotDate,
       PC.Value	
FROM PC
WHERE Value IS NOT NULL


DELETE Staging.PerformanceCounters WHERE InstanceID = @InstanceID
INSERT INTO Staging.PerformanceCounters(
	   InstanceID,
	   SnapshotDate,
	   object_name,
       counter_name,
       instance_name,
       cntr_value,
       cntr_type
	   )
SELECT @InstanceID,
	   SnapshotDate,
	   object_name,
       counter_name,
       instance_name,
       cntr_value,
       cntr_type
FROM @PerformanceCounters
WHERE (cntr_type <> 65792 OR object_name='Batch Resp Statistics')

EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,
                             @Reference = @Ref,
                             @SnapshotDate = @SnapshotDate