CREATE PROC [Report].[SQLPatching_Get](@InstanceIDs VARCHAR(MAX)=NULL)
AS
EXEC dbo.SQLPatching_Get @InstanceIDs = @InstanceIDs