CREATE PROCEDURE dbo.JobErrorAck
	@InstanceID INT,
	@job_id UNIQUEIDENTIFIER=NULL,
	@Clear BIT=0
AS
UPDATE J 
	SET AckDate = CASE WHEN @Clear = 1 THEN NULL ELSE GETUTCDATE() END
FROM dbo.Jobs J
WHERE InstanceID = @InstanceID 
AND EXISTS(SELECT 1 
			FROM dbo.AgentJobStatus AJS
			WHERE AJS.InstanceID = J.InstanceID 
			AND AJS.job_id = J.job_id
			AND AJS.JobStatus IN(1, 2, 5)
			)
AND (J.job_id = @job_id OR @job_id IS NULL)
OPTION(RECOMPILE)