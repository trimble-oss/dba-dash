CREATE PROC dbo.MemoryCounters_Get(
    @InstanceID INT
)
AS
SELECT CounterID
FROM dbo.Counters
WHERE   (   object_name='Buffer Manager'
            OR object_name = 'Memory Manager'
            OR counter_name='Count of Nodes reporting thread resources low'
            OR object_name = 'sys.dm_os_sys_memory'
            OR object_name = 'Plan Cache'
        )