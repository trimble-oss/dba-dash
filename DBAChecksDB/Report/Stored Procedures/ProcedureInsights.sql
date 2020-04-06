CREATE PROC [Report].[ProcedureInsights](@UTCOffset INT = 0, @Top INT=30,@Instance SYSNAME,@DatabaseID INT=NULL,@Type VARCHAR(50)='PROCEDURE')
WITH EXEC AS OWNER
AS
DECLARE @SQL NVARCHAR(MAX)=N'
WITH T AS (
SELECT CAST(DATEADD(mi, @UTCOffset, PS.SnapshotDate) AS DATE) DT,
       D.name,
       P.object_name,
       P.object_id,
       D.DatabaseID,
	   ' + CASE @Type WHEN 'PROCEDURE' THEN 'P.ProcID' WHEN 'FUNCTION' THEN 'P.FunctionID' ELSE NULL END + ' AS ProcID,
       SUM(PS.total_elapsed_time) TotalDuration,
	   SUM(PS.execution_count) ExecutionCount,
	   SUM(SUM(PS.total_elapsed_time)) OVER(PARTITION BY ' + CASE @Type WHEN 'PROCEDURE' THEN 'P.ProcID' WHEN 'FUNCTION' THEN 'P.FunctionID' ELSE NULL END + ') TotalDurationAllDays
FROM ' + CASE @Type WHEN 'PROCEDURE' THEN 'dbo.ProcStats_60MIN' WHEN 'FUNCTION' THEN 'dbo.FunctionStats_60MIN' ELSE NULL END + ' AS PS
    ' + CASE @Type WHEN 'PROCEDURE' THEN 'JOIN dbo.Procs P ON P.ProcID = PS.ProcID' WHEN 'FUNCTION' THEN 'JOIN dbo.Functions P ON P.FunctionID = PS.FunctionID' ELSE NULL END + '
    JOIN dbo.Databases D ON D.DatabaseID = P.DatabaseID
    JOIN dbo.Instances I ON I.InstanceID = D.InstanceID
WHERE I.Instance = @Instance
AND PS.InstanceID = I.InstanceID
' + CASE WHEN @DatabaseID IS NULL THEN '' ELSE 'AND D.DatabaseID= @DatabaseID' END + '
AND PS.SnapshotDate>= DATEADD(d,-90,CAST(DATEADD(mi,-@UTCOffset,GETUTCDATE()) AS DATE))
GROUP BY CAST(DATEADD(mi, @UTCOffset, PS.SnapshotDate) AS DATE),
         D.name,
         P.object_name,
         P.object_id,
         D.DatabaseID,
		 ' + CASE @Type WHEN 'PROCEDURE' THEN 'P.ProcID' WHEN 'FUNCTION' THEN 'P.FunctionID' ELSE NULL END + '
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
	EXEC sp_executesql @SQL,N'@Instance SYSNAME,@DatabaseID INT,@UTCOffset INT,@Top INT',@Instance,@DatabaseID,@UTCOffset,@Top
END 
ELSE
BEGIN
	DECLARE @t TABLE
	(
		[DT] DATE,
		[name] NVARCHAR(128),
		[object_name] NVARCHAR(128),
		[object_id] INT,
		[DatabaseID] INT,
		[ProcID] INT,
		[TotalDuration] BIGINT,
		[ExecutionCount] BIGINT,
		[TotalDurationAllDays] BIGINT,
		[Rank] BIGINT
	);
	SELECT * FROM @t
END