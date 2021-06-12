CREATE PROC dbo.InstancesWithDDLSnapshot_Get(
	@TagIDs VARCHAR(MAX)=NULL
)
AS
SELECT I.Instance
FROM dbo.InstancesMatchingTags(@TagIDs) I
WHERE I.IsActive=1
AND EXISTS(SELECT 1
			FROM dbo.Databases D 
			JOIN dbo.DDLSnapshots SS ON SS.DatabaseID = D.DatabaseID
			WHERE D.InstanceID = I.InstanceID
			)
GROUP BY I.Instance
ORDER BY I.Instance
