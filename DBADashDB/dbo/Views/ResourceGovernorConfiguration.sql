CREATE VIEW dbo.ResourceGovernorConfiguration
AS
SELECT InstanceID,
       is_enabled,
       classifier_function,
       reconfiguration_error,
       reconfiguration_pending,
       max_outstanding_io_per_volume,
       script,
       ValidFrom,
       ValidTo 
FROM dbo.ResourceGovernorConfigurationHistory
WHERE ValidTo = '9999-12-31'
GO