CREATE PROC [Report].[DBAChecksAll]
AS
EXEC Report.Summary_Get
EXEC Report.Drives_Get
EXEC Report.Backups_Get
EXEC Report.Logshipping_Get
EXEC Report.AgentJobs_Get