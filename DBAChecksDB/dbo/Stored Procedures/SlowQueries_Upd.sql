

CREATE PROC [dbo].[SlowQueries_Upd](@SlowQueries dbo.SlowQueries READONLY,@InstanceID INT,@SnapshotDate DATETIME2(3))
AS
DECLARE @UTCOffset INT
SELECT @UTCOffset = UTCOffset 
FROM dbo.Instances 
WHERE InstanceID=@InstanceID

DECLARE @MinDate DATETIME2(3)
SELECT @MinDate =MIN(timestamp) 
FROM @SlowQueries

DECLARE @Ref VARCHAR(30)='SlowQueries'
DECLARE @MaxDate DATETIME2(3)
SELECT @MaxDate = ISNULL(MAX(timestamp),'19000101')
FROM dbo.SlowQueries 
WHERE InstanceID = @InstanceID
AND timestamp>=@MinDate

INSERT INTO dbo.SlowQueries
(
	InstanceID,
	DatabaseID,
	event_type,
	object_name,
	timestamp,
	duration,
	cpu_time,
	logical_reads,
	physical_reads,
	writes,
	username,
	text,
	client_hostname,
	client_app_name,
	result,
	Uniqueifier
)
SELECT @InstanceID,
		D.DatabaseID,
		event_type,
		object_name,
		DATEADD(mi,@UTCOffset,timestamp) AS timestamp,
		duration,
		cpu_time,
		logical_reads,
		physical_reads,
		writes,
		username,
		ISNULL(batch_text,statement),
		client_hostname,
		client_app_name,
		result,
		ROW_NUMBER() OVER(PARTITION BY timestamp ORDER BY timestamp) -- just to ensure uniqueness in key
FROM @SlowQueries SQ
LEFT JOIN dbo.Databases D ON D.database_id = SQ.database_id AND D.InstanceID = @InstanceID AND D.IsActive=1
WHERE timestamp>@MaxDate


EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										@Reference = @Ref,
										@SnapshotDate = @SnapshotDate