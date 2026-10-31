/*
	The active instances whose display name starts with @InstanceFilter, exact matches first - for MCP tools that
	work on one instance to turn the name the client gave into an InstanceID.
*/
CREATE PROC AI.Instance_Get(
	@InstanceFilter NVARCHAR(256),
	/* An empty filter with a large @MaxRows lists every active instance, for a tool that scopes to all of them. */
	@MaxRows INT = 20
)
AS
SET NOCOUNT ON
SELECT TOP(@MaxRows)
	I.InstanceID,
	I.InstanceDisplayName
FROM dbo.Instances I
WHERE I.IsActive = 1
AND I.InstanceDisplayName LIKE @InstanceFilter + '%'
ORDER BY CASE WHEN I.InstanceDisplayName = @InstanceFilter THEN 0 ELSE 1 END, I.InstanceDisplayName
