CREATE PROC [Report].[ConfigurationHistory_Get](@InstanceIDs VARCHAR(MAX)=NULL,@FromDate DATETIME2=NULL,@ToDate DATETIME2=NULL)
AS
EXEC dbo.SysConfigHistory_Get
	@InstanceIDs=@InstanceIDs,
	@FromDate=@FromDate,
	@ToDate=@ToDate