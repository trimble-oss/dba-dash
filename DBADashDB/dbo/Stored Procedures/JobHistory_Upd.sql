CREATE PROC dbo.JobHistory_Upd(
	@InstanceID INT,
	@JobHistory dbo.JobHistory READONLY,
	@SnapshotDate DATETIME2(2)
)
AS
SET XACT_ABORT ON
DECLARE @Ref VARCHAR(30)='JobHistory'
DECLARE @UTCOffset INT

SELECT @UTCOffset = UTCOffset
FROM dbo.Instances 
WHERE InstanceID = @InstanceID

DECLARE @InsertedJobHistory TABLE(
	InstanceID INT NOT NULL,
	job_id UNIQUEIDENTIFIER NOT NULL,
	step_id INT NOT NULL,
	RunDateTime DATETIME2(2) NOT NULL,
	run_status INT NOT NULL,
	RunDurationSec INT NOT NULL,
	retries_attempted INT NOT NULL
)

INSERT INTO dbo.Jobs(InstanceID,job_id,name,description,enabled,IsActive)
SELECT @InstanceID,job_id,'!!NEW!! ' + CAST(job_id as NVARCHAR(128)),'{Job metadata hasn''t been populated for this job yet}',CAST(1 AS BIT),CAST(1 as BIT)
FROM @JobHistory T 
WHERE NOT EXISTS(SELECT 1 FROM dbo.Jobs J WHERE J.job_id = T.job_id AND J.InstanceID = @InstanceID)
GROUP BY job_id

/* 		
	 	Performance optimization:
	 	The goal is for the query optimizer to seek on InstanceID within each partition, then perform a backward ordered scan to get the MAX instance_id for each partition very efficiently.
	 	We then return the MAX across partitions.  Older partitions could probably be skipped, but the cost of including them is small.
*/
DECLARE @max_instance_id INT=-1
SELECT TOP(1) @max_instance_id = ISNULL(MAX(m.instance_id),-1)
FROM sys.partitions p
OUTER APPLY(SELECT TOP(1) instance_id
			FROM dbo.JobHistory
			WHERE InstanceID = @InstanceID
			AND $PARTITION.PF_JobHistory(RunDateTime) = p.partition_number
			ORDER BY instance_id DESC
			) m
WHERE p.object_id = OBJECT_ID('dbo.JobHistory')
AND p.index_id=1
AND p.rows > 0
AND m.instance_id IS NOT NULL

BEGIN TRAN

INSERT INTO dbo.JobHistory(
		InstanceID,
		instance_id,
		job_id,
		step_id,
		step_name,
		sql_message_id,
		sql_severity,
		message,
		run_status,
		RunDateTime,
		RunDurationSec,
		operator_id_emailed,
		operator_id_netsent,
		operator_id_paged,
		retries_attempted,
		server)
OUTPUT INSERTED.InstanceID,INSERTED.job_id,INSERTED.step_id,INSERTED.RunDateTime,INSERTED.run_status,INSERTED.RunDurationSec,INSERTED.retries_attempted INTO @InsertedJobHistory
SELECT @InstanceID,
		instance_id,
		job_id,
		step_id,
		step_name,
		sql_message_id,
		sql_severity,
		message,
		run_status,
		DATEADD(mi,@UTCOffset,dt.RunDateTime),
		dt.RunDurationSec,
		operator_id_emailed,
		operator_id_netsent,
		operator_id_paged,
		retries_attempted,
		server
FROM @JobHistory jh
OUTER APPLY
    (
        SELECT	DATEADD(s,run_time%100,DATEADD(mi,run_time/100%100,DATEADD(hh,run_time/10000,DATEADD(d,run_date%100-1,DATEADD(mm,run_date/100%100-1,DATEADD(yy,NULLIF(run_date,0)/10000-1900,0)))))) as RunDateTime,
               ((jh.run_duration / 1000000) * 86400)
               + (((jh.run_duration - ((jh.run_duration / 1000000) * 1000000)) / 10000) * 3600)
               + (((jh.run_duration - ((jh.run_duration / 10000) * 10000)) / 100) * 60)
               + (jh.run_duration - (jh.run_duration / 100) * 100) AS RunDurationSec
    ) dt
WHERE jh.instance_id > @max_instance_id;

WITH L AS (
	SELECT job_id,
			MAX(CASE WHEN run_status IN(0,3) AND step_id=0 THEN RunDateTime ELSE NULL END) as LastFailed,
			MAX(CASE WHEN run_status=1 AND step_id=0 THEN RunDateTime ELSE NULL END) as LastSucceeded,
			MAX(CASE WHEN run_status IN(0,3) AND step_id<>0 THEN RunDateTime ELSE NULL END) as StepLastFailed
	FROM @InsertedJobHistory
	GROUP BY job_id
)
UPDATE J 
SET J.LastFailed = ISNULL(L.LastFailed,J.LastFailed),
	J.LastSucceeded = ISNULL(L.LastSucceeded,J.LastSucceeded),
	J.StepLastFailed = ISNULL(L.StepLastFailed,J.StepLastFailed)
FROM dbo.Jobs J
JOIN L ON J.job_id = L.job_id
WHERE J.InstanceID = @InstanceID


SELECT InstanceID,
	job_id,
	step_id,
	DG.DateGroup as RunDateTime,
	SUM(CASE WHEN run_status IN(0,3) THEN 1 ELSE 0 END) as FailedCount,
	SUM(CASE WHEN run_status=1 THEN 1 ELSE 0 END) as SucceededCount,
	ISNULL(SUM(retries_attempted),0) as RetryCount,
	ISNULL(SUM(RunDurationSec),0) as RunDurationSec,
	ISNULL(MAX(RunDurationSec),0) as MaxRunDurationSec,
	ISNULL(MIN(RunDurationSec),0) as MinRunDurationSec
INTO #60
FROM @InsertedJobHistory jh
CROSS APPLY dbo.DateGroupingMins(jh.RunDateTime,60) DG
GROUP BY InstanceID,
		job_id,
		step_id,
		DG.DateGroup

DECLARE @MinRunDateTime DATETIME2(2) = (SELECT MIN(RunDateTime) FROM #60)

UPDATE J
SET J.FailedCount += T.FailedCount,
	J.SucceededCount += T.SucceededCount,
	J.RetryCount += T.RetryCount,
	J.RunDurationSec += T.RunDurationSec,
	J.MaxRunDurationSec = (SELECT MAX(c) FROM (VALUES (T.MaxRunDurationSec),(J.MaxRunDurationSec)) T(c)),
	J.MinRunDurationSec = (SELECT MIN(c) FROM (VALUES (T.MinRunDurationSec),(J.MinRunDurationSec)) T(c))
FROM dbo.JobStats_60MIN J
JOIN #60 T ON J.job_id = T.job_id AND J.step_id = T.step_id AND J.RunDateTime = T.RunDateTime AND J.InstanceID = T.InstanceID
WHERE J.InstanceID = @InstanceID
AND J.RunDateTime >= @MinRunDateTime

INSERT INTO dbo.JobStats_60MIN(
		InstanceID,
		job_id,
		step_id,
		RunDateTime,
		FailedCount,
		SucceededCount,
		RetryCount,
		RunDurationSec,
		MaxRunDurationSec,
		MinRunDurationSec
)
SELECT 	InstanceID,
		job_id,
		step_id,
		RunDateTime,
		FailedCount,
		SucceededCount,
		RetryCount,
		RunDurationSec,
		MaxRunDurationSec,
		MinRunDurationSec
FROM #60 T 
WHERE NOT EXISTS(SELECT 1 FROM dbo.JobStats_60MIN J WHERE J.job_id = T.job_id AND J.step_id = T.step_id AND J.InstanceID = @InstanceID AND J.RunDateTime = T.RunDateTime)

EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
									 @Reference = @Ref,
									 @SnapshotDate = @SnapshotDate

COMMIT
