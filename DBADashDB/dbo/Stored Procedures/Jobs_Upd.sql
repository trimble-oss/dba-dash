CREATE PROC dbo.Jobs_Upd(
	@InstanceID INT,
	@Jobs dbo.Jobs READONLY,
	@SnapshotDate DATETIME2(2)
)
AS
SET XACT_ABORT ON
DECLARE @Ref VARCHAR(30)='Jobs'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN

	INSERT INTO dbo.DDL	(
		DDLHash,
		DDL
	)
	SELECT DISTINCT DDLHash,
			DDL
	FROM @Jobs T
	WHERE NOT EXISTS(SELECT 1 FROM dbo.DDL WHERE DDL.DDLHash = T.DDLHash)

	BEGIN TRAN

	INSERT INTO dbo.JobDDLHistory(
		InstanceID,
		job_id,
		version_number,
		SnapshotDate,
		date_modified,
		DDLID
	)
	SELECT @InstanceID,T.job_id,T.version_number,@SnapshotDate,T.date_modified,DDL.DDLID 
	FROM @Jobs T
	JOIN dbo.DDL ON T.DDLHash = DDL.DDLHash
	WHERE NOT EXISTS(SELECT 1 FROM dbo.Jobs J WHERE J.InstanceID = @InstanceID AND J.job_id = T.job_id AND DDL.DDLID = J.DDLID)

	INSERT INTO dbo.Jobs(
		InstanceID,
		job_id,
		originating_server,
		name,
		enabled,
		description,
		start_step_id,
		category_id,
		category,
		owner,
		notify_level_eventlog,
		notify_level_email,
		notify_level_netsend,
		notify_level_page,
		notify_email_operator,
		notify_netsend_operator,
		notify_page_operator,
		delete_level,
		date_created,
		date_modified,
		version_number,
		has_schedule,
		has_server,
		has_step,
		DDLID,
		IsActive,
		SnapshotCreatedDate,
		SnapshotUpdatedDate
		)
		SELECT @InstanceID,
		job_id,
		originating_server,
		name,
		enabled,
		description,
		start_step_id,
		category_id,
		category,
		owner,
		notify_level_eventlog,
		notify_level_email,
		notify_level_netsend,
		notify_level_page,
		notify_email_operator,
		notify_netsend_operator,
		notify_page_operator,
		delete_level,
		date_created,
		date_modified,
		version_number,
		has_schedule,
		has_server,
		has_step,
		DDLID,
		1,
		@SnapshotDate,
		@SnapshotDate
	FROM @Jobs T
	JOIN dbo.DDL ON T.DDLHash = DDL.DDLHash
	WHERE NOT EXISTS(SELECT 1 FROM dbo.Jobs J WHERE T.job_id = J.job_id AND J.InstanceID = @InstanceID)

	UPDATE J 
		SET IsActive = 0,
		SnapshotUpdatedDate = @SnapshotDate
	FROM dbo.Jobs J
	WHERE NOT EXISTS(SELECT 1 FROM @Jobs T WHERE T.job_id = J.job_id)
	AND J.InstanceID = @InstanceID

	UPDATE dbo.Jobs
		SET originating_server=T.originating_server,
		name=T.name,
		enabled=T.enabled,
		description=T.description,
		start_step_id=T.start_step_id,
		category_id=T.category_id,
		category=T.category,
		owner=T.owner,
		notify_level_eventlog=T.notify_level_eventlog,
		notify_level_email=T.notify_level_email,
		notify_level_netsend=T.notify_level_netsend,
		notify_level_page=T.notify_level_page,
		notify_email_operator=T.notify_email_operator,
		notify_netsend_operator=T.notify_netsend_operator,
		notify_page_operator = T.notify_page_operator,
		delete_level=T.delete_level,
		date_created = T.date_created,
		date_modified = T.date_modified,
		version_number = T.version_number,
		has_schedule = T.has_schedule,
		has_server = T.has_server,
		has_step = T.has_step,
		DDLID = DDL.DDLID,
		Isactive=1,
		SnapshotCreatedDate = ISNULL(J.SnapshotCreatedDate,@SnapshotDate),
		SnapshotUpdatedDate = @SnapshotDate
	FROM dbo.Jobs J
	JOIN @Jobs T ON T.job_id = J.job_id
	JOIN dbo.DDL ON T.DDLHash = DDL.DDLHash
	WHERE J.InstanceID = @InstanceID
	AND NOT EXISTS(SELECT J.job_id,
							J.originating_server,
							J.name,
							J.enabled,
							J.description,
							J.start_step_id,
							J.category_id,
							J.category,
							J.owner,
							J.notify_level_eventlog,
							J.notify_level_email,
							J.notify_level_netsend,
							J.notify_level_page,
							J.notify_email_operator,
							J.notify_netsend_operator,
							J.notify_page_operator,
							J.delete_level,
							J.date_created,
							J.date_modified,
							J.version_number,
							J.has_schedule,
							J.has_server,
							J.has_step,
							J.DDLID
			INTERSECT 
					SELECT T.job_id,
							T.originating_server,
							T.name,
							T.enabled,
							T.description,
							T.start_step_id,
							T.category_id,
							T.category,
							T.owner,
							T.notify_level_eventlog,
							T.notify_level_email,
							T.notify_level_netsend,
							T.notify_level_page,
							T.notify_email_operator,
							T.notify_netsend_operator,
							T.notify_page_operator,
							T.delete_level,
							T.date_created,
							T.date_modified,
							T.version_number,
							T.has_schedule,
							T.has_server,
							T.has_step,
							DDL.DDLID
				)

	COMMIT
	
	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
	                             @Reference = @Ref,
	                             @SnapshotDate = @SnapshotDate
END