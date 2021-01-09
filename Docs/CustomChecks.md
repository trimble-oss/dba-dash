
# Custom Checks
The DBADash summary dashboard is used to highlight issues with agent jobs, backups, corruption and other things that DBAs are typically interested in monitoring.  You might want to extend these monitoring capabilities to include some more bespoke tests and this is possible through the use of custom checks.  e.g. Application specific health tests, azure elastic jobs, SSRS subscriptions.  

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