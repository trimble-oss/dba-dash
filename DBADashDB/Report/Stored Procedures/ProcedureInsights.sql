CREATE PROC [Report].[ProcedureInsights](@UTCOffset INT = 0, @Top INT=30,@Instance SYSNAME,@DatabaseID INT=NULL,@Types VARCHAR(200)='P,FN,TR,TA,PC,X')
WITH EXEC AS OWNER
AS
DECLARE @SQL NVARCHAR(MAX)=N'
WITH T AS (
SELECT CAST(DATEADD(mi, @UTCOffset, OES.SnapshotDate) AS DATE) DT,
       D.name,
       O.ObjectName,
	   O.SchemaName,
       O.object_id,
       D.DatabaseID,
	   O.ObjectID,
       SUM(OES.total_elapsed_time) TotalDuration,
	   SUM(OES.execution_count) ExecutionCount,
	   SUM(SUM(OES.total_elapsed_time)) OVER(PARTITION BY O.ObjectID) TotalDurationAllDays
FROM dbo.ObjectExecutionStats_60MIN OES
JOIN dbo.DBObjects O ON O.ObjectID = OES.ObjectID
    JOIN dbo.Databases D ON D.DatabaseID = O.DatabaseID
    JOIN dbo.Instances I ON I.InstanceID = D.InstanceID
WHERE I.Instance = @Instance
AND OES.InstanceID = I.InstanceID
' + CASE WHEN @DatabaseID IS NULL THEN '' ELSE 'AND D.DatabaseID= @DatabaseID' END + '
AND OES.SnapshotDate>= DATEADD(d,-90,CAST(DATEADD(mi,-@UTCOffset,GETUTCDATE()) AS DATE))
AND EXISTS(SELECT 1 FROM STRING_SPLIT(@Types,'','') ss WHERE ss.Value =  O.ObjectType)
GROUP BY CAST(DATEADD(mi, @UTCOffset, OES.SnapshotDate) AS DATE),
         D.name,
         O.ObjectName,
		 O.SchemaName,
         O.object_id,
		 O.ObjectID,
         D.DatabaseID
)
, rankings AS (
	SELECT *,
		DENSE_RANK() OVER(ORDER BY TotalDurationAllDays DESC) Rank 
	FROM T
)
SELECT *
FROM rankings
WHERE rankings.Rank<=@Top
ORDER BY DT
OPTION(RECOMPILE)'

PRINT @SQL

IF @Instance IS NOT NULL
BEGIN
	EXEC sp_executesql @SQL,N'@Instance SYSNAME,@DatabaseID INT,@UTCOffset INT,@Top INT,@Types VARCHAR(200)',@Instance,@DatabaseID,@UTCOffset,@Top,@Types
END 
ELSE
BEGIN
	DECLARE @t TABLE
	(
		[DT] DATE,
		[name] NVARCHAR(128),
		[ObjectName] NVARCHAR(128),
		[SchemaName] NVARCHAR(128),
		[object_id] INT,
		[DatabaseID] INT,
		[ObjectID] BIGINT,
		[TotalDuration] BIGINT,
		[ExecutionCount] BIGINT,
		[TotalDurationAllDays] BIGINT,
		[Rank] BIGINT
	);
	SELECT * FROM @t
END