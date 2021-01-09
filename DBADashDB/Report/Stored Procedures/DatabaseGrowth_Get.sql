CREATE PROC Report.DatabaseGrowth_Get(@InstanceIDs VARCHAR(MAX)=NULL,@DatabaseID INT=NULL,@Days INT=90)
AS
DECLARE @InstanceID INT
DECLARE @Instances TABLE(
	InstanceID INT PRIMARY KEY
)
IF @InstanceIDs IS NULL
BEGIN
	INSERT INTO @Instances
	(
	    InstanceID
	)
	SELECT InstanceID 
	FROM dbo.Instances 
	WHERE IsActive=1
END 
ELSE 
BEGIN
	INSERT INTO @Instances
	(
		InstanceID
	)
	SELECT Item
	FROM dbo.SplitStrings(@InstanceIDs,',')

	IF @@ROWCOUNT=1
	BEGIN
		SELECT @InstanceID=InstanceID 
		FROM @Instances
	END
END

SELECT I.InstanceID,CASE WHEN @InstanceID IS NULL THEN NULL ELSE D.DatabaseID END AS DatabaseID, CASE WHEN @InstanceID IS NULL THEN I.Instance WHEN @DatabaseID IS NULL THEN D.name ELSE D.name + ' - ' + F.filegroup_name END AS name,
       SS.SnapshotDate,
       ISNULL(SUM(SS.space_used) / 128.0, 0) AS UsedMB,
       ISNULL(SUM(SS.Size - SS.space_used) / 128.0,0) AS FreeMB,
	   SUM(CASE WHEN SS.space_used IS NULL THEN SS.Size ELSE 0 END)/128 AS OtherMB,
       SUM(SS.Size) / 128.0 AS AllocatedMB
FROM dbo.Instances I
    JOIN dbo.Databases D ON D.InstanceID = I.InstanceID
    JOIN dbo.DBFiles F ON D.DatabaseID = F.DatabaseID
    JOIN dbo.DBFileSnapshot SS ON SS.FileID = F.FileID
WHERE D.IsActive = 1
AND   I.IsActive = 1
AND F.type<>1
AND (I.InstanceID = @InstanceID OR  @InstanceID IS NULL)
AND (D.DatabaseID=@DatabaseID OR @DatabaseID IS NULL)
AND EXISTS(SELECT 1 FROM @Instances t WHERE I.InstanceID = t.InstanceID)
AND   SS.SnapshotDate >= DATEADD(d, -@Days, GETUTCDATE())
GROUP BY CASE WHEN @InstanceID IS NULL THEN I.Instance WHEN @DatabaseID IS NULL THEN D.name ELSE D.name + ' - ' + F.filegroup_name END,I.InstanceID,CASE WHEN @InstanceID IS NULL THEN NULL ELSE D.DatabaseID END,
         SS.SnapshotDate
OPTION (RECOMPILE);