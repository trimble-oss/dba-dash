CREATE PROC [Report].[TraceFlags_Get](@InstanceIDs VARCHAR(MAX)=NULL)
AS
EXEC dbo.TraceFlags_Get @InstanceIDs = @InstanceIDs