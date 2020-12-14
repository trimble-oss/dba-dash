# OS Performance Counters & Custom Metrics

# Summary
DBAChecks will automatically capture key performance metrics from the sys.dm_os_performance_counters DMV.  e.g. Page reads/sec, Memory Grants Pending, SQL Compilations/sec and more. The performance counters collected can easily be customized - adding additional counters or removing counters that you are not interested in.  It's also possible to add your own appliation performance metrics by creating a stored procedure that will return this data in a specified format.

# OS Performance Counters
To customize what is collected, edit the "PerformanceCounters.xml" file or create a copy of the file called "PerformanceCountersCustom.xml".  It's recommended to create a "PerformanceCountersCustom.xml" file which will completely override the existing "PerformanceCounters.xml" file.  This will simplify appliation upgrades - otherwise you would need to take care not to overwrite your custom version of "PerformanceCounters.xml".

A short version of the XML file is listed below:

```XML
<Counters>
  <Counter object_name="Buffer Manager" counter_name="Buffer cache hit ratio" instance_name="" />
  <Counter object_name="Buffer Manager" counter_name="Buffer cache hit ratio base" instance_name="" />
  <Counter object_name="Buffer Manager" counter_name="Lazy writes/sec" instance_name="" />
</Counters>
```

For the object_name this should be everying after the colon symbol ":".  e.g. "Buffer Manager" instead of "SQLServer:Buffer Manager".  The value before the colon can vary for named instances or Azure DB so this is excluded.

If the counter is of type "537003264" or "1073874176" you will need to include the base counter to allow the counter to be calculated - as with "Buffer cache hit ratio" above.  Also, check that there is a row in the CounterMapping table in the DBAChecks repository database.

If the instance_name is specified in the XML (including a blank string) the counter will filter for that specific counter instance.  If you want to collect all instances of a counter you can omit the instance_name attribute from the XML.

# Custom SQL Counters

 You are not limited to the counters available in os performance counters DMV - It's possible to create a stored procedure to return any custom metric.  The stored procedure needs to return the data in a specific format - the same format as the data collected for os performance counters.  This allows your custom metrics to be collected and treated in the same way as os performance counters.
 
 Here is an example stored procedure:
 
```SQL 
 CREATE PROC dbo.DBAChecks_CustomPerformanceCounters
 AS
 ''TODO: Add example here...
 ```
 