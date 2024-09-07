CREATE PROC dbo.IdentityColumnThresholds_Get(
	@InstanceID INT,
	@DatabaseID INT,
	@object_name NVARCHAR(128)
)
AS
/* 
	Returns a row even if no configuration exists at the specified level - returning the inherited threshold.
	IsInherited returns true if the configuration is inherited.
*/
SELECT TOP(1) 
		ISNULL(I.InstanceGroupName,'{Root}') + ISNULL(' \ ' + D.name,'') + ISNULL(' \ ' + NULLIF(P.object_name,''),'') AS SelectedObject,
		T.PctUsedWarningThreshold,
		T.PctUsedCriticalThreshold,
		T.DaysWarningThreshold,
		T.DaysCriticalThreshold,
		CASE WHEN P.InstanceID = T.InstanceID 
					AND P.DatabaseID = T.DatabaseID 
					AND P.object_name = T.object_name
			THEN CAST(0 AS BIT) 
			WHEN P.InstanceID = -1 AND P.DatabaseID =-1 AND P.object_name ='' THEN CAST(0 AS BIT)
			ELSE CAST(1 AS BIT) 
			END AS IsInherited,
		CASE WHEN T.object_name <> '' THEN 'Table' WHEN T.DatabaseID <> -1 THEN 'Database' WHEN T.InstanceID <> -1 THEN 'Instance' ELSE 'Root' END as ConfiguredLevel
FROM (SELECT @InstanceID AS InstanceID, @DatabaseID DatabaseID, @object_name object_name) P
LEFT JOIN dbo.Instances I ON P.InstanceID = I.InstanceID
LEFT JOIN dbo.Databases D ON P.DatabaseID = D.DatabaseID AND D.InstanceID = P.InstanceID
LEFT JOIN dbo.IdentityColumnThresholds T ON(
										(T.InstanceID = P.InstanceID OR T.InstanceID = -1)
										AND (T.DatabaseID = P.DatabaseID OR T.DatabaseID = -1)
										AND (T.object_name = P.object_name OR T.object_name = '')
										)
ORDER BY T.InstanceID DESC, 
		T.DatabaseID DESC, 
		T.object_name DESC