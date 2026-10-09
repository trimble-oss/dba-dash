CREATE PROC dbo.AvailabilityGroupHealthEvents_Get(
	@InstanceID INT,
	@FromDate DATETIME2(3)=NULL, /* UTC.  NULL for the last 30 days */
	@ToDate DATETIME2(3)=NULL /* UTC.  NULL for now */
)
AS
/* AlwaysOn_health events collected from an instance */
SET NOCOUNT ON
SELECT	HE.EventTime AS [Event Time],
		HE.EventName AS [Event],
		HE.AvailabilityGroupName AS [Availability Group],
		HE.ReplicaName AS [Replica],
		HE.DatabaseName AS [Database],
		HE.Component,
		HE.PreviousState AS [Previous State],
		HE.CurrentState AS [Current State],
		HE.DDLAction AS [DDL Action],
		HE.DDLPhase AS [DDL Phase],
		HE.ErrorNumber AS [Error Number],
		HE.Details
FROM dbo.AvailabilityGroupHealthEvents HE
WHERE HE.InstanceID = @InstanceID
AND HE.EventTime >= ISNULL(@FromDate,DATEADD(d,-30,GETUTCDATE()))
AND HE.EventTime < ISNULL(@ToDate,'99991231')
ORDER BY HE.EventTime DESC
