CREATE FUNCTION dbo.CollectionErrorClassify(
	@ErrorSource VARCHAR(100),
	@ErrorContext VARCHAR(100),
	@ErrorMessage NVARCHAR(MAX)
)
RETURNS TABLE
AS
/*
	Splits an error log row's context into what kind of error it is and where it happened, for the DBA Dash ErrorLog
	summary (dbo.CollectionErrorLog_Get with @GroupBy) and its filters.

	ErrorType:
		Retry	- from an attempt that was retried ("Collect[Retry]", "Import[Retry]").  The collection carried on.
		Warning	- "[Warning]" in the context, or a message that starts with "Warning".  Same rule as the service's
				  CollectionRunsTable.GetStatus, so a warning here is a warning in Collection Health.
		Error	- everything else

	ErrorStage:
		WMI		- collecting through WMI ("Collect:WMI", "Collect:Win32_OperatingSystem WMI"...)
		Collect	- collecting from the instance ("Collect", "Collect:InstanceMetadata", schema snapshots - their context is the database)
		Import	- importing into the repository ("Import", "Import[Existing]"...)
		Other	- the service's own work: alert processing, maintenance, custom SQL alerts...
*/
RETURN
SELECT	CASE	WHEN @ErrorContext LIKE '%[[]Retry%]' THEN 'Retry'
				WHEN @ErrorContext LIKE '%[[]Warning]%' OR @ErrorMessage LIKE N'Warning%' THEN 'Warning'
				ELSE 'Error' END AS ErrorType,
		CASE	WHEN @ErrorContext LIKE 'Import%' THEN 'Import'
				WHEN @ErrorContext LIKE 'Collect%WMI%' THEN 'WMI'
				WHEN @ErrorContext LIKE 'Collect%' OR @ErrorSource = 'SchemaSnapshot' THEN 'Collect'
				ELSE 'Other' END AS ErrorStage
