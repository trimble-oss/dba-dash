/*
	Acknowledge the failures/warnings of an elastic job (or every elastic job on the instance with @job_id NULL), or clear the
	acknowledgement with @Clear = 1.  See dbo.ElasticJobStatus.  The same as dbo.JobErrorAck for SQL Agent jobs.
*/
CREATE PROCEDURE dbo.ElasticJobErrorAck
	@InstanceID INT,
	@job_id UNIQUEIDENTIFIER=NULL,
	@Clear BIT=0
AS
UPDATE J
	SET AckDate = CASE WHEN @Clear = 1 THEN NULL ELSE GETUTCDATE() END
FROM dbo.ElasticJobs J
WHERE J.InstanceID = @InstanceID
AND EXISTS(	SELECT 1
			FROM dbo.ElasticJobStatus EJS
			WHERE EJS.InstanceID = J.InstanceID
			AND EJS.job_id = J.job_id
			AND EJS.JobStatus IN(1, 2, 5)
			)
AND (J.job_id = @job_id OR @job_id IS NULL)
OPTION(RECOMPILE)
