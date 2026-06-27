CREATE PROC dbo.DBFiles_Get(
	@InstanceIDs VARCHAR(MAX)=NULL,
	@DatabaseID INT=NULL,
	@IncludeCritical BIT=1,
	@IncludeWarning BIT=1,
	@IncludeNA BIT=0,
	@IncludeOK BIT=0,
	@FilegroupLevel BIT=1,
    @Types VARCHAR(50)=NULL,
    @ShowHidden BIT=1,
    @DriveName NVARCHAR(256)=NULL
)
AS
DECLARE @IDs IDs
IF @InstanceIDs IS NOT NULL
BEGIN
	INSERT INTO @IDs(ID)
	SELECT value FROM STRING_SPLIT(@InstanceIDs,',')
END

EXEC dbo.DBFilesReport_Get
	@InstanceIDs = @IDs,
	@DatabaseID = @DatabaseID,
	@IncludeCritical = @IncludeCritical,
	@IncludeWarning = @IncludeWarning,
	@IncludeNA = @IncludeNA,
	@IncludeOK = @IncludeOK,
	@FilegroupLevel = @FilegroupLevel,
	@Types = @Types,
	@ShowHidden = @ShowHidden,
	@DriveName = @DriveName
