CREATE PROC Alert.TagScopeMatchingInstanceCount_Get(
	@ApplyToTagIDs VARCHAR(MAX)
)
AS
/*
	Preview for the tag picker: how many active instances match the selected tags (same rules as alert evaluation).
	Hidden instances are counted separately as rules only apply to them with Apply To Hidden.
*/
SET NOCOUNT ON

IF EXISTS(SELECT 1 FROM Alert.TagScope_Normalize(-1,@ApplyToTagIDs) N WHERE LEN(N.ApplyToTagIDs) > 800)
BEGIN
	THROW 50000, 'Too many tags selected.  The list of TagIDs is limited to 800 characters.', 1;
END

SELECT COUNT(CASE WHEN I.ShowInSummary = 1 THEN 1 END) AS InstanceCount,
	COUNT(CASE WHEN I.ShowInSummary = 0 THEN 1 END) AS HiddenInstanceCount
FROM Alert.TagScope_Normalize(-1,@ApplyToTagIDs) N
CROSS JOIN dbo.Instances I
CROSS APPLY Alert.TagScopeMatchesInstance(N.ApplyToTagID,N.ApplyToTagIDs,I.InstanceID) TM
WHERE I.IsActive = 1
AND (N.ApplyToTagID = -1 OR TM.IsMatch = 1)
