CREATE PROC DBADash.AIModels_Get
AS
SELECT ModelName, DisplayName
FROM DBADash.AIModels
WHERE IsEnabled = 1
ORDER BY SortOrder, DisplayName
