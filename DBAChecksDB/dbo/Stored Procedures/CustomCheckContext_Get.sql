CREATE PROC dbo.CustomCheckContext_Get(@InstanceIDs VARCHAR(MAX)=NULL)
AS
SELECT DISTINCT Context
FROM dbo.CustomChecks cc
WHERE (EXISTS(SELECT 1 
			FROM STRING_SPLIT(@InstanceIDs,',') ss 
			WHERE ss.Value = cc.InstanceID)
			OR @InstanceIDs IS NULL)
ORDER BY cc.Context