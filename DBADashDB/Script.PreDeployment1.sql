/* Remove partitioning from Switch.JobStats_60MIN table.  #411 */
IF EXISTS(
	SELECT 1 
	FROM sys.partitions
	WHERE object_id = OBJECT_ID('Switch.JobStats_60MIN')
	AND index_id IN(0,1)
	HAVING COUNT(*) > 1
	)
BEGIN
	PRINT 'Remove partitioning from Switch.JobStats_60MIN'
	ALTER TABLE [Switch].[JobStats_60MIN] DROP CONSTRAINT [PK_JobStats_60MIN] 
	ALTER TABLE [Switch].[JobStats_60MIN] ADD  CONSTRAINT [PK_JobStats_60MIN] PRIMARY KEY CLUSTERED 
	(
		[InstanceID] ASC,
		[job_id] ASC,
		[step_id] ASC,
		[RunDateTime] ASC
	) ON [PRIMARY]
END