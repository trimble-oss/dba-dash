CREATE PROC [dbo].[DatabaseExtendedProperties_Get] (
    @InstanceIDs VARCHAR(MAX) = NULL,
    @DatabaseID INT = -1
)
AS
SELECT EP.Name AS [Property],
       EP.Value,
       EP.ValidFrom
FROM dbo.DatabaseExtendedProperties EP
JOIN dbo.Databases D ON D.DatabaseID = EP.DatabaseID
WHERE (
        @InstanceIDs IS NULL
        OR EXISTS (
            SELECT 1
            FROM STRING_SPLIT(@InstanceIDs, ',') s
            WHERE CAST(s.value AS INT) = EP.InstanceID
        )
      )
AND (@DatabaseID = -1 OR EP.DatabaseID = @DatabaseID)
AND D.IsActive = 1
ORDER BY EP.Name
