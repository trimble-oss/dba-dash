CREATE PROC [Report].[Patching_Get](@TagIDs NVARCHAR(MAX)='-1',@TagMatching VARCHAR(50)='ALL')
AS
DECLARE @Instances TABLE(
	InstanceID INT,
	Instance SYSNAME
)
INSERT INTO @Instances
(
    InstanceID,
    Instance
)
EXEC Report.Instances_Get @TagIDs=@TagIDs,@TagMatching=@TagMatching

SELECT Instance, Edition,EngineEdition,BuildClrVersion,ProductMajorVersion,ProductLevel,ProductUpdateLevel,ProductUpdateReference,ProductVersion,I.WindowsCaption,I.WindowsRelease,
	   CASE WHEN I.ProductVersion LIKE '9.%' THEN 'SQL 2005' 
			WHEN I.ProductVersion LIKE '10.0%' THEN 'SQL 2008' 
			WHEN I.ProductVersion LIKE '10.5%' THEN 'SQL 2008 R2'
			WHEN I.ProductVersion LIKE '11.%' THEN 'SQL 2012'
			WHEN I.ProductVersion LIKE '12.%' THEN 'SQL 2014'
			WHEN I.ProductVersion LIKE '13.%' THEN 'SQL 2016'
			WHEN I.ProductVersion LIKE '14.%' THEN 'SQL 2017'
			WHEN I.ProductVersion LIKE '15.%' THEN 'SQL 2019'
			ELSE I.ProductVersion END + ' ' + ISNULL(I.Edition + ' ','') + ISNULL(I.ProductLevel + ' ','') + ISNULL(I.ProductUpdateLevel,'') + ' (' + I.ProductVersion + ISNULL(', ' + I.ProductUpdateReference,'') + ')' AS SQLVersion
FROM dbo.Instances I
WHERE EXISTS(SELECT 1 FROM @Instances t WHERE t.InstanceID = I.InstanceID)
ORDER BY ProductVersion