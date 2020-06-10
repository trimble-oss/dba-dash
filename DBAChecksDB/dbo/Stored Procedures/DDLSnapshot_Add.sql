CREATE   PROC [dbo].[DDLSnapshot_Add](
	@ss dbo.DDLSnapshot READONLY,
	@ConnectionId SYSNAME=NULL,
	@InstanceID INT=NULL,
	@DB SYSNAME,
	@SnapshotDate DATETIME2(3),
	@StartTime DATETIME2(3),
	@EndTime DATETIME2(3)
)
AS
DECLARE @UpdateCount INT=-1
DECLARE @DatabaseId INT
DECLARE @ValidatedSnapshotDate DATETIME2(3)
IF @InstanceID IS NULL
BEGIN
	SELECT @DatabaseId = D.DatabaseID
	FROM dbo.Instances I 
	JOIN dbo.Databases D ON D.InstanceID = I.InstanceID
	WHERE I.ConnectionID = @ConnectionId 
	AND D.name = @DB
	AND D.IsActive=1
	AND I.IsActive=1
END
ELSE
BEGIN
	SELECT @DatabaseId = D.DatabaseID
	FROM dbo.Instances I 
	JOIN dbo.Databases D ON D.InstanceID = I.InstanceID
	WHERE I.InstanceID=@InstanceID
	AND D.name = @DB
	AND D.IsActive=1
	AND I.IsActive=1
END

DECLARE @LastSnapshotDate DATETIME2(3) 
SELECT TOP(1) @LastSnapshotDate = s.SnapshotDate 
FROM dbo.DDLSnapshots s
WHERE s.DatabaseID = @DatabaseID
ORDER BY s.SnapshotDate DESC


IF @DatabaseId IS NOT NULL
BEGIN

	INSERT INTO dbo.DDL(DDLHash,DDL)
	SELECT ss.DDLHash,ss.DDL
	FROM @ss ss
	WHERE NOT EXISTS(
			SELECT 1 
			FROM dbo.DDL 
			WHERE ss.DDLHash = DDL.DDLHash
			);

	INSERT INTO dbo.DBObjects
	(
		DatabaseID,
		ObjectType,
		object_id,
		SchemaName,
		ObjectName,
		DDLID,
		SnapshotDateCreated,
		SnapshotDateModified,
		ObjectDateCreated,
		ObjectDateModified,
		RevisionCount,
		IsActive
	)
	OUTPUT Inserted.ObjectID,-1,INSERTED.DDLID,'19000101',@SnapshotDate,INSERTED.ObjectDateModified,INSERTED.ObjectDateModified INTO dbo.DBObjectHistory(ObjectID,DDLIDOld,DDLIDNew,ValidFrom,ValidTo,ObjectValidFrom,ObjectValidTo)
	SELECT @DatabaseID,ss.ObjectType,object_id,ss.SchemaName,ss.ObjectName,DDL.DDLID,@SnapshotDate,@SnapshotDate,ss.ObjectDateCreated,ss.ObjectDateModified,0,1 
	FROM @ss ss
	LEFT JOIN dbo.DDL ON ss.DDLHash = DDL.DDLHash
	WHERE NOT EXISTS(SELECT 1 
					FROM dbo.DBObjects O 
					WHERE O.DatabaseID = @DatabaseID 
					AND O.ObjectName = ss.ObjectName
					AND O.SchemaName =ss.SchemaName
					AND O.ObjectType = ss.ObjectType)

	SET @UpdateCount=@@ROWCOUNT
	UPDATE O 
		SET O.DDLID = ISNULL(DDL.DDLID,-1),
		O.object_id = ISNULL(ss.object_id,O.object_id),
		O.SnapshotDateModified = @SnapshotDate,
		O.RevisionCount+=1,
		O.IsActive = CASE WHEN DDL.DDLID IS NULL THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END,
		O.ObjectDateCreated = ss.ObjectDateCreated,
		O.ObjectDateModified = ss.ObjectDateModified
	OUTPUT Deleted.ObjectID,Deleted.DDLID,ISNULL(Inserted.DDLID,-1),Deleted.SnapshotDateModified,@SnapshotDate,Deleted.ObjectDateModified,Inserted.ObjectDateModified INTO dbo.DBObjectHistory(ObjectID,DDLIDOld,DDLIDNew,ValidFrom,ValidTo,ObjectValidFrom,ObjectValidTo)
	FROM dbo.DBObjects O
	LEFT JOIN @ss ss ON ss.ObjectName = O.ObjectName AND ss.SchemaName = O.SchemaName AND ss.ObjectType = O.ObjectType
	LEFT JOIN dbo.DDL ON ss.DDLHash = DDL.DDLHash
	WHERE O.DatabaseID=@DatabaseID
	AND (O.DDLID<>DDL.DDLID OR O.object_id <> ss.OBJECT_ID OR (ss.ObjectName IS NULL AND O.IsActive=1))

	SET @UpdateCount += @@ROWCOUNT
	IF @UpdateCount=0 AND @LastSnapshotDate IS NOT NULL
	BEGIN
		UPDATE dbo.DDLSnapshots 
			SET ValidatedDate =@SnapshotDate
		WHERE SnapshotDate = @LastSnapshotDate 
		AND DatabaseID = @DatabaseID

		SET @ValidatedSnapshotDate=@LastSnapshotDate
	
	END
	ELSE
	BEGIN
		INSERT INTO dbo.DDLSnapshots
		(
			DatabaseID,
			SnapshotDate,
			ValidatedDate,
			DiffCount
		)
		VALUES
		(   @DatabaseID,
			@SnapshotDate,
			@SnapshotDate,
			@UpdateCount
			)
	END
	INSERT INTO dbo.DDLSnapshotsLog(DatabaseID,SnapshotDate,ValidatedSnapshot,EndDate,Duration)
	SELECT @DatabaseId,@SnapshotDate,@EndTime,@ValidatedSnapshotDate,DATEDIFF(ms,@StartTime,@EndTime)
	
END