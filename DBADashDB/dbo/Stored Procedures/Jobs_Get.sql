CREATE PROC Jobs_Get(
	@InstanceID INT,
	@JobId UNIQUEIDENTIFIER=NULL
)
AS
SELECT	job_id,
		name,
		enabled
FROM dbo.Jobs 
WHERE InstanceID = @InstanceID 
AND IsActive=1
AND (job_id = @JobId OR @JobId IS NULL)
ORDER BY name