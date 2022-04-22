CREATE PROC dbo.CollectionErrorLog_Get(
	@InstanceID INT=NULL,
	@Days INT=7,
	@InstanceGroupName NVARCHAR(128)=NULL,
	@ErrorSource VARCHAR(100)=NULL,
	@ErrorContext VARCHAR(100)=NULL,
	@ErrorMessage VARCHAR(200)=NULL,
	@InstanceDisplayName NVARCHAR(128)=NULL
)
AS
SELECT I.Instance,
	I.InstanceDisplayName, 
	E.ErrorDate,
	E.InstanceID,
	E.ErrorSource,
	E.ErrorMessage,
	E.ErrorContext
FROM dbo.CollectionErrorLog E
LEFT JOIN dbo.Instances I ON I.InstanceID = E.InstanceID
WHERE (I.InstanceID = @InstanceID OR @InstanceID IS NULL)
AND (I.InstanceGroupName = @InstanceGroupName OR @InstanceGroupName IS NULL)
AND (I.InstanceDisplayName = @InstanceDisplayName OR @InstanceDisplayName IS NULL)
AND E.ErrorDate>=DATEADD(d,-@Days,GETUTCDATE())
AND (E.ErrorSource = @ErrorSource OR @ErrorSource IS NULL)
AND (E.ErrorMessage LIKE '%' + @ErrorMessage + '%' OR @ErrorMessage IS NULL)
AND (E.ErrorContext = @ErrorContext OR @ErrorContext IS NULL)
ORDER BY E.ErrorDate DESC
OPTION(RECOMPILE)