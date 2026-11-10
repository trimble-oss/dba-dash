/*
	What to do about an instance's collection errors, where there's something to suggest - shared by the DBA Dash ErrorLog
	(dbo.CollectionErrorLog_Get) and the Collection Health report (dbo.CollectionHealth_Get) so they say the same thing.
	The reports show it as a card at the top (dbo.CollectionErrorInsight), and the ErrorLog's row per instance as a column too.

	WMI: errors through WMI (dbo.CollectionErrorClassify's WMI stage) from 2 or more collections suggest WMI isn't working for
	the instance at all, rather than one query failing - WinRM, the firewall or permissions.  Either fix that or stop using
	WMI for the instance (No WMI), which collects drives through SQL instead and leaves out what only WMI collects.

	@WMISourceCount is the collections (error sources) with WMI errors, @WMIErrorCount the errors.  NULL where there's
	nothing to suggest.  HelpUrl is for the link.
*/
CREATE FUNCTION dbo.CollectionErrorSuggestion(
	@WMISourceCount INT,
	@WMIErrorCount INT
)
RETURNS TABLE
AS
RETURN
SELECT	CASE WHEN @WMISourceCount >= 2
			THEN CONCAT('WMI failed for ',@WMISourceCount,' collections (',@WMIErrorCount,' errors).  Fix WMI access, or set No WMI for this instance on the Source tab of the service config tool.  Click for help.')
			END AS Suggestion,
		CASE WHEN @WMISourceCount >= 2 THEN 'https://dbadash.com/docs/help/wmi/' END AS HelpUrl
