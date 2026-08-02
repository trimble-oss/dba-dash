CREATE PROC dbo.AgentJobExclusions_Get(
    @InstanceID INT = NULL /* NULL = all levels.  -1 = Root.  Otherwise a specific instance. */
)
AS
SET NOCOUNT ON

SELECT ex.AgentJobExclusionID,
       ex.InstanceID,
       ex.JobNameFilter,
       ex.CategoryFilter,
       ex.DescriptionFilter
FROM dbo.AgentJobExclusions ex
WHERE (@InstanceID IS NULL OR ex.InstanceID = @InstanceID)
ORDER BY ex.InstanceID, ex.JobNameFilter, ex.CategoryFilter
