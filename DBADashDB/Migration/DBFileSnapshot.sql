/*
	Migrates dbo.DBFileSnapshot history from one instance to another within the same DBADash repository database.
	Use when a database or server has been moved/replaced and you want to preserve historical file size/space metrics.

	PARAMETERS:
		@SourceRepositoryDatabase - The DBADash repository database containing source history (defaults to current database)
		@SourceConnectionID       - The ConnectionID of the source instance in dbo.Instances (e.g., old server name)
		@TargetConnectionID       - The ConnectionID of the target instance in dbo.Instances (e.g., new server name)
		@DatabaseName             - Optional: Name of a single database to migrate. If NULL, migrates all databases.
		@PreviewOnly              - Set to 1 (default) to preview the migration without inserting data. Set to 0 to execute.

	BEHAVIOR:
		- Maps databases by name between source and target instances
		- Maps database files by database name + file_id
		- Only inserts history older than the earliest existing snapshot for each target file
		- Skips duplicate (SnapshotDate, FileID) combinations
		- Returns result sets showing:
			* Mapped file counts by database
			* Unmatched source databases
			* Unmatched source files
			* Row counts and date ranges to be migrated

	EXAMPLE 1 - Preview migration for a single database:
		EXEC Migration.DBFileSnapshot
			@SourceConnectionID = 'OldServer',
			@TargetConnectionID = 'NewServer',
			@PreviewOnly = 1;

	EXAMPLE 2 - Execute migration for all databases on an instance:
		EXEC Migration.DBFileSnapshot
			@SourceConnectionID = 'OldServer',
			@TargetConnectionID = 'NewServer',
			@PreviewOnly = 0;

	EXAMPLE 3 - Migrate from a different DBADash repository database:
		EXEC Migration.DBFileSnapshot
			@SourceRepositoryDatabase = 'DBADashArchive',
			@SourceConnectionID = 'OldServer',
			@TargetConnectionID = 'NewServer',
			@PreviewOnly = 0;
*/
CREATE PROC Migration.DBFileSnapshot(
	@SourceRepositoryDatabase SYSNAME = NULL,
	@SourceConnectionID SYSNAME,
	@TargetConnectionID SYSNAME,
	@DatabaseName SYSNAME = NULL,
	@PreviewOnly BIT = 1
)
AS
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET @SourceRepositoryDatabase = ISNULL(@SourceRepositoryDatabase,DB_NAME());

IF DB_NAME() <> @SourceRepositoryDatabase AND DB_ID(@SourceRepositoryDatabase) IS NULL
BEGIN
	RAISERROR('Invalid @SourceRepositoryDatabase', 16, 1);
	RETURN;
END


IF ISNULL(@SourceConnectionID, N'') = N'' OR ISNULL(@TargetConnectionID, N'') = N''
BEGIN
	RAISERROR('@SourceConnectionID and @TargetConnectionID are required.', 16, 1);
	RETURN;
END

IF @SourceConnectionID = @TargetConnectionID
BEGIN
	RAISERROR('Source and target connection IDs must be different.', 16, 1);
	RETURN;
END


IF NOT EXISTS(
	SELECT 1
	FROM dbo.Instances
	WHERE ConnectionID = @TargetConnectionID
	AND IsActive = 1
)
BEGIN
	RAISERROR('The target instance specified by @TargetConnectionID was not found in dbo.Instances.', 16, 1);
	RETURN;
END

CREATE TABLE #SourceFiles(
	SourceDatabaseID INT NOT NULL,
	DBName SYSNAME NOT NULL,
	file_id INT NOT NULL,
	FileName SYSNAME NOT NULL,
	SourceFileID INT NOT NULL,
	PRIMARY KEY(SourceFileID)
);

CREATE TABLE #TargetFiles(
	TargetDatabaseID INT NOT NULL,
	DBName SYSNAME NOT NULL,
	file_id INT NOT NULL,
	FileName SYSNAME NOT NULL,
	TargetFileID INT NOT NULL,
	PRIMARY KEY(TargetFileID)
);

CREATE TABLE #FileMap(
	DBName SYSNAME NOT NULL,
	file_id INT NOT NULL,
	FileName SYSNAME NOT NULL,
	SourceFileID INT NOT NULL,
	TargetFileID INT NOT NULL,
	PRIMARY KEY(SourceFileID)
);

CREATE TABLE #TargetHistory(
	TargetFileID INT NOT NULL,
	FirstTargetSnapshotDate DATETIME2(2) NULL,
	PRIMARY KEY(TargetFileID)
);

CREATE TABLE #Summary(
	DBName SYSNAME NOT NULL,
	RowsToInsert BIGINT NOT NULL,
	FirstSnapshotDate DATETIME2(2) NULL,
	LastSnapshotDate DATETIME2(2) NULL
);

DECLARE @SQL NVARCHAR(MAX);
DECLARE @RowsInserted BIGINT;

SET @SQL = N'
INSERT INTO #SourceFiles(
	SourceDatabaseID,
	DBName,
	file_id,
	FileName,
	SourceFileID
)
SELECT D.DatabaseID,
	   D.name,
	   F.file_id,
	   F.name,
	   F.FileID
FROM ' + QUOTENAME(@SourceRepositoryDatabase) + N'.dbo.Databases D
JOIN ' + QUOTENAME(@SourceRepositoryDatabase) + N'.dbo.Instances I ON I.InstanceID = D.InstanceID
JOIN ' + QUOTENAME(@SourceRepositoryDatabase) + N'.dbo.DBFiles F ON F.DatabaseID = D.DatabaseID
																AND F.InstanceID = D.InstanceID
