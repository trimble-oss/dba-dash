CREATE PROC dbo.CollectionErrorLog_Get(
	@InstanceID INT=NULL,
	@Days INT=7,
	@InstanceGroupName NVARCHAR(128)=NULL,
	@ErrorSource VARCHAR(100)=NULL,
	@ErrorContext VARCHAR(100)=NULL,
	@ErrorMessage VARCHAR(200)=NULL,
	@InstanceDisplayName NVARCHAR(128)=NULL, /* '(No instance)' for the errors that don't belong to one */
	@InstanceIDs IDs READONLY,
	@FromDate DATETIME2(2)=NULL, /* UTC.  With @ToDate, an exact range in place of @Days */
	@ToDate DATETIME2(2)=NULL, /* UTC */
	@IncludeRetries BIT=1, /* 0 to leave out errors from attempts that were retried ([Retry] in the context) */
	@ErrorType VARCHAR(10)=NULL, /* Error, Warning or Retry - see dbo.CollectionErrorClassify */
	@ErrorStage VARCHAR(10)=NULL, /* Collect, WMI, Import or Other - see dbo.CollectionErrorClassify */
	@GroupBy VARCHAR(10)=NULL /* NULL or None: a row per error.  Instance or Source: counts, a row per instance or per error source */
)
AS
/*
	The DBA Dash ErrorLog report.  A blank filter is no filter - the report's text boxes send '' once cleared.

	Grouped, the context is split into columns rather than rows (dbo.CollectionErrorClassify):
		Errors/Warnings/Retries		- what kind of error.  These add up to ErrorCount.
		Collect/WMI/Import/Other	- where it happened.  These add up to ErrorCount too.
	NULL rather than 0, so what's there stands out.  LastMessage is the latest error's message (first 1000 characters), and
	LastMessageFull all of it, for the link that shows it.

	DrillGroupBy is for the report's instance link: from a row per instance to that instance's sources, and from a
	row per error to that instance's errors.

	What to do about the errors, where there's something to suggest (dbo.CollectionErrorSuggestion), is a card at the top of
	the report whatever the grouping (dbo.CollectionErrorInsight), and a Suggestion column on a row per instance, which says
	which instances it's for however many there are.

	Result sets
	0	The errors - a row per error, instance or source
	1	Insights - a card at the top of the report for each row (CustomReport.ShowInsights)
*/
SET @ErrorSource = NULLIF(@ErrorSource,'')
SET @ErrorContext = NULLIF(@ErrorContext,'')
SET @ErrorMessage = NULLIF(@ErrorMessage,'')
SET @InstanceDisplayName = NULLIF(@InstanceDisplayName,N'')
SET @ErrorType = NULLIF(@ErrorType,'')
SET @ErrorStage = NULLIF(@ErrorStage,'')
SET @GroupBy = NULLIF(NULLIF(@GroupBy,''),'None')

IF @GroupBy NOT IN('Instance','Source')
BEGIN
	RAISERROR('@GroupBy should be Instance, Source or None',11,1);
	RETURN;
END;

/* GroupKey is the instance or the source - whichever the rows are per, NULL for a row per error.  ErrorMessage is for a row
   per error, and LastMessage the group's latest message (first 1000 characters) on one of its rows. */
CREATE TABLE #Errors(
	ErrorID INT NOT NULL,
	GroupKey NVARCHAR(128) COLLATE DATABASE_DEFAULT NULL,
	Instance NVARCHAR(128) COLLATE DATABASE_DEFAULT NULL,
	InstanceDisplayName NVARCHAR(128) COLLATE DATABASE_DEFAULT NOT NULL,
	InstanceID INT NULL,
	ErrorSource VARCHAR(100) COLLATE DATABASE_DEFAULT NOT NULL,
	ErrorContext VARCHAR(100) COLLATE DATABASE_DEFAULT NULL,
	ErrorDate DATETIME2(2) NOT NULL,
	ErrorType VARCHAR(10) COLLATE DATABASE_DEFAULT NOT NULL,
	ErrorStage VARCHAR(10) COLLATE DATABASE_DEFAULT NOT NULL,
	ErrorMessage NVARCHAR(MAX) COLLATE DATABASE_DEFAULT NULL,
	LastMessage NVARCHAR(1000) COLLATE DATABASE_DEFAULT NULL
);

INSERT INTO #Errors(ErrorID,GroupKey,Instance,InstanceDisplayName,InstanceID,ErrorSource,ErrorContext,ErrorDate,ErrorType,ErrorStage,ErrorMessage,LastMessage)
SELECT	T.ErrorID,
		T.GroupKey,
		T.Instance,
		T.InstanceDisplayName,
		T.InstanceID,
		T.ErrorSource,
		T.ErrorContext,
		T.ErrorDate,
		T.ErrorType,
		T.ErrorStage,
		CASE WHEN @GroupBy IS NULL THEN T.ErrorMessage END,
		CASE WHEN @GroupBy IS NOT NULL AND ROW_NUMBER() OVER(PARTITION BY T.GroupKey ORDER BY T.ErrorDate DESC, T.ErrorID DESC) = 1 THEN LEFT(T.ErrorMessage,1000) END
FROM (	SELECT	E.*,
				CASE WHEN @GroupBy = 'Instance' THEN E.InstanceDisplayName WHEN @GroupBy = 'Source' THEN E.ErrorSource END AS GroupKey
		FROM dbo.CollectionErrorLogSearch(@InstanceID,@Days,@InstanceGroupName,@ErrorSource,@ErrorContext,@ErrorMessage,@InstanceDisplayName,
										@InstanceIDs,@FromDate,@ToDate,@IncludeRetries,@ErrorType,@ErrorStage) E
		) T
