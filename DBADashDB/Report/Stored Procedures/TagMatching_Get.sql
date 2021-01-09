CREATE PROC Report.TagMatching_Get
AS
SELECT 'Match Any Tag' AS Label,'ANY' AS Value
UNION ALL
SELECT 'Match All Tags' AS Label,'ALL' AS Value
UNION ALL
SELECT 'Exclude (Instances that match ALL tags)' AS Label,'EXCLUDEALL' AS Value
UNION ALL
SELECT 'Exclude (Instances that match ANY tags)' AS Label,'EXCLUDEANY' AS Value