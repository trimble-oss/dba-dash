CREATE PROC dbo.AgentJobs_Get(
	@InstanceIDs VARCHAR(MAX) = NULL,
	@enabled TINYINT=NULL,
	@IncludeCritical BIT=1,
	@IncludeWarning BIT=1,
	@IncludeNA BIT=0,
	@IncludeOK BIT=0,
    @IncludeACK BIT=1,
	@JobName SYSNAME=NULL,
    @JobID UNIQUEIDENTIFIER=NULL,
    @ShowHidden BIT=1
)
AS
DECLARE @IDs IDs

IF @InstanceIDs IS NOT NULL
BEGIN
	INSERT INTO @IDs(ID)
	SELECT Value FROM STRING_SPLIT(@InstanceIDs,',')
END

EXEC dbo.AgentJobsReport_Get
	@InstanceIDs = @IDs,
	@enabled = @enabled,
	@IncludeCritical = @IncludeCritical,
	@IncludeWarning = @IncludeWarning,
	@IncludeNA = @IncludeNA,
	@IncludeOK = @IncludeOK,
	@IncludeACK = @IncludeACK,
	@JobName = @JobName,
	@JobID = @JobID,
	@ShowHidden = @ShowHidden
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[AgentJobs_Get] TO [Reports]
    AS [dbo];
