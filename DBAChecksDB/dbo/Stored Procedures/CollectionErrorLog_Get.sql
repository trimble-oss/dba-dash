CREATE PROC [dbo].[CollectionErrorLog_Get](@InstanceID INT=NULL,@Days INT=7)
AS
SELECT I.Instance, 
	E.ErrorDate,
	E.InstanceID,
	E.ErrorSource,
	E.ErrorMessage,
	ErrorContext
FROM dbo.CollectionErrorLog E
JOIN dbo.Instances I ON I.InstanceID = E.InstanceID
WHERE (I.InstanceID = @InstanceID OR @InstanceID IS NULL)
AND E.ErrorDate>=DATEADD(d,-@Days,GETUTCDATE())
ORDER BY E.ErrorDate DESC