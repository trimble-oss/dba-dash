CREATE FUNCTION dbo.CollectionErrorLogSearch(
	@InstanceID INT,
	@Days INT,
	@InstanceGroupName NVARCHAR(128),
	@ErrorSource VARCHAR(100),
	@ErrorContext VARCHAR(100),
	@ErrorMessage VARCHAR(200),
	@InstanceDisplayName NVARCHAR(128),
	@InstanceIDs IDs READONLY,
	@FromDate DATETIME2(2),
	@ToDate DATETIME2(2),
	@IncludeRetries BIT,
	@ErrorType VARCHAR(10),
	@ErrorStage VARCHAR(10)
)
RETURNS TABLE
AS
/*
	The error log rows for the DBA Dash ErrorLog (dbo.CollectionErrorLog_Get) - the same rows whether it lists them or
	counts them by instance or source, so a count always lists the rows it counts.  Parameters as CollectionErrorLog_Get.
	Callers should use OPTION(RECOMPILE) - the filters are optional.

	Errors that don't belong to an instance (alert processing, maintenance...) are shown as "(No instance)", and
	@InstanceDisplayName = '(No instance)' finds them.
*/
RETURN
SELECT	E.ErrorID,
		E.ErrorDate,
		E.InstanceID,
		I.Instance,
		ISNULL(I.InstanceDisplayName,N'(No instance)') AS InstanceDisplayName,
		E.ErrorSource,
		E.ErrorMessage,
		E.ErrorContext,
		C.ErrorType,
		C.ErrorStage
FROM dbo.CollectionErrorLog E
LEFT JOIN dbo.Instances I ON I.InstanceID = E.InstanceID
CROSS APPLY dbo.CollectionErrorClassify(E.ErrorSource,E.ErrorContext,E.ErrorMessage) C
WHERE (I.InstanceID = @InstanceID OR @InstanceID IS NULL)
AND (I.InstanceGroupName = @InstanceGroupName OR @InstanceGroupName IS NULL)
AND (ISNULL(I.InstanceDisplayName,N'(No instance)') = @InstanceDisplayName OR @InstanceDisplayName IS NULL)
AND E.ErrorDate>=ISNULL(@FromDate,DATEADD(d,-@Days,GETUTCDATE()))
AND (E.ErrorDate < @ToDate OR @ToDate IS NULL)
AND (@IncludeRetries = 1 OR ISNULL(E.ErrorContext,'') NOT LIKE '%[[]Retry%]')
AND (E.ErrorSource = @ErrorSource OR @ErrorSource IS NULL)
AND (E.ErrorMessage LIKE '%' + @ErrorMessage + '%' OR @ErrorMessage IS NULL)
AND (E.ErrorContext = @ErrorContext OR @ErrorContext IS NULL)
AND (C.ErrorType = @ErrorType OR @ErrorType IS NULL)
AND (C.ErrorStage = @ErrorStage OR @ErrorStage IS NULL)
-- Error doesn't belong to an Instance, or it belongs to one of the selected Instances, or no Instances are selected
AND (I.InstanceID IS NULL
	OR EXISTS(SELECT 1
			FROM @InstanceIDs T
			WHERE T.ID = I.InstanceID
			)
	OR NOT EXISTS(
			SELECT 1
			FROM @InstanceIDs T
			)
	)
