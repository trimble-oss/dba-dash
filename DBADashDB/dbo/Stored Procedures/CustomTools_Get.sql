CREATE PROC dbo.CustomTools_Get(
	@InstanceID INT
)
AS
DECLARE @Allowed NVARCHAR(MAX)
SELECT @Allowed = CA.AllowedCustomProcs
FROM dbo.Instances I
JOIN dbo.DBADashAgent CA ON I.CollectAgentID = CA.DBADashAgentID
WHERE InstanceID = @InstanceID
AND CA.MessagingEnabled=1

SELECT schema_name,
		object_name,
		parameters,
		MetaData
FROM dbo.AvailableProcs AP
LEFT JOIN dbo.CustomReport CR ON CR.SchemaName = AP.schema_name 
							AND CR.ProcedureName = AP.object_name
							AND CR.Type = 'DirectExecutionReport'
WHERE EXISTS(SELECT 1 
		FROM STRING_SPLIT(@Allowed,',') SS
		WHERE PARSENAME(SS.value,1)=object_name
		AND ISNULL(PARSENAME(SS.value,2),'dbo') = schema_name
		)
AND InstanceID = @InstanceID