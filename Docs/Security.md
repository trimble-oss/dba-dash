# DBA Dash Security

## Service Account
The service account used for DBADash should be a member of the **sysadmin** role on the SQL instance and also be a member of  the **local "Administrators" group**.  It's possible to configure a user with a lower level of access but this might prevent DBA Dash from collecting certain information about your SQL instances.

### Why local admin?  
This is required to run WMI queries (optional).  These are used to collect drive space, driver info and o/s info.  If you don't want the tool to use WMI, select the "**Don't use WMI**" checkbox when adding an instance in the DBA Dash Service Config Tool. If you don't check this box, WMI collection will be attempted - resulting in a logged error if the user doesn't have access.  If drive space isn't collected via WMI it will be collected through SQL instead - but only for drives that contain SQL files. You could provision the required WMI access to your service account.  

### Why SysAdmin?

If the tool doesn't run as sysadmin it won't be able to collect last good CHECKDB date as well as some other data from the registry like processor name, system manufacturer and model.  If you don't want to grant sysadmin access, you can assign the permissions listed below instead

**Server Level Permissions:**
* View Server State
* View Any Database
* Connect Any Database
* Alter Event Session (For Slow Query trace if used)
* View Any Definition
**MSDB Database:**
* Add user to db_datareader role.
* GRANT EXECUTE ON agent_datetime TO [DBADashUser]

## Config file Security

The application configuration is stored in a "ServiceConfig.json" file.  You can edit this manually but it's recommended to use the "DBADashServiceConfigTool.exe" app to ensure a valid configuration.  Sensitive information like connection string passwords and AWS Secret key are encrypted automatically.  This is done to avoid storing the data in plain text but it should be considered as **obfuscation** rather than encryption (due to the storage of encryption keys).  Ideally you should use Windows authentication to connect to your SQL instances which avoids the need to store passwords in the config file.  

If you are collecting data from remote SQL instances via a S3 bucket you could consider using IAM roles instead of specifying the credentials in the config file.  I would also recommend creating a new S3 bucket and configure minimal permissions that allow only read/write access to the new bucket.