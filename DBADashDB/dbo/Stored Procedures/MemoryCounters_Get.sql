CREATE PROC dbo.MemoryCounters_Get(
    @InstanceID INT,
    @FromDate DATETIME2(2),
    @ToDate DATETIME2(2)
)
AS
DECLARE @Counters VARCHAR(MAX)

SET @Counters = STUFF((
SELECT ',' + CAST(CounterID AS VARCHAR(MAX))
FROM dbo.Counters
WHERE (object_name='Buffer Manager'
OR object_name = 'Memory Manager'
OR counter_name='Count of Nodes reporting thread resources low'
OR object_name = 'sys.dm_os_sys_memory'
)
FOR XML PATH(''),TYPE).value('.','VARCHAR(MAX)'),1,1,'')

EXEC dbo.PerformanceCounterSummary_Get @Counters = @Counters, 
                                       @InstanceID = @InstanceID,  
                                       @FromDate = @FromDate, 
                                       @ToDate = @ToDate  