CREATE PROC dbo.RunningQueriesServerSummary_Get(
	@InstanceIDs NVARCHAR(MAX)=NULL
)
AS
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
	SELECT value
	FROM STRING_SPLIT(@InstanceIDs,',')
END;
WITH T AS (
	SELECT I.InstanceID,
		   I.ConnectionID AS Instance,
		   S.SnapshotDateUTC,
		   S.RunningQueries,
		   S.BlockedQueries,
		   S.BlockedQueriesWaitMs,
		   S.MaxMemoryGrant*8 as MaxMemoryGrantKB,
		   S.LongestRunningQueryMs,
		   S.CriticalWaitCount,
		   S.CriticalWaitTime,
		   ROW_NUMBER() OVER(PARTITION BY S.InstanceID ORDER BY S.SnapshotDateUTC DESC) rnum
	FROM dbo.RunningQueriesSummary S 
	JOIN dbo.Instances I ON I.InstanceID = S.InstanceID
	WHERE EXISTS(SELECT 1 FROM @Instances t WHERE t.InstanceID = I.InstanceID)
	AND S.SnapshotDateUTC>=DATEADD(mi,-15,GETUTCDATE())
	AND S.SnapshotDateUTC< DATEADD(mi,1,GETUTCDATE())
)
SELECT T.InstanceID,
       T.Instance,
       T.SnapshotDateUTC,
       T.RunningQueries,
       T.BlockedQueries,
       T.BlockedQueriesWaitMs,
       T.MaxMemoryGrantKB,
       T.LongestRunningQueryMs,
       T.CriticalWaitCount,
       T.CriticalWaitTime
FROM T 
WHERE T.rnum = 1