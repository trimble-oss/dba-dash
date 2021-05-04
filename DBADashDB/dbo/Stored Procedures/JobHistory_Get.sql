CREATE PROC dbo.JobHistory_Get(
	@InstanceID INT,
	@JobID UNIQUEIDENTIFIER,
	@StepID INT=0,
	@instance_id INT=NULL,
	@Top INT=500,
	@FailedOnly BIT=0
)
AS
DECLARE @SQL NVARCHAR(MAX)
DECLARE @instance_id_from INT
IF @instance_id IS NOT NULL
BEGIN
	SELECT TOP(1) @instance_id_from = instance_id 
	FROM dbo.JobHistory
	WHERE InstanceID = @InstanceID 
	AND job_id = @JobID
	AND step_id=0
	AND instance_id<@instance_id
	ORDER BY instance_id DESC

	SET @instance_id_from = ISNULL(@instance_id_from,0)
END

SET @SQL = N'
SELECT TOP(@Top) 
	JH.InstanceID,
	J.name,
	JH.job_id,
	JH.RunDateTime,
	JH.instance_id,
	JH.step_id,
	JH.step_name,
	JH.sql_message_id,
	JH.sql_severity,
	JH.message,
	JH.run_status,
	CASE WHEN JH.run_status=0 THEN ''Failed'' WHEN JH.run_status=1 THEN ''Succeeded'' WHEN JH.run_status=2 THEN ''Retry'' WHEN JH.run_status=3 THEN ''Cancelled'' WHEN JH.run_status=4 THEN ''In Progress'' ELSE FORMAT(JH.run_status,''N0'') END as run_status_description,
	JH.RunDurationSec,
	JH.retries_attempted
FROM dbo.JobHistory JH
JOIN dbo.Jobs J ON JH.job_id = J.job_id AND J.InstanceID = JH.InstanceID
WHERE JH.InstanceID = @InstanceID
AND JH.job_id = @JobID
' + CASE WHEN @StepID IS NULL THEN '' ELSE 'AND JH.step_id = @StepID' END + ' 
' + CASE WHEN @instance_id IS NULL THEN '' ELSE 'AND JH.instance_id>@instance_id_from AND JH.instance_id <= @instance_id' END + ' 
' + CASE WHEN @FailedOnly=1 THEN 'AND JH.run_status<> 1' ELSE '' END + '
ORDER BY JH.instance_id DESC'

EXEC sp_executesql @SQL,N'@InstanceID INT,@JobID UNIQUEIDENTIFIER,@StepID INT,@instance_id INT,@instance_id_from INT,@Top INT',@InstanceID,@JobID,@StepID,@instance_id,@instance_id_from,@Top
