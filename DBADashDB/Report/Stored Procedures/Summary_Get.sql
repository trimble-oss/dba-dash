CREATE PROC [Report].[Summary_Get](@InstanceIDs VARCHAR(MAX)=NULL)
AS
EXEC dbo.Summary_Get @InstanceIDs = @InstanceIDs