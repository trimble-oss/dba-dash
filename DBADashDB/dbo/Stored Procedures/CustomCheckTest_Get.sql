
CREATE PROC dbo.CustomCheckTest_Get(@InstanceIDs VARCHAR(MAX)=NULL)
AS
SELECT DISTINCT Test 
FROM dbo.CustomChecks cc
WHERE (EXISTS(SELECT 1 
			FROM STRING_SPLIT(@InstanceIDs,',') ss 
			WHERE ss.Value = cc.InstanceID)
			OR @InstanceIDs IS NULL)
ORDER BY Test