CREATE PROC dbo.DatabaseExtendedProperties_Get (
    @InstanceIDs IDs READONLY,
    @DatabaseID INT = NULL,
    @InstanceID INT = NULL
)
AS
SELECT  I.InstanceID,
        D.DatabaseID,
        I.InstanceDisplayName,
        D.name AS [Database],
        EP.Name AS [Property],
        EP.Value,
        EP.ValidFrom
FROM dbo.DatabaseExtendedProperties EP
JOIN dbo.Databases D ON D.DatabaseID = EP.DatabaseID
JOIN dbo.Instances I ON I.InstanceID = D.InstanceID
WHERE EXISTS (
            SELECT 1
            FROM @InstanceIDs t
            WHERE t.ID = EP.InstanceID
        )
AND (@DatabaseID <=0 OR @DatabaseID IS NULL OR EP.DatabaseID = @DatabaseID)
AND D.IsActive = 1
AND I.IsActive = 1
ORDER BY I.InstanceDisplayName, D.name, EP.Name