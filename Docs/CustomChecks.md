# Custom Checks
The DBA Dash summary dashboard is used to highlight issues with agent jobs, backups, corruption and other things that DBAs are typically interested in monitoring.  You might want to extend these monitoring capabilities to include some more bespoke tests and this is possible through the use of custom checks.  e.g. Application specific health tests, azure elastic jobs, SSRS subscriptions.  

To include your own custom checks you need to create a stored procedure called "DBADash_CustomCheck" in the database that the agent connects to.  If the stored procedure exists, the agent will run the stored procedure and collect the results of your custom checks.  The results of the SP need to be in the format that the agent expects - use the template stored procedure below as a staring point.  

```SQL
/*
	DBADash Custom Check template
	Replace "Edit Here" section to run your own custom checks
*/
CREATE PROC [dbo].[DBADash_CustomCheck]
AS
-- Table variable used to ensure the output is in the required format
DECLARE @CustomChecks TABLE(
	Test NVARCHAR(128) NOT NULL,
	Context NVARCHAR(128) NOT NULL,
	Status TINYINT NOT NULL,
	Info NVARCHAR(MAX) NULL,
	PRIMARY KEY(Test,Context),
	CHECK (Status IN(1,2,3,4))
)

/****************** Edit Here *********************/
/* 
	Run your own tests and insert the results into @CustomChecks table.
	Test = Name of your custom test
	Context = Context where the test applies to.  e.g. application name, job name, database name, server name etc.  
	Status =   1=Critical (Red), 2=Warning (Yellow), 3= N/A (Grey), 4=OK (Green)
	Info = Any additional information you would like to include
*/

INSERT INTO @CustomChecks(Test,Context,Status,Info)
SELECT 'App test','App1',1,'Failed'
UNION ALL
SELECT 'App test','App2',2,'Warning'
UNION ALL
SELECT 'DB Test','DB1',3,'N/A'
UNION ALL
SELECT 'DB Test','DB2',4,'OK'

/***************************************************/

-- return data in required format
SELECT Test,
	Context,
	Status,
	Info
FROM @CustomChecks
```
By default the custom check stored procedure will be run as part of the general data collection which runs every 1hr.

## Example - Azure Elastic Agent Jobs
If you use elastic agent jobs in azure you could use the custom checks to add some basic monitoring for your elastic jobs.  Here is an example stored procedure that you could create in your job database.  Edit the script as required:

```
CREATE PROC [dbo].[DBADash_CustomCheck]
AS
WITH T AS (
SELECT job_name,
	SUM(CASE WHEN lifecycle='Failed' THEN 1 ELSE 0 END) AS FailCount,
	SUM(CASE WHEN lifecycle='Succeeded' THEN 1 ELSE 0 END) AS SucceededCount,
	MAX(CASE WHEN lifecycle='Failed' THEN end_time ELSE NULL END) AS LastFail,
	MAX(CASE WHEN lifecycle='Succeeded' THEN end_time ELSE NULL END) AS LastSucceeded,
	MAX(CASE WHEN lifecycle='WaitingForChildJobExecutions' THEN 'Y' ELSE 'N' END) AS IsInProgress
FROM jobs.job_executions
WHERE step_id IS NULL
AND (end_time>=DATEADD(d,-7,GETUTCDATE()) OR end_time IS NULL)
GROUP BY job_name
)
SELECT 'ElasticJobStatus' AS Test, 
	job_name AS Context,
	CASE WHEN T.LastFail IS NULL THEN 4 WHEN LastFail>ISNULL(LastSucceeded,'19000101') THEN 1 ELSE 2 END AS Status,
	CONCAT('Fail Count 7 Days:',FailCount,', Succeed Count 7 Days:',SucceededCount, ', Last Fail: ', FORMAT(T.LastFail,'yyyy-MM-dd HH:mm'), ', Last Succeeded: ', T.LastSucceeded, ', Job In Progress:', T.IsInProgress) AS Info
FROM T 
GO
```

