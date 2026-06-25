CREATE PROC dbo.Drives_Get(
	@InstanceIDs VARCHAR(MAX)=NULL,
	@InstanceID INT=NULL,
	@IncludeCritical BIT=1,
	@IncludeWarning BIT=1,
	@IncludeNA BIT=0,
	@IncludeOK BIT=0,
	@IncludeMetrics BIT=0,
	@ShowHidden BIT=1,
	@DriveName NVARCHAR(256)=NULL,
	@HasMetrics BIT=0
)
AS
DECLARE @IDs IDs

IF @InstanceID IS NOT NULL
BEGIN
	INSERT INTO @IDs(ID) VALUES(@InstanceID)
END
ELSE IF @InstanceIDs IS NOT NULL
BEGIN
	INSERT INTO @IDs(ID)
	SELECT Value FROM STRING_SPLIT(@InstanceIDs,',')
END

EXEC dbo.DrivesReport_Get
	@InstanceIDs = @IDs,
	@IncludeCritical = @IncludeCritical,
	@IncludeWarning = @IncludeWarning,
	@IncludeNA = @IncludeNA,
	@IncludeOK = @IncludeOK,
	@IncludeMetrics = @IncludeMetrics,
	@ShowHidden = @ShowHidden,
	@DriveName = @DriveName,
	@HasMetrics = @HasMetrics
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[Drives_Get] TO [Reports]
    AS [dbo];