WHERE I.ConnectionID = @SourceConnectionID
/* Note: D.IsActive, I.IsActive & F.IsActive = 1 are not checked for source since we want to migrate history for databases/files that may no longer be active */
AND D.name NOT IN(N''master'',N''model'',N''msdb'',N''tempdb'')
AND (@DatabaseName IS NULL OR D.name = @DatabaseName);';

EXEC sp_executesql @SQL,
	N'@SourceConnectionID SYSNAME,@DatabaseName SYSNAME',
	@SourceConnectionID,
	@DatabaseName;

IF NOT EXISTS(SELECT 1 FROM #SourceFiles)
BEGIN
	RAISERROR('No source DBFiles rows were found for the supplied source instance/database.', 16, 1);
	RETURN;
END

INSERT INTO #TargetFiles(
	TargetDatabaseID,
	DBName,
	file_id,
	FileName,
	TargetFileID
)
SELECT D.DatabaseID,
	   D.name,
	   F.file_id,
	   F.name,
	   F.FileID
FROM dbo.Databases D
JOIN dbo.Instances I ON I.InstanceID = D.InstanceID
JOIN dbo.DBFiles F ON F.DatabaseID = D.DatabaseID
				AND F.InstanceID = D.InstanceID
WHERE I.ConnectionID = @TargetConnectionID
AND I.IsActive = 1
AND D.IsActive = 1
AND F.IsActive = 1
AND D.name NOT IN(N'master',N'model',N'msdb',N'tempdb')
AND (@DatabaseName IS NULL OR D.name = @DatabaseName);

IF NOT EXISTS(SELECT 1 FROM #TargetFiles)
BEGIN
	RAISERROR('No target DBFiles rows were found for the supplied target instance/database.', 16, 1);
	RETURN;
END

INSERT INTO #FileMap(
	DBName,
	file_id,
	FileName,
	SourceFileID,
	TargetFileID
)
SELECT S.DBName,
	   S.file_id,
	   S.FileName,
	   S.SourceFileID,
	   T.TargetFileID
FROM #SourceFiles S
JOIN #TargetFiles T ON T.DBName = S.DBName
					 AND T.file_id = S.file_id;

IF NOT EXISTS(SELECT 1 FROM #FileMap)
BEGIN
	RAISERROR('No database/file mappings were found between source and target.', 16, 1);
	RETURN;
END

INSERT INTO #TargetHistory(
	TargetFileID,
	FirstTargetSnapshotDate
)
SELECT FM.TargetFileID,
	   MIN(TFS.SnapshotDate)
FROM #FileMap FM
LEFT JOIN dbo.DBFileSnapshot TFS ON TFS.FileID = FM.TargetFileID
GROUP BY FM.TargetFileID;

SET @SQL = N'
INSERT INTO #Summary(
	DBName,
	RowsToInsert,
	FirstSnapshotDate,
	LastSnapshotDate
)
SELECT FM.DBName,
	   COUNT_BIG(*),
	   MIN(S.SnapshotDate),
	   MAX(S.SnapshotDate)
FROM ' + QUOTENAME(@SourceRepositoryDatabase) + N'.dbo.DBFileSnapshot S
JOIN #FileMap FM ON FM.SourceFileID = S.FileID
LEFT JOIN #TargetHistory TH ON TH.TargetFileID = FM.TargetFileID
WHERE (TH.FirstTargetSnapshotDate IS NULL OR S.SnapshotDate < TH.FirstTargetSnapshotDate)
AND NOT EXISTS(
	SELECT 1
	FROM dbo.DBFileSnapshot T
	WHERE T.FileID = FM.TargetFileID
	AND T.SnapshotDate = S.SnapshotDate
)
GROUP BY FM.DBName;';

EXEC sp_executesql @SQL;

SELECT DBName,
	   COUNT(*) AS MappedFileCount
FROM #FileMap
GROUP BY DBName
ORDER BY DBName;

SELECT DISTINCT S.DBName AS UnmatchedSourceDatabase
FROM #SourceFiles S
WHERE NOT EXISTS(
	SELECT 1
	FROM #TargetFiles T
	WHERE T.DBName = S.DBName
)
ORDER BY S.DBName;

SELECT S.DBName,
	   S.file_id,
	   S.FileName
FROM #SourceFiles S
WHERE EXISTS(
	SELECT 1
	FROM #TargetFiles T
	WHERE T.DBName = S.DBName
)
AND NOT EXISTS(
	SELECT 1
	FROM #FileMap FM
	WHERE FM.SourceFileID = S.SourceFileID
)
ORDER BY S.DBName,
		 S.file_id;

SELECT DBName,
	   RowsToInsert,
	   FirstSnapshotDate,
	   LastSnapshotDate
FROM #Summary
ORDER BY DBName;

IF @PreviewOnly = 1
BEGIN
	RETURN;
END

SET @SQL = N'
INSERT INTO dbo.DBFileSnapshot(
	SnapshotDate,
	FileID,
	Size,
	space_used
)
SELECT S.SnapshotDate,
	   FM.TargetFileID,
	   S.Size,
	   S.space_used
FROM ' + QUOTENAME(@SourceRepositoryDatabase) + N'.dbo.DBFileSnapshot S
JOIN #FileMap FM ON FM.SourceFileID = S.FileID
LEFT JOIN #TargetHistory TH ON TH.TargetFileID = FM.TargetFileID
WHERE (TH.FirstTargetSnapshotDate IS NULL OR S.SnapshotDate < TH.FirstTargetSnapshotDate)
AND NOT EXISTS(
	SELECT 1
	FROM dbo.DBFileSnapshot T
	WHERE T.FileID = FM.TargetFileID
	AND T.SnapshotDate = S.SnapshotDate
);';

BEGIN TRAN;

EXEC sp_executesql @SQL;
SET @RowsInserted = @@ROWCOUNT;

COMMIT;

SELECT @RowsInserted AS RowsInserted;