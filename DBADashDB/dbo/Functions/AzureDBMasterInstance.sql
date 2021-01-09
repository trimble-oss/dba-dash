CREATE FUNCTION dbo.AzureDBMasterInstance(@InstanceID INT)
RETURNS TABLE 
AS
RETURN 
SELECT CASE WHEN I.ConnectionID LIKE '%|master' THEN I.InstanceID ELSE  MI.InstanceID END AS MasterInstanceID
FROM dbo.Instances I
LEFT JOIN dbo.Instances MI ON MI.ConnectionID = I.Instance + '|master'
WHERE I.InstanceID = @InstanceID