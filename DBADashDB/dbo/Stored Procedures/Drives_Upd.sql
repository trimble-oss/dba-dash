CREATE PROC [dbo].[Drives_Upd](@Drives Drives READONLY,@InstanceID INT,@SnapshotDate DATETIME2(2))
AS
DECLARE @Ref VARCHAR(30)='Drives'
INSERT INTO dbo.DriveSnapshot(DriveID,Capacity,FreeSpace, SnapshotDate)
SELECT D.DriveID,T.Capacity,T.FreeSpace,@SnapshotDate
FROM @Drives T
JOIN dbo.Drives D ON T.Name = D.Name AND D.InstanceID = @InstanceID
WHERE NOT EXISTS(SELECT 1 
				FROM dbo.DriveSnapshot DSS 
				WHERE DSS.DriveID = D.DriveID 
				AND DSS.SnapshotDate = @SnapshotDate
				)

IF NOT EXISTS(SELECT 1 FROM dbo.CollectionDates WHERE SnapshotDate>=@SnapshotDate AND InstanceID = @InstanceID AND Reference=@Ref)
BEGIN
	WITH  T AS (
		SELECT * 
		FROM dbo.Drives 
		WHERE InstanceID = @InstanceID
	)
	MERGE T
	USING (SELECT * FROM @Drives) as S
		ON S.Name = T.Name 

	WHEN MATCHED THEN
	UPDATE SET T.Capacity=S.Capacity,
	T.FreeSpace = S.FreeSpace,
	T.Label = S.Label,
	T.IsActive=1
	WHEN NOT MATCHED BY TARGET THEN 
	INSERT(InstanceID,
		Name,
		Capacity,
		FreeSpace,
		Label,
		IsActive)
	VALUES(@InstanceID,
		Name,
		Capacity,
		FreeSpace,
		Label,
		1)
	WHEN NOT MATCHED BY SOURCE THEN 
	UPDATE SET T.IsActive=0;

	EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										 @Reference = @Ref,
										 @SnapshotDate = @SnapshotDate
END
