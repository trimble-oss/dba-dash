
CREATE PROC [Report].[HostUpgrade](@InstanceIDs VARCHAR(MAX)=NULL)
AS
EXEC dbo.HostUpgradeHistory_Get @InstanceIDs = @InstanceIDs