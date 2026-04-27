CREATE PROC dbo.DatabaseExtendedPropertiesHistory_Get (
    @DatabaseID INT = NULL,
    @InstanceID INT = NULL,
	@Property SYSNAME = NULL
)
AS
SELECT	D.name AS [Database],
		EPH.Name AS Property,
		EPH.Value AS OldValue,
		EPH.NewValue AS NewValue,
		EPH.ValidFrom,
		EPH.ValidTo
FROM dbo.DatabaseExtendedPropertiesHistory EPH
JOIN dbo.Databases D ON EPH.DatabaseID = D.DatabaseID
WHERE EPH.InstanceID = @InstanceID
AND EPH.DatabaseID = @DatabaseID     
AND (EPH.Name = @Property OR @Property IS NULL)
AND D.IsActive = 1
ORDER BY ValidTo DESC
OPTION(RECOMPILE)
