CREATE PROC Report.AlertsConfig_Get(
	@InstanceIDs VARCHAR(MAX) = NULL
)
AS
EXEC dbo.AlertsConfig_Get @InstanceIDs = @InstanceIDs