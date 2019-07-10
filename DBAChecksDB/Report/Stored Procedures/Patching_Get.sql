CREATE PROC Report.Patching_Get(@TagIDs NVARCHAR(MAX)='-1',@TagMatching VARCHAR(50)='ALL')
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

SELECT Instance, Edition,EngineEdition,BuildClrVersion,ProductMajorVersion,ProductUpdateLevel,ProductUpdateReference,ProductVersion
FROM dbo.Instances I
WHERE EXISTS(SELECT 1 FROM @Instances t WHERE t.InstanceID = I.InstanceID)
ORDER BY ProductVersion