CREATE PROC dbo.DBFiles_Upd(
	@DBFiles DBFiles READONLY,
	@InstanceID INT,
	@SnapshotDate DATETIME2(2)
)
AS
SET XACT_ABORT ON
SET NOCOUNT ON
DECLARE @Ref VARCHAR(30)='DBFiles'
IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN
	CREATE TABLE #DBFiles(
		DatabaseID INT NOT NULL,
		file_id INT NOT NULL,
		data_space_id INT NULL,
		name sysname NOT NULL,
		filegroup_name sysname NULL,
		physical_name NVARCHAR(260) NULL,
		type TINYINT NULL,
		size BIGINT NULL,
		space_used BIGINT NULL,
		max_size BIGINT NULL,
		growth BIGINT NULL,
		is_percent_growth BIT NULL,
		is_read_only BIT NULL,
		state TINYINT NULL,
		PRIMARY KEY (DatabaseID,file_id)
	);
	INSERT INTO #DBFiles(DatabaseID,
		   file_id,
		   data_space_id,
		   name,
		   filegroup_name,
		   physical_name,
		   type,
		   size,
		   space_used,
		   max_size,
		   growth,
		   is_percent_growth,
		   is_read_only,
		   state
	)
	SELECT D.DatabaseID
		  ,F.[file_id]
		  ,F.[data_space_id]
		  ,F.[name]
		  ,F.[filegroup_name]
		  ,F.[physical_name]
		  ,F.[type]
		  ,F.[size]
		  ,F.[space_used]
		  ,F.[max_size]
		  ,F.[growth]
		  ,F.[is_percent_growth]
		  ,F.[is_read_only]
		  ,F.state
	FROM @DBFiles F 
	JOIN dbo.Databases D ON F.database_id = D.database_id
	WHERE D.IsActive=1
	AND D.InstanceID = @InstanceID
	AND file_id IS NOT NULL


	BEGIN TRAN

	INSERT INTO dbo.DBFiles(
		InstanceID,
		DatabaseID,
		file_id,
		data_space_id,
		name,
		filegroup_name,
		physical_name,
		type,
		size,
		space_used,
		max_size,
		growth,
		is_percent_growth,
		is_read_only,
		IsActive,
		state
	)
	SELECT @InstanceID AS InstanceID,
		   T.DatabaseID,
		   T.file_id,
		   T.data_space_id,
		   T.name,
		   T.filegroup_name,
		   T.physical_name,
		   T.type,
		   T.size,
		   T.space_used,
		   T.max_size,
		   T.growth,
		   T.is_percent_growth,
		   T.is_read_only,
		   CAST(1 AS BIT) AS IsActive,
		   T.state
	FROM #DBFiles T
	WHERE NOT EXISTS
	(
		SELECT 1
		FROM dbo.DBFiles F WITH (UPDLOCK)
		WHERE F.file_id = T.file_id
		AND F.DatabaseID = T.DatabaseID
		AND F.InstanceID = @InstanceID
	);

	UPDATE F
		SET F.data_space_id = T.data_space_id,
			F.name = T.name,
			F.filegroup_name = ISNULL(T.filegroup_name, F.filegroup_name),
			F.physical_name = T.physical_name,
			F.type = T.type,
			F.size = T.size,
			F.space_used = T.space_used,
			F.max_size = T.max_size,
			F.growth = T.growth,
			F.is_percent_growth = T.is_percent_growth,
			F.is_read_only = T.is_read_only,
			F.IsActive = 1,
			F.state = T.state
	FROM dbo.DBFiles F
	JOIN #DBFiles T ON F.file_id = T.file_id
						AND F.DatabaseID = T.DatabaseID
	WHERE F.InstanceID = @InstanceID;


	UPDATE F
	SET F.IsActive=0
	FROM dbo.DBFiles F
	WHERE NOT EXISTS(	SELECT 1
						FROM #DBFiles T
						WHERE T.DatabaseID = F.DatabaseID
						AND T.file_id = F.file_id
					)
	AND F.InstanceID = @InstanceID

	INSERT INTO dbo.DBFileSnapshot(SnapshotDate,FileID,Size,space_used)
	SELECT @SnapshotDate,
		  F.FileID,
		  T.size,
		  T.space_used
	FROM #DBFiles T 
	JOIN dbo.DBFiles F ON T.file_id = F.file_id AND T.DatabaseID = F.DatabaseID
	WHERE F.IsActive=1
	AND F.InstanceID = @InstanceID

	COMMIT

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										 @Reference = @Ref,
										 @SnapshotDate = @SnapshotDate

END