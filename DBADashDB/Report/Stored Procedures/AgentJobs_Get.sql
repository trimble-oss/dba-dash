CREATE PROC [Report].[AgentJobs_Get](
	@InstanceIDs VARCHAR(MAX) = NULL,
	@enabled TINYINT=1,
	@FilterLevel TINYINT=2,
	@JobName SYSNAME=NULL
)
AS
DECLARE @IncludeNA BIT=0,@IncludeWarning BIT=0,@IncludeOK BIT=0,@IncludeCritical BIT=1
IF @FilterLevel=2
BEGIN 
	SELECT @IncludeWarning=1
END
IF @FilterLevel=4
BEGIN
	SELECT @IncludeWarning=1, 
			@IncludeNA=1,
			@IncludeOK=1
END

EXEC dbo.AgentJobs_Get @InstanceIDs=@InstanceIDs,@enabled=@enabled,@JobName=@JobName,@IncludeCritical=@IncludeCritical,@IncludeWarning=@IncludeWarning,@IncludeNA=@IncludeNA,@IncludeOK=@IncludeOK

GO
