/*
	The card at the top of the Collection Health report and the DBA Dash ErrorLog for the instances where
	dbo.CollectionErrorSuggestion has something to suggest - one card for all of them, so it doesn't repeat on every row.
	InsightSeverity and Insight are the columns a report shows as cards (CustomReport.ShowInsights), and Insight is markdown:
	[text](url) is a link.  No row when there's nothing to suggest.

	@InstanceCount is the instances with a suggestion.  @Instances names them, or is NULL when the report is for one instance
	and there's no need to.  @WMISourceCount and @WMIErrorCount are the collections with WMI errors and the errors, for one instance.
*/
CREATE FUNCTION dbo.CollectionErrorInsight(
	@InstanceCount INT,
	@Instances NVARCHAR(MAX),
	@WMISourceCount INT,
	@WMIErrorCount INT
)
RETURNS TABLE
AS
RETURN
SELECT	'Warning' AS InsightSeverity,
		CASE WHEN @InstanceCount = 1
			THEN CONCAT(N'WMI failed for ',@WMISourceCount,N' collections (',@WMIErrorCount,N' errors)',N' on ' + @Instances,N'.  ',
						N'[Fix WMI access](https://dbadash.com/docs/help/wmi/), or set No WMI for this instance on the Source tab of the service config tool.')
			ELSE CONCAT(N'WMI failed for several collections on ',@InstanceCount,N' instances',N': ' + @Instances,N'.  ',
						N'[Fix WMI access](https://dbadash.com/docs/help/wmi/), or set No WMI for these instances on the Source tab of the service config tool.')
			END AS Insight
WHERE @InstanceCount > 0