OPTION(RECOMPILE);

/* What to do about each instance's errors, where there's something to suggest (dbo.CollectionErrorSuggestion) */
SELECT	W.InstanceDisplayName,
		W.WMISourceCount,
		W.WMIErrorCount,
		S.Suggestion,
		S.HelpUrl
INTO #Suggestions
FROM (	SELECT	InstanceDisplayName,
				COUNT(DISTINCT ErrorSource) AS WMISourceCount,
				COUNT(*) AS WMIErrorCount
		FROM #Errors
		WHERE ErrorStage = 'WMI'
		GROUP BY InstanceDisplayName
		) W
CROSS APPLY dbo.CollectionErrorSuggestion(W.WMISourceCount,W.WMIErrorCount) S
WHERE S.Suggestion IS NOT NULL;

IF @GroupBy IS NULL
BEGIN
	SELECT	E.Instance,
			E.InstanceDisplayName,
			E.ErrorDate,
			E.InstanceID,
			E.ErrorSource,
			E.ErrorContext,
			E.ErrorType,
			E.ErrorStage,
			E.ErrorMessage,
			'None' AS DrillGroupBy
	FROM #Errors E
	ORDER BY E.ErrorDate DESC, E.ErrorID DESC;
END
ELSE
BEGIN
	SELECT	E.GroupKey,
			COUNT(DISTINCT E.InstanceDisplayName) AS InstanceCount,
			COUNT(DISTINCT E.ErrorSource) AS SourceCount,
			COUNT(*) AS ErrorCount,
			NULLIF(COUNT(CASE WHEN E.ErrorType = 'Error' THEN 1 END),0) AS Errors,
			NULLIF(COUNT(CASE WHEN E.ErrorType = 'Warning' THEN 1 END),0) AS Warnings,
			NULLIF(COUNT(CASE WHEN E.ErrorType = 'Retry' THEN 1 END),0) AS Retries,
			NULLIF(COUNT(CASE WHEN E.ErrorStage = 'Collect' THEN 1 END),0) AS Collect,
			NULLIF(COUNT(CASE WHEN E.ErrorStage = 'WMI' THEN 1 END),0) AS WMI,
			NULLIF(COUNT(CASE WHEN E.ErrorStage = 'Import' THEN 1 END),0) AS Import,
			NULLIF(COUNT(CASE WHEN E.ErrorStage = 'Other' THEN 1 END),0) AS Other,
			MIN(E.ErrorDate) AS FirstError,
			MAX(E.ErrorDate) AS LastError,
			MAX(E.LastMessage) AS LastMessage,
			MAX(CASE WHEN E.LastMessage IS NOT NULL THEN E.ErrorID END) AS LastErrorID
	INTO #Groups
	FROM #Errors E
	GROUP BY E.GroupKey;

	/* A row per instance has a Sources count and the instance's suggestion.  A row per source has an Instances count. */
	IF @GroupBy = 'Instance'
	BEGIN
		SELECT	G.GroupKey AS InstanceDisplayName,
				G.SourceCount,
				G.ErrorCount,G.Errors,G.Warnings,G.Retries,G.Collect,G.WMI,G.Import,G.Other,G.FirstError,G.LastError,
				S.Suggestion,
				S.HelpUrl,
				G.LastMessage,
				L.ErrorMessage AS LastMessageFull,
				'Source' AS DrillGroupBy
		FROM #Groups G
		LEFT JOIN #Suggestions S ON S.InstanceDisplayName = G.GroupKey
		LEFT JOIN dbo.CollectionErrorLog L ON L.ErrorID = G.LastErrorID
		ORDER BY G.Errors DESC, G.ErrorCount DESC;
	END
	ELSE
	BEGIN
		SELECT	G.GroupKey AS ErrorSource,
				G.InstanceCount,
				G.ErrorCount,G.Errors,G.Warnings,G.Retries,G.Collect,G.WMI,G.Import,G.Other,G.FirstError,G.LastError,
				G.LastMessage,
				L.ErrorMessage AS LastMessageFull
		FROM #Groups G
		LEFT JOIN dbo.CollectionErrorLog L ON L.ErrorID = G.LastErrorID
		ORDER BY G.Errors DESC, G.ErrorCount DESC;
	END
END

/* Insights: what to do about the instances' errors, a card at the top of the report.  The instances are named where the
   errors are from more than one - up to 5, so the card doesn't grow with the number of instances. */
DECLARE @InsightInstances NVARCHAR(MAX)
IF (SELECT COUNT(DISTINCT InstanceDisplayName) FROM #Errors) > 1
BEGIN
	SELECT @InsightInstances = STUFF((	SELECT TOP(5) N', ' + InstanceDisplayName
										FROM #Suggestions
										ORDER BY InstanceDisplayName
										FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,2,N'')
			+ CASE WHEN (SELECT COUNT(*) FROM #Suggestions) > 5 THEN CONCAT(N' and ',(SELECT COUNT(*) FROM #Suggestions) - 5,N' more') ELSE N'' END
END

SELECT	I.InsightSeverity,
		I.Insight
FROM (	SELECT	COUNT(*) AS InstanceCount,
				MAX(WMISourceCount) AS WMISourceCount,
				MAX(WMIErrorCount) AS WMIErrorCount
		FROM #Suggestions
		) S
CROSS APPLY dbo.CollectionErrorInsight(S.InstanceCount,@InsightInstances,S.WMISourceCount,S.WMIErrorCount) I
