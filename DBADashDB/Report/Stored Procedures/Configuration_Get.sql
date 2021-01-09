CREATE PROC [Report].[Configuration_Get](@InstanceIDs VARCHAR(MAX)=NULL)
AS
EXEC dbo.Configuration_Get @InstanceIDs = @InstanceIDs