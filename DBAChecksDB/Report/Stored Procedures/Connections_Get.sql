
CREATE PROC [Report].[Connections_Get](@TagIDs NVARCHAR(MAX)='-1',@TagMatching VARCHAR(50)='ALL',@InstanceName SYSNAME=NULL)
AS
DECLARE @TagCount INT
DECLARE @tTags TABLE(
	TagID INT PRIMARY KEY
)
INSERT INTO @tTags
(
    TagID
)
SELECT T.TagID
FROM dbo.Tags T 
WHERE EXISTS(SELECT 1 FROM dbo.SplitStrings(@TagIDs,',') SS WHERE CAST(SS.Item AS INT) = T.TagID)

SET @TagCount=@@ROWCOUNT;

SELECT I.InstanceID,I.ConnectionID, I.EditionID
FROM dbo.Instances I
LEFT JOIN dbo.InstanceTag IT ON IT.InstanceID = I.InstanceID
			AND EXISTS(SELECT 1 FROM @tTags T WHERE T.TagID = IT.TagID)
WHERE I.IsActive=1
AND (I.Instance LIKE + '%' + @InstanceName + '%' OR @InstanceName IS NULL)
GROUP BY I.InstanceID,I.ConnectionID,I.EditionID
HAVING (COUNT(IT.InstanceID) = @TagCount AND @TagMatching='ALL')
OR (COUNT(IT.InstanceID)>0 AND @TagMatching='ANY')
OR (COUNT(IT.InstanceID)=0 AND @TagMatching='EXCLUDEANY')
OR (COUNT(IT.InstanceID)<>@TagCount AND @TagMatching='EXCLUDEALL')
ORDER BY I.ConnectionID