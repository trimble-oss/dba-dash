CREATE PROC [dbo].[Instances_Get]
AS
SELECT I.InstanceID,I.ConnectionID,I.Instance
FROM dbo.Instances I
WHERE I.IsActive=1
ORDER BY I.Instance