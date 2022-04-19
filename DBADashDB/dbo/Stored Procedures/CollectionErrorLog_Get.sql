CREATE PROC dbo.CollectionErrorLog_Get(
	@InstanceID INT=NULL,
	@Days INT=7,
	@InstanceGroupName NVARCHAR(128)=NULL
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
AND E.ErrorDate>=DATEADD(d,-@Days,GETUTCDATE())
ORDER BY E.ErrorDate DESC