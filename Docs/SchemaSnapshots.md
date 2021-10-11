# Schema Snapshots
## About
Schema snapshots are an optional feature that allows you to take a point in time schema snapshot of your SQL Databases.  This can provide a useful schema history for your database objects and allow you to track exactly what changes have been made and when.  It can also be used to compare schemas between databases - though it's a bit limited as a comparison tool compared to other tools on the market.

Schema snapshots use [SMO](https://en.wikipedia.org/wiki/SQL_Server_Management_Objects) to script out the objects in your database to be stored in the DBA Dash repository database.  You can pick the databases you want to track and what schedule you want to run the snapshots.
## Before you enable schema snapshots
Creating schema snapshots of your SQL Server databases might take some time depending on the schema of your databases and the number of databases on your SQL Server instances. This is potentially a heavier operation than some of the other data collections - though it currently runs single threaded and is unlikely to cause performance issues.  

The default schedule for schema snapshots is daily at 11pm but you can [configure](Collection.md) this to run on a schedule that works best for your instance. If you have hundreds or thousands of databases on your SQL instance the snapshot process could take a considerable amount of time.  Also, if you have a single database that has ***millions*** of objects it might not be suitable for schema snapshots and should be excluded. You should also exclude any databases that have a very volatile database schema. For normal use cases this feature should work very well and could be something you find very useful.
## Setup
![DBA Dash Schema Snapshot Setup](/Docs/DBADash_SchemaSnapshotSetup.PNG)

Schema snapshots are configured for your SQL instances using the DBA Dash Service Config Tool.  When adding or updating an instance in the "Source" tab, select the "Other" sub-tab to configure schema snapshots.
 - Enter a comma-separated list of databases you want to snapshot.  Or use "*" for all databases. Databases can be excluded with "-".
 e.g.   **DB1,DB2,DB3** = *DB1, DB2 & DB3 only*
 ***,-ExcludedDB** = *All Databases except ExcludedDB*
* Click Add/Update to add or update the connection to the config.
* Click "Save" to save the new config.
* Restart the service to pick up the change.

## Under the hood
Schema snapshots have a storage efficient design that means you can take regular snapshots without needing to worry about the storage of those snapshots.  The DBA Dash GUI provides a good way to work with the collected snapshot data so you don't need to read on unless you are curious about how this works...

The scripted objects are stored in a table called "dbo.DDL" and they are stored as compressed NVARCHAR text.  A SHA_256 hash is computed for the text and this is used to ensure that a script is stored once only - even if the text appears in many databases across instances.  If there is any small difference in the text (e.g. whitespace), the hash value would be different and there would be a separate row in this table.

It's recommended to create your snapshots at a low frequency (daily), but even if you generated a snapshot every 5min - as long as the schema isn't changing, you are not storing any extra data.  

The "dbo.DDL" table has a "DDLID" key column that we can use to associate an object with a particular schema.  There is a "dbo.DBObjects" table that tracks the objects in each database.  The "dbo.DDLHistory" table is used to provide a schema history for each object.  The current version of an object has a ValidTo date of "9999-12-31".  If the schema has changed for an object, the ValidTo is updated to the current date and a new row is inserted with a ValidFrom set to the current date and a ValidTo set to "9999-12-31". Data only changes in this table for objects that have changed in the new snapshot.  The "dbo.DDLSnapshot_Add" stored procedure handles the processing of new database snapshots.

**Tip:** Use `CAST(DECOMPRESS(DDL) AS NVARCHAR(MAX))` to convert the binary data in the DDL table to text form in SQL if required.

There is also a "dbo.DDLSnapshotsLog" table that is of interest. This stores the dates of the snapshots against your database. If a snapshot runs and there are no changes it updates the ValidatedSnapshot column for the existing snapshot.  If there are any changes at all it will create a new row for the database in this table.  This allows us to see that we have a database snapshot created last month and we have validated that snapshot hasn't changed up to the current date.
*Note: This isn't a total guarantee as changes could be applied and rolled back between taking snapshots.*

If you want to get the full schema for a database at any particular date, use the **dbo.DBSchemaAtDate** function, passing in the DatabaseID and date.  e.g.

    DECLARE @Date DATETIME2(3) = '20210716'
    DECLARE @DB SYSNAME = 'ReportServer'
    DECLARE @Instance SYSNAME = 'Dash2019'
    
    SELECT I.Instance,
    		D.name AS DB,
    		S.ObjectType,
    		OT.TypeDescription,
    		S.ObjectName,
    		S.SchemaName,
    		S.SnapshotDate,
    		S.DDLID,
    		CAST(DECOMPRESS(DDL.DDL) as NVARCHAR(MAX))
    FROM dbo.Instances I 
    INNER JOIN dbo.Databases D ON D.InstanceID = I.InstanceID
    CROSS APPLY dbo.DBSchemaAtDate(D.DatabaseID,@Date) S 
    INNER JOIN dbo.DDL ON S.DDLID = DDL.DDLID
    INNER JOIN dbo.ObjectType OT ON S.ObjectType = OT.ObjectType
    WHERE D.name = @DB
    AND I.Instance = @Instance
