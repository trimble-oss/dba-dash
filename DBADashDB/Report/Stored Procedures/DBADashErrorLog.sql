CREATE PROC [Report].[DBADashErrorLog](@InstanceID INT=NULL,@Days INT=7)
AS
EXEC  [dbo].[CollectionErrorLog_Get] @InstanceID=@InstanceID,@Days=@Days