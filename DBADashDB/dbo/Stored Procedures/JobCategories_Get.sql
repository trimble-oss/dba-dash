CREATE PROC dbo.JobCategories_Get(
	@InstanceID INT
)
AS
SELECT DISTINCT category 
FROM dbo.Jobs
WHERE InstanceID = @InstanceID
AND category IS NOT NULL