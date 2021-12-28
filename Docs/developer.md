# Developer Notes

## Overview of Projects

* ### DBADash
    A class library that is shared with the other projects.  The code to collect and import data is located here.

* ### DBADash.Test
    Contains a limited set of tests - to be expanded upon.  

* ### DBADashConfig
    For automating the ServiceConfig.json file from the command line.  e.g. Adding new SQL Server connections to the config.

* ### DBADashDB
    The DBA Dash repository database source code.  The project output is a *.dacpac file that is used to create and upgrade the repository database.

* ### DBADashGUI
    A Windows Forms app for displaying the collected data.

* ### DBADashService
    A Windows service that is responsible for collecting data from your SQL instances and importing that data into the repository database.

* ### ServiceConfigTool
    A Windows Forms app that is used to manage the ServiceConfig.json file.  This serves the same purpose as the *DBADashConfig* project but in GUI instead of commandline format.  e.g. Adding new SQL Server connections to the config.

* ### DBADashReports
    SSRS reports for displaying the collected data.  This is a legacy project that has been replaced with *DBADashGUI*, but this could be revived with community contributions.

## Build

The solution will output to a common build folder *DBADashBuild*.  This common build folder contains everything you need; the service, configuration tools and GUI. 

## Modifying existing collections

The code for existing collections is located [DBADash/SQL](DBADash/SQL).  If you need to add or remove columns from the results you will need to update the associated user defined type in the DBADashDB project.  Also, the stored procedure that handles the import called *{CollectionType}_Upd* will also need to be modified along with the tables that store the data.  In the DBImporter.cs file there is a UpgradeDS method that should be modified to add the extra columns to the DataTable if they don't already exist.  The call to UpgradeDS is to allow us to import data collected by older versions of DBA Dash. e.g. If you have a remote instance of DBA Dash that pushes data to a S3 bucket you don't need to upgrade this instance at the same time.

## Adding a new collection

DBA Dash can already collect [custom performance counters](OSPerformanceCounters.md) and you can add also add [custom checks](CustomChecks.md).  If those options are not suitable you might consider modifying DBA Dash to add a new collection.  Ideally the changes you make will be contributed back to the main project as other people will be able to benefit from them and it will make updates easier for you in future.  To be included back into the main project the changes must have value to other users of DBA Dash - consider how you can make the changes generic and avoid including any bespoke features.  If you still need to develop bespoke features and functionality these will need to be maintained in a separate fork of the project.

* Create a SQL Query that will capture the data for the new collection. 
    - Consider the impact of collecting this data and the frequency of collection.  The query should run on all versions of SQL Server supported by DBA Dash or code added to exclude the collection from unsupported versions
* Add a new item to the DBADash project under the SQL folder.  Select "Text" file and name the file "SQL{MyCollection}.sql"
* The SQLStrings.cs file can be modified to include the new collection.
* Add the collection type to the *CollectionType* enum in the DBCollector class.
* There is a collectionTypeIsApplicable method in the DBCollector that determines if a collection should be run that you should review.
* Collection will be handled by the *collect* method.  This method includes some generic handling so modification is not necessary in most cases. 
* The CollectionSchedules.cs file has the default collection schedules.  If you want the new collection to be collected automatically by default, the collection should be added to the DefaultSchedules.
* The DBADashDB project will need to be modified to add:
    - A new user defined type with the name {MyCollection}.  This type must match the column list from your collection query.
    - A new stored procedure called *{MyCollection}_Upd*.  
      - This stored procedure should take a parameter {MyCollection} of type {MyCollection} (table valued parameter). 
      - This stored procedure should also take an @InstanceID parameter and a @SnapshotDate parameter.
      - dbo.CollectionDates_Upd can be called to log when the collection has ran.
      - The stored procedure needs to process/store the collected data.  It's likely you will need to add new tables to store the data associated with your new collection.
      - The Script.PostDeployment1.sql file has an insert into CollectionDatesThresholds that you might want to modify.
* The DBImporter.cs file has the code used to import the data.  Add {MyCollection} to the tables string array.  This will ensure that *{MyCollection}_Upd* gets called for your new collection.
* You might also need to consider data retention.  Some collections use partition switching to efficiently remove old data.  The service calls PurgeData to remove old partitions.
