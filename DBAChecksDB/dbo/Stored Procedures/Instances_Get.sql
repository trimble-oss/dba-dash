CREATE PROC [Instances_Get]
AS
SELECT I.InstanceID,I.Instance
FROM dbo.Instances I
WHERE I.IsActive=1
ORDER BY I.Instance