# DBAChecks
## Project Summary
DBAChecks is a tool for SQL Server DBAs to assist with daily checks, performance monitoring and change tracking.

 - Backups, Log Shipping, Agent Jobs, DBCC, Corruption, Drive space, AGs
 - IO Performance, CPU, Waits, Blocking
 - Stored Procedure/Function/Trigger execution stats
 - Capture slow queries (Extended Event trace)
 - Azure DB monitoring
 - Track changes to configuration, SQL Patching, drivers etc.
 - Schema change tracking
 - Option to monitor instances in isolated environments via S3 bucket.

## Requirements
 
 - SQL Server 2016 SP1 or later required for DBAChecks repository database.
 - SQL 2005*-SQL 2019 & Azure DB supported for monitored instances.
 - Windows machine to run agent.  Agent can monitor multiple SQL instances.
 - 
## Installation
 - Extract the application binaries on the server where you want to run the agent. 
	 *Ideally it's best NOT to run the agent on your production SQL Server to limit the impact of monitoring your SQL instance as 			much as possible*
 - The first thing we need to do is create the database that will be used as the central repository for your SQL instances.  Run the DBAChecksServiceConfig.exe tool.  Click the "Deploy/Update Database" button.  
 - A connection dialog is shown - use this to connect to the SQL Instance that you want to deploy the central repository database to.
 - The DB Deploy dialog is shown. The default database name is DBAChecksDB - click deploy to create the database.
 - It might take a few moments to create the database.  Click "OK" when you see the "Deploy succeeded" dialog.
 - The next step is to add databases that we want to monitor.  Click the "Source" tab.
 - Click the button to the right of the "Source" to connect to the SQL Instance you want to monitor.  Alternatively, the connection string can be entered manually.  
	 *Note: Connection strings are encrypted to avoid storing them in plain text but it is recommended to use Windows authentication*
 - Click "Add/Update" to add the connection.  Repeat as necessary to add the other SQL Instances you want to monitor.
 *Note: Additional options are available for your source connections.  Capture slow queries, take schema snapshots and customize schedules.*
 - Click "Save".  A "ServiceConfig.json" file is created in the application folder that stores the configuration details for this agent.
 - At this stage you might want to run "DBAChecksService.exe".  This runs the agent as a console application and you can monitor what the application is doing. 
  *On startup and every 1hr on the hour it will collect "General" information from your SQL instances.  e.g. Backups, Drive space, Agent jobs etc.*
  *Every 1min it will collect performance data.  e.g. CPU, IO, Waits etc.*
 - You will most likely want to install the agent as a Windows service.  Close the DBAChecksService.exe application and go back to the DBAChecksServiceConfig.exe application.  Click the service tab.
 - Click "Install".  Enter the credentials you want to use to run the service as and click "OK".
 - Close the command window.
 - The service should now be installed and you can click the "Start" button to start the service.
 - Installation is now complete.  You can run the "DBAChecksGUI" application to get started using DBAChecks.  

**Note:**
More advanced service configuration is possible.  e.g. A remote agent can be configured to write to a S3 bucket and another agent that connects to your repository database can use the S3 bucket as a source instead of a SQL connection string.  
