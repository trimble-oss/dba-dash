CREATE PROC [Report].[DBAChecksErrorLog](@InstanceID INT=NULL,@Days INT=7)
AS
EXEC  [dbo].[CollectionErrorLog_Get] @InstanceID=@InstanceID,@Days=@Days