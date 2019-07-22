CREATE PROC [dbo].[Drives_Upd](@Drives Drives READONLY,@InstanceID INT,@SnapshotDate DATETIME2(2))
AS
IF NOT EXISTS(SELECT 1 FROM dbo.DriveSnapshot SS INNER JOIN dbo.Drives D ON D.DriveID = SS.DriveID WHERE D.InstanceID=@InstanceID AND SS.SnapshotDate = @SnapshotDate)
BEGIN
	INSERT INTO dbo.DriveSnapshot(DriveID,Capacity,FreeSpace, SnapshotDate)
	SELECT D.DriveID,T.Capacity,T.FreeSpace,@SnapshotDate
	FROM @Drives T
	JOIN dbo.Drives D ON T.Name = D.Name AND D.InstanceID = @InstanceID

END
IF NOT EXISTS(SELECT 1 FROM dbo.SnapshotDates WHERE DrivesDate >=@SnapshotDate AND InstanceID=@InstanceID)
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

	UPDATE dbo.SnapshotDates
	SET DrivesDate=@SnapshotDate
	WHERE InstanceID=@InstanceID
END
