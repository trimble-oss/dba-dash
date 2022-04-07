CREATE   PROC [dbo].[DDLSnapshot_Add](
	@ss dbo.DDLSnapshot READONLY,
	@ConnectionId SYSNAME=NULL,
	@InstanceID INT=NULL,
	@DB SYSNAME,
	@SnapshotDate DATETIME2(3),
	@StartTime DATETIME2(3),
	@EndTime DATETIME2(3),
	@SnapshotOptions VARCHAR(MAX),
	@SnapshotOptionsHash BINARY(32)
)
AS
SET XACT_ABORT ON
DECLARE @Count INT=0
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
WHERE s.DatabaseID = @DatabaseId
ORDER BY s.SnapshotDate DESC

DECLARE @DDLSnapshotOptionsID INT

SELECT @DDLSnapshotOptionsID= DDLSnapshotOptionsID 
FROM dbo.DDLSnapshotOptions 
WHERE SnapshotOptionsHash = @SnapshotOptionsHash

IF @@ROWCOUNT =0
BEGIN
	INSERT INTO dbo.DDLSnapshotOptions
	(
	    SnapshotOptionsHash,
	    SnapshotOptions
	)
	VALUES
	(  @SnapshotOptionsHash,
	    @SnapshotOptions
	    )
	SET @DDLSnapshotOptionsID = SCOPE_IDENTITY()
END

IF @DatabaseId IS NOT NULL
BEGIN;
	WITH DeDupeDDL AS (
		SELECT ss.DDLHash,
			ss.DDL,
			ROW_NUMBER() OVER(PARTITION BY ss.DDLHash ORDER BY ss.DDLHash) AS rnum
		FROM @ss ss
		WHERE NOT EXISTS(
				SELECT 1 
				FROM dbo.DDL 
				WHERE ss.DDLHash = DDL.DDLHash
				)
	)
	INSERT INTO dbo.DDL(DDLHash,DDL)
	SELECT DDLHash,DDL
	FROM DeDupeDDL
	WHERE rnum = 1;

	BEGIN TRAN;
		WITH T AS (
			SELECT * 
			FROM dbo.DBObjects
			WHERE DatabaseID = @DatabaseId
		)
		MERGE T
		USING(SELECT ss.ObjectName,
					 ss.SchemaName,
					 ss.ObjectType,
					 ss.OBJECT_ID,
					 ss.DDLHash,
					 ss.DDL,
					 ss.ObjectDateCreated,
					 ss.ObjectDateModified,
					 DDL.DDLID
				FROM @ss ss
				LEFT JOIN dbo.DDL ON DDL.DDLHash = ss.DDLHash
				) AS S
			ON S.ObjectName = T.ObjectName AND S.SchemaName = T.SchemaName AND S.ObjectType = T.ObjectType
		WHEN NOT MATCHED THEN INSERT(DatabaseID,
									ObjectType,
									object_id,
									SchemaName,
									ObjectName,
									IsActive)
		VALUES(@DatabaseId,s.ObjectType,s.object_id,s.SchemaName,s.ObjectName,1 )
		WHEN MATCHED AND T.IsActive=0
		THEN UPDATE SET T.IsActive=1
		WHEN NOT MATCHED BY SOURCE AND T.IsActive=1 
					THEN UPDATE SET T.IsActive=0;

		UPDATE H
			SET H.SnapshotValidTo = @SnapshotDate
		FROM dbo.DDLHistory H
		WHERE H.DatabaseID=@DatabaseId
		AND H.SnapshotValidTo = '9999-12-31'
		AND NOT EXISTS(
				SELECT 1
				FROM @ss AS SS 
				JOIN dbo.DDL ON SS.DDLHash = DDL.DDLHash
				JOIN dbo.DBObjects O ON O.ObjectName = SS.ObjectName
									AND O.ObjectType = SS.ObjectType
									AND O.SchemaName = SS.SchemaName								
				WHERE O.ObjectID = H.ObjectID
				AND DDL.DDLID = H.DDLID
				AND O.DatabaseID = @DatabaseId
				)
		SET @Count+=@@ROWCOUNT		

		INSERT INTO dbo.DDLHistory
		(
			DatabaseID,
			ObjectID,
			DDLID,
			SnapshotValidFrom,
			SnapshotValidTo,
			object_id,
			ObjectDateCreated,
			ObjectDateModified
		)
		SELECT O.DatabaseID,O.ObjectID,DDL.DDLID,@SnapshotDate,'9999-12-31',SS.OBJECT_ID,SS.ObjectDateCreated,SS.ObjectDateModified
		FROM @ss AS SS 
		JOIN dbo.DDL ON SS.DDLHash = DDL.DDLHash
		JOIN dbo.DBObjects O ON O.ObjectName = SS.ObjectName
							AND O.ObjectType = SS.ObjectType
							AND O.SchemaName = SS.SchemaName
							AND O.DatabaseID = @DatabaseId
		WHERE NOT EXISTS(SELECT 1
						FROM dbo.DDLHistory H
						WHERE H.DatabaseID=@DatabaseId
						AND H.SnapshotValidTo = '9999-12-31'
						AND H.ObjectID = O.ObjectID)

		SET @Count +=@@ROWCOUNT



		IF @Count=0
		BEGIN
			UPDATE dbo.DDLSnapshots 
				SET ValidatedDate =@SnapshotDate
			WHERE SnapshotDate = @LastSnapshotDate 
			AND DatabaseID = @DatabaseId

			SET @ValidatedSnapshotDate=@LastSnapshotDate
	
		END
		ELSE
		BEGIN

			INSERT INTO dbo.DDLSnapshots
			(
				DatabaseID,
				SnapshotDate,
				ValidatedDate,
				Created,
				Dropped,
				Modified,
				DDLSnapshotOptionsID
			)
			SELECT @DatabaseId,
					@SnapshotDate,
					@SnapshotDate,
					SUM(CASE WHEN [Action] = 'Created' THEN 1 ELSE 0 END) AS Created,
					SUM(CASE WHEN [Action] = 'Dropped' THEN 1 ELSE 0 END) AS Dropped,
					SUM(CASE WHEN [Action] = 'Modified' THEN 1 ELSE 0 END) AS Modified,
					@DDLSnapshotOptionsID
			FROM dbo.DBSnapshotDiff(@DatabaseId,@SnapshotDate)


		END
		INSERT INTO dbo.DDLSnapshotsLog(DatabaseID,SnapshotDate,ValidatedSnapshot,EndDate,Duration)
		SELECT @DatabaseId,@SnapshotDate,@ValidatedSnapshotDate,@EndTime,DATEDIFF(ms,@StartTime,@EndTime)
	COMMIT
END