CREATE PROC dbo.SlowQueries_Upd(
		@SlowQueries dbo.SlowQueries READONLY,
		@InstanceID INT,
		@SnapshotDate DATETIME2(3)
)
AS
DECLARE @AzureDatabaseID INT 
DECLARE @MinDate DATETIME2(3)
DECLARE @Ref VARCHAR(30)='SlowQueries'
DECLARE @MaxDate DATETIME2(3)

SELECT @MinDate =MIN(timestamp) 
FROM @SlowQueries

SELECT @MaxDate = ISNULL(MAX(timestamp),'19000101')
FROM dbo.SlowQueries 
WHERE InstanceID = @InstanceID
AND timestamp>=@MinDate

/* For AzureDB there is a 1:1 mapping between dbo.Instances and dbo.Databases.  Get the associated DatabaseID */
SELECT @AzureDatabaseID = D.DatabaseID
FROM dbo.Instances I
JOIN dbo.Databases D ON I.InstanceID = D.InstanceID
WHERE I.EngineEdition=5
AND I.InstanceID = @InstanceID
AND I.IsActive=1
AND D.IsActive=1

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
	Uniqueifier,
	session_id,
	context_info
)
SELECT @InstanceID,
		ISNULL(D.DatabaseID,@AzureDatabaseID), /* For AzureDB, use @AzureDatabaseID if the database_id from the extended event doesn't match for some reason. (Issue #481) */
		event_type,
		object_name,
		timestamp,
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
		ROW_NUMBER() OVER(PARTITION BY timestamp ORDER BY timestamp), -- just to ensure uniqueness in key
		session_id,
		context_info
FROM @SlowQueries SQ
LEFT JOIN dbo.Databases D ON D.database_id = SQ.database_id AND D.InstanceID = @InstanceID AND D.IsActive=1
WHERE timestamp > @MaxDate

EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										@Reference = @Ref,
										@SnapshotDate = @SnapshotDate