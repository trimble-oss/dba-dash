CREATE PROC dbo.CustomChecksHistory_Get(
	@InstanceID INT,
	@Test NVARCHAR(128),
	@Context NVARCHAR(128),
	@Status TINYINT=NULL,
	@DateFrom DATETIME2(3)=NULL,
	@DateTo DATETIME2(3)=NULL
)
AS
IF @DateTo IS NULL 
	SET @DateTo = GETUTCDATE()
IF @DateFrom IS NULL 
	SET @DateFrom = DATEADD(d,-14,GETUTCDATE())
DECLARE @SQL NVARCHAR(MAX) 
SET @SQL =
'SELECT CCH.InstanceID,
	   I.ConnectionID,
       CCH.Test,
       CCH.Context,
       CCH.Status,
       CCH.Info,
       CCH.SnapshotDate
FROM dbo.CustomChecksHistory CCH
JOIN dbo.Instances I ON I.InstanceID = CCH.InstanceID
WHERE CCH.SnapshotDate>= @DateFrom
AND CCH.SnapshotDate < @DateTo
' + CASE WHEN @InstanceID IS NULL THEN '' ELSE 'AND CCH.InstanceID = @InstanceID' END + '
' + CASE WHEN @Test IS NULL THEN '' ELSE 'AND CCH.Test = @Test' END + ' 
' + CASE WHEN @Context IS NULL THEN '' ELSE 'AND CCH.Context = @Context' END + '
' + CASE WHEN @Status IS NULL THEN '' ELSE 'AND CCH.Status = @Status' END  + '
ORDER BY CCH.SnapshotDate DESC'

EXEC sp_executesql @SQL,N'@InstanceID INT,@Test NVARCHAR(128),@Context NVARCHAR(128),@Status TINYINT,@DateFrom DATETIME2(3),@DateTo DATETIME2(3)',@InstanceID,@Test,@Context,@Status,@DateFrom,@DateTo