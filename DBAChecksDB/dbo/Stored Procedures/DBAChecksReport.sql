CREATE PROC [dbo].[DBAChecksReport]
AS
EXEC DBAChecksSummary_Get
EXEC dbo.DriveReport_Get @FilterLevel=2
EXEC dbo.BackupReport_Get @FilterLevel=2
EXEC dbo.LogshippingReport_Get @FilterLevel=2
EXEC dbo.AgentJobReport_Get
