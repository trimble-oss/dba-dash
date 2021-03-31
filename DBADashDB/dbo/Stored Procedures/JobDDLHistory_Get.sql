CREATE   PROC dbo.JobDDLHistory_Get(
	@JobID UNIQUEIDENTIFIER,
	@InstanceID INT
)
AS
SELECT J.name,
       H.job_id,
       H.version_number,
       H.SnapshotDate,
       H.date_modified,
       H.DDLID,
	   LAG(H.DDLID) OVER(ORDER BY H.SnapshotDate) AS PreviousDDLID
FROM dbo.Jobs J 
JOIN dbo.JobDDLHistory H ON J.InstanceID = H.InstanceID AND J.job_id = H.job_id
WHERE J.job_id = @JobID
AND J.InstanceID=@InstanceID
ORDER BY H.SnapshotDate DESC