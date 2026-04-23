CREATE PROC AI.Models_Get
AS
SET NOCOUNT ON;
SELECT ModelID,
	   ModelName,
	   DisplayName,
	   SortOrder,
	   IsEnabled
FROM AI.Models
WHERE IsEnabled = 1
ORDER BY SortOrder, DisplayName
