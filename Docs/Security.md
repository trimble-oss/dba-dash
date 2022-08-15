# DBA Dash Security

## Service Account
The service account used for DBADash should be a member of the **sysadmin** role on the SQL instance and also be a member of  the **local "Administrators" group**.  It's possible to configure a user with a lower level of access but this might prevent DBA Dash from collecting certain information about your SQL instances.

### Why local admin?  
This is required to run WMI queries (optional).  These are used to collect drive space, driver info and o/s info.  If you don't want the tool to use WMI, select the "**Don't use WMI**" checkbox when adding an instance in the DBA Dash Service Config Tool. If you don't check this box, WMI collection will be attempted - resulting in a logged error if the user doesn't have access.  If drive space isn't collected via WMI it will be collected through SQL instead - but only for drives that contain SQL files. You could provision the required WMI access to your service account.  

### Why SysAdmin?

If the tool doesn't run as sysadmin it won't be able to collect last good CHECKDB date as well as some other data from the registry like processor name, system manufacturer and model.  Sysadmin can be granted using:

````SQL
ALTER SERVER ROLE [sysadmin] ADD MEMBER [{LoginName}]
````

### Firewall

DBA Dash collects most of it's data via a SQL connection (Typically port 1433).  If you check the "No WMI" box when adding a connection then ALL data will be collected via the SQL connection and no additional firewall configuration would be required.  

It can be useful to have WMI collection enabled though as this allows you to collect some extra data like drive capacity and free space for all drives.

These firewall rules can be used to allow WMI:

* Windows Management Instrumentation (ASync-In)
* Windows Management Instrumentation (DCOM-In)
* Windows Management Instrumentation (WMI-In)

If you want to check if WMI is working, you can run this powershell script to test:
```Powershell
Get-WmiObject -Class Win32_computerSystem -ComputerName "YOUR_COMPUTER_NAME"
```

As part of the DriversWMI collection DBA Dash will also attempt to get the AWS PV driver version by reading this registry key: SOFTWARE\Amazon\PVDriver. This requires a different firewall rule: 
* File and Printer Sharing (SMB-In)

This powerhell query provides a quick way to test if it's working (it should run without errors):
```Powershell
[Microsoft.Win32.RegistryKey]::OpenRemoteBaseKey("LocalMachine", "YOUR_COMPUTER_NAME")
```
It's also possible to disable the "DriversWMI" collection in the service configuration tool if you are not interested in collecting driver versions for your SQL instances.

## Running with Minimal Permissions

If you **don't** want to grant sysadmin access, you can assign the permissions listed below instead

**Server Level Permissions:**
* View Server State
* View Any Database
* Connect Any Database
* Alter Event Session (For Slow Query trace if used)
* View Any Definition

**MSDB Database:**
* Add user to the *db_datareader* role.
* Add user to the *SQLAgentReaderRole* role

This script can be used to provision the required permissions:
````SQL
/*
	Use this script to configure permissions for the DBA Dash service account if you don't want to use the sysadmin server role.
	DBA Dash can collect more data when running as sysadmin but most features and functionallity will work with a more limited account
	See here for details: https://github.com/trimble-oss/dba-dash/edit/main/Docs/Security.md

	On the destination connection the service will need to be a member of db_owner role on the repository database
	To allow the service to create the repository database you can use:
	GRANT CREATE ANY DATABASE TO {LoginName}
*/
DECLARE @LoginName SYSNAME = 'DBADashService' /* !!!! Replace with your own service login !!!! */
DECLARE @SQL NVARCHAR(MAX)
SET @SQL = N'
GRANT VIEW SERVER STATE TO ' + QUOTENAME(@LoginName) + '
GRANT VIEW ANY DATABASE TO ' + QUOTENAME(@LoginName) + '
GRANT CONNECT ANY DATABASE TO ' + QUOTENAME(@LoginName) + '
GRANT VIEW ANY DEFINITION TO ' + QUOTENAME(@LoginName) + '
GRANT ALTER ANY EVENT SESSION TO ' + QUOTENAME(@LoginName) + ' /* Required if you want to use slow query capture */
USE [msdb]
IF NOT EXISTS(SELECT * 
			FROM msdb.sys.database_principals
			WHERE name = ' + QUOTENAME(@LoginName,'''') + ')
BEGIN
	CREATE USER ' + QUOTENAME(@LoginName) + ' FOR LOGIN ' + QUOTENAME(@LoginName) + '
END
ALTER ROLE [db_datareader] ADD MEMBER ' + QUOTENAME(@LoginName) + '
ALTER ROLE [SQLAgentReaderRole] ADD MEMBER ' + QUOTENAME(@LoginName) + '
'
PRINT @SQL
EXEC sp_executesql @SQL
````

### Repository Database Permissions
The service will also need db_owner permissions to the repository database.  The repository database is created by clicking the "Deploy/Update Database" button in the service configuration tool, otherwise it's created when the service starts.  To allow the service account to create the repository database you can use:

````SQL
DECLARE @LoginName SYSNAME = 'DBADashService' /* !!!! Replace with your own service login !!!! */
DECLARE @SQL NVARCHAR(MAX)
SET @SQL = N'
GRANT CREATE ANY DATABASE TO ' + QUOTENAME(@LoginName) 
PRINT @SQL
EXEC sp_executesql @SQL
````

Or to grant the permissions after creating the repository database:

````SQL
DECLARE @LoginName SYSNAME = 'DBADashService' /* !!!! Replace with your own service login !!!! */
DECLARE @RepositoryName SYSNAME = 'DBADashDB' /* !!!! Replace with your own Repository Database Name (default:DBADashDB) !!!! */
DECLARE @SQL NVARCHAR(MAX)
SET @SQL = N'
USE ' + QUOTENAME(@RepositoryName) + '
GO
CREATE USER ' + QUOTENAME(@LoginName) + ' FOR LOGIN ' + QUOTENAME(@LoginName) + '
GO
ALTER ROLE [db_owner] ADD MEMBER ' + QUOTENAME(@LoginName) 
PRINT @SQL
EXEC sp_executesql @SQL
````

## Config file Security

The application configuration is stored in a "ServiceConfig.json" file.  You can edit this manually but it's recommended to use the "DBADashServiceConfigTool.exe" app to ensure a valid configuration.  Sensitive information like connection string passwords and AWS Secret key are encrypted automatically.  This is done to avoid storing the data in plain text but it should be considered as **obfuscation** rather than encryption (due to the storage of encryption keys).  Ideally you should use Windows authentication to connect to your SQL instances which avoids the need to store passwords in the config file.  

If you are collecting data from remote SQL instances via a S3 bucket you could consider using IAM roles instead of specifying the credentials in the config file.  I would also recommend creating a new S3 bucket and configure minimal permissions that allow only read/write access to the new bucket.

## DBA Dash GUI

To use the DBA Dash GUI, users only need access to the DBA Dash repository database.  No access is required to the monitored instances.  To grant the minimum permissions to run the DBA Dash GUI, add the user the the **App** role in the DBA Dash database. This grants the user SELECT and EXECUTE permissions to the database.

The **ManageGlobalViews** role can be used to allow the user to save their customized metrics dashboards for all users. They will also have access to delete shared dashboards.  Users with db_owner will also have the same access.  Otherwise the user can still create their own dashboards.  

