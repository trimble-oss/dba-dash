CREATE PROC dbo.DDLSnapshotInstanceSummary_Get(@InstanceIDs VARCHAR(MAX)=NULL)
AS
DECLARE @SQL NVARCHAR(MAX) 
SET @SQL =N'
SELECT I.InstanceGroupName,
	  MAX(lss.SnapshotDate) as LastUpdated,
	  MAX(lss.ValidatedDate) as LastValidated
FROM dbo.Databases D
CROSS APPLY(SELECT TOP(1)	ss.SnapshotDate,
							ValidatedDate 
			FROM dbo.DDLSnapshots ss 
			WHERE ss.DatabaseID = D.DatabaseID 
			ORDER BY ss.SnapshotDate DESC
			) lss
JOIN dbo.Instances I ON D.InstanceID = I.InstanceID
WHERE I.IsActive=1
AND D.IsActive=1
' + CASE WHEN @InstanceIDs IS NULL THEN '' ELSE 'AND EXISTS(SELECT 1 
															FROM STRING_SPLIT(@InstanceIDs,'','') ss 
															WHERE ss.value = I.InstanceID
															)' END + '
GROUP BY I.InstanceGroupName
ORDER BY LastUpdated DESC'

EXEC sp_executesql @SQL,N'@InstanceIDs VARCHAR(MAX)',@InstanceIDs