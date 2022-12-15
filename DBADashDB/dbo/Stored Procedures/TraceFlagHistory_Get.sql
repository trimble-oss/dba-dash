CREATE PROC dbo.TraceFlagHistory_Get(
	@InstanceIDs VARCHAR(MAX)=NULL,
	@ShowHidden BIT=1
)
AS
DECLARE @Instances TABLE(
	InstanceID INT PRIMARY KEY
)
IF @InstanceIDs IS NULL
BEGIN
	INSERT INTO @Instances
	(
	    InstanceID
	)
	SELECT InstanceID 
	FROM dbo.Instances 
	WHERE IsActive=1
END 
ELSE 
BEGIN
	INSERT INTO @Instances
	(
		InstanceID
	)
	SELECT value
	FROM STRING_SPLIT(@InstanceIDs,',')
END;

WITH T AS (
	SELECT	InstanceID,
			TraceFlag,
			ValidFrom AS ChangeDate, 
			'Added' Change
	FROM dbo.TraceFlags 
	WHERE ValidFrom >'1900-01-01 00:00:00.00'
	UNION ALL
	SELECT	InstanceID,
			TraceFlag,
			ValidFrom,
			'Added'
	FROM dbo.TraceFlagHistory
	WHERE ValidFrom >'1900-01-01 00:00:00.00'
	UNION ALL
	SELECT	InstanceID,
			TraceFlag,
			ValidTo,
			'Removed'
	FROM dbo.TraceFlagHistory
)
SELECT I.ConnectionID,
	   T.InstanceID,
	   I.InstanceDisplayName,
       T.TraceFlag,
       T.ChangeDate,
       T.Change
FROM T 
JOIN dbo.Instances I ON I.InstanceID = T.InstanceID
WHERE EXISTS(	SELECT 1 
				FROM @Instances I 
				WHERE I.InstanceID = T.InstanceID
			)
AND (I.ShowInSummary=1 OR @ShowHidden=1)
ORDER BY T.ChangeDate DESC