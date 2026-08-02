CREATE PROC dbo.AgentJobExclusions_Upd(
    @AgentJobExclusionID INT = NULL OUTPUT, /* NULL to add a new exclusion, otherwise the ID of the exclusion to update */
    @InstanceID INT, /* -1 = Root (applies to all instances) */
    @JobNameFilter NVARCHAR(128) = NULL,
    @CategoryFilter NVARCHAR(128) = NULL,
    @DescriptionFilter NVARCHAR(512) = NULL
)
AS
SET XACT_ABORT ON
SET NOCOUNT ON

/* Normalize blank filters to NULL (NULL = match any) */
SET @JobNameFilter = NULLIF(LTRIM(RTRIM(@JobNameFilter)), '')
SET @CategoryFilter = NULLIF(LTRIM(RTRIM(@CategoryFilter)), '')
SET @DescriptionFilter = NULLIF(LTRIM(RTRIM(@DescriptionFilter)), '')

IF @JobNameFilter IS NULL AND @CategoryFilter IS NULL AND @DescriptionFilter IS NULL
BEGIN
    RAISERROR('At least one of @JobNameFilter, @CategoryFilter or @DescriptionFilter must be supplied.  An exclusion with no filters would exclude every job.', 11, 1)
    RETURN
END

IF @AgentJobExclusionID IS NULL
BEGIN
    INSERT INTO dbo.AgentJobExclusions(InstanceID, JobNameFilter, CategoryFilter, DescriptionFilter)
    VALUES(@InstanceID, @JobNameFilter, @CategoryFilter, @DescriptionFilter)

    SET @AgentJobExclusionID = SCOPE_IDENTITY()
END
ELSE
BEGIN
    UPDATE dbo.AgentJobExclusions
    SET InstanceID = @InstanceID,
        JobNameFilter = @JobNameFilter,
        CategoryFilter = @CategoryFilter,
        DescriptionFilter = @DescriptionFilter
    WHERE AgentJobExclusionID = @AgentJobExclusionID
END
