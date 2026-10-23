CREATE PROC dbo.JobCategories_Get(
	@InstanceID INT=NULL,
	@InstanceIDs IDs READONLY /* for the multi-instance timeline - categories used on any of these instances */
)
AS
SELECT DISTINCT category 
FROM dbo.Jobs
WHERE (InstanceID = @InstanceID OR InstanceID IN (SELECT ID FROM @InstanceIDs))
AND category IS NOT NULL
ORDER BY category