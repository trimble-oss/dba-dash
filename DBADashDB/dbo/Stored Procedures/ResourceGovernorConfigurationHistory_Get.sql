CREATE PROC dbo.ResourceGovernorConfigurationHistory_Get(
	@InstanceID INT
)
AS
SELECT RG.InstanceID,
	I.Instance, 
	I.InstanceDisplayName,
	RG.is_enabled,
	RG.classifier_function,
	RG.reconfiguration_error,
	RG.reconfiguration_pending,
	RG.max_outstanding_io_per_volume,
	RG.script,
	RG.ValidFrom,
	RG.ValidTo,
	FIRST_VALUE(script) OVER(ORDER BY ValidTo ROWS BETWEEN 1 PRECEDING AND 1 PRECEDING) script_previous
FROM dbo.ResourceGovernorConfigurationHistory RG 
JOIN dbo.Instances I ON RG.InstanceID = I.InstanceID
WHERE RG.InstanceID = @InstanceID
ORDER BY RG.ValidTo DESC
